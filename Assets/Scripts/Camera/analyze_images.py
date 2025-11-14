import openai
import base64
import os
import sys
import json
import re
import csv
import time
import uuid
from concurrent.futures import ThreadPoolExecutor, as_completed
from threading import Lock
from datetime import datetime

# Optional image preprocessing (resize/compress) via Pillow
try:
    from PIL import Image
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False

# --- Log File for Tokens ---
# Make sure log directory exists
LogDirectory = "C:/Users/tower/Documents/Unity Projects/SubwayNav/Assets/StreamingAssets/TokenLog"
os.makedirs(LogDirectory, exist_ok=True)

# Create unique file per run
runTimestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
LogFile = os.path.join(LogDirectory, f"token_usage_log_{runTimestamp}.csv")
token_log_lock = Lock()

def reset_token_log_csv(log_file=LogFile):
    with token_log_lock, open(log_file, "w", newline="") as f:
        writer = csv.writer(f)
        writer.writerow(["timestamp", "total_tokens"])

def log_token_usage_csv(token_count, log_file=LogFile):
    with token_log_lock, open(log_file, "a", newline="") as f:
        writer = csv.writer(f)
        writer.writerow([datetime.now().isoformat(), token_count])

# Reset the log file:
reset_token_log_csv()

# --- Key and API Setup ---
with open("C:/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key_1.txt", "r") as f:
    api_key = f.read().strip()
client = openai.OpenAI(api_key=api_key)

# --- Image encoding and preprocessing ---

def _read_bytes(path):
    with open(path, "rb") as f:
        return f.read()

def _infer_mime(path):
    ext = os.path.splitext(path)[-1].lower()
    return "png" if ext == ".png" else "jpeg"

def preprocess_and_encode_image(image_path, max_side=1024, jpeg_quality=85):
    """
    Load, optionally resize/compress, and return a data URL for the image.
    Falls back to raw bytes if Pillow isn't available.
    Returns (data_url, meta) where meta has {'orig_bytes', 'final_bytes', 'mime'}.
    """
    mime = _infer_mime(image_path)
    raw = _read_bytes(image_path)

    if not PIL_AVAILABLE:
        b64 = base64.b64encode(raw).decode("utf-8")
        return f"data:image/{mime};base64,{b64}", {"orig_bytes": len(raw), "final_bytes": len(raw), "mime": mime}

    try:
        with Image.open(image_path) as img:
            img = img.convert("RGB") if mime == "jpeg" else img.convert("RGBA")
            w, h = img.size
            scale = max(w, h) / float(max_side) if max(w, h) > max_side else 1.0
            if scale > 1.0:
                new_w, new_h = int(w / scale), int(h / scale)
                img = img.resize((new_w, new_h), Image.LANCZOS)

            from io import BytesIO
            buf = BytesIO()
            save_kwargs = {"format": "JPEG", "quality": jpeg_quality} if mime == "jpeg" else {"format": "PNG", "optimize": True}
            img.save(buf, **save_kwargs)
            data = buf.getvalue()
    except Exception:
        # Fallback to raw bytes if preprocessing fails
        data = raw

    b64 = base64.b64encode(data).decode("utf-8")
    return f"data:image/{mime};base64,{b64}", {"orig_bytes": len(raw), "final_bytes": len(data), "mime": mime}

# --- Pre-Process Images ---

def make_image_message_from_dataurl(fname, data_url):
    return [
        {"type": "text", "text": f"Filename: {fname}"},
        {"type": "image_url", "image_url": {"url": data_url}}
    ]

def preprocess_reference_images(ref_image_paths, max_side=768, jpeg_quality=80):
    ref_msgs = []
    for img_path in ref_image_paths:
        fname = os.path.basename(img_path)
        data_url, _meta = preprocess_and_encode_image(img_path, max_side=max_side, jpeg_quality=jpeg_quality)
        ref_msgs.extend(make_image_message_from_dataurl(fname, data_url))
    return ref_msgs

# Build messages for a single target image

def build_single_image_messages(reference_msgs, fname, data_url, request_id):
    instructions = (
        "You will analyze exactly one surveillance image from a subway station. "
        "Return a single JSON object with fields: "
        "id (echo exactly the provided id), name (filename without extension), "
        "crowdCost (integer 0-10), fireCost (integer 0-10). "
        "Respond with JSON only, no extra text."
    )

    messages = [
        {"role": "system", "content": (
            "You are provided with several reference images for context only. "
            "Do not analyze reference images; use them only for comparison."
        )}
    ]
    if reference_msgs:
        messages.append({"role": "assistant", "content": reference_msgs})

    user_content = [
        {"type": "text", "text": f"id: {request_id}"},
        {"type": "text", "text": instructions},
        {"type": "text", "text": f"Filename: {fname}"},
        {"type": "image_url", "image_url": {"url": data_url}},
    ]
    messages.append({"role": "user", "content": user_content})
    return messages

def log_token_usage_csv(token_count, log_file=LogFile):
    log_exists = os.path.exists(log_file)
    with token_log_lock, open(log_file, "a", newline="") as f:
        writer = csv.writer(f)
        if not log_exists:
            writer.writerow(["timestamp", "total_tokens"])
        writer.writerow([datetime.now().isoformat(), token_count])

def analyze_single_image(image_path, reference_msgs, data_url_cache, max_retries=2, base_backoff=0.75):
    fname = os.path.basename(image_path)
    name_no_ext = os.path.splitext(fname)[0]
    data_url = data_url_cache.get(fname)
    if not data_url:
        data_url, _ = preprocess_and_encode_image(image_path)
        data_url_cache[fname] = data_url

    request_id = str(uuid.uuid4())
    messages = build_single_image_messages(reference_msgs, fname, data_url, request_id)

    last_exc = None
    for attempt in range(max_retries + 1):
        try:
            t0 = time.time()
            response = client.chat.completions.create(
                model="gpt-5",
                messages=messages,
                response_format={"type": "text"},
            )
            latency_ms = int((time.time() - t0) * 1000)
            # Log token usage if present
            if hasattr(response, "usage") and response.usage:
                total_tokens = getattr(response.usage, "total_tokens", None)
                if total_tokens is not None:
                    log_token_usage_csv(total_tokens)
            # Parse JSON object
            content = response.choices[0].message.content
            obj = extract_json_object(content)
            # Validate id and name if present
            if isinstance(obj, dict):
                if "id" in obj and obj["id"] != request_id:
                    raise ValueError(f"ID mismatch: expected {request_id}, got {obj.get('id')}")
                if "name" in obj and obj["name"] != name_no_ext:
                    # Not fatal; fix name to expected
                    obj["name"] = name_no_ext
                # Keep only required fields for Unity consumer
                result = {
                    "name": name_no_ext,
                    "crowdCost": int(obj.get("crowdCost", 0)),
                    "fireCost": int(obj.get("fireCost", 0)),
                }
                # stderr monitoring
                print(f"OK {fname} in {latency_ms}ms", file=sys.stderr)
                return result
            raise ValueError("Model did not return a JSON object")
        except Exception as exc:
            last_exc = exc
            # Backoff with jitter
            delay = (base_backoff * (2 ** attempt)) + (0.1 * (attempt + 1))
            print(f"Retry {attempt+1}/{max_retries} for {fname}: {exc} (sleep {delay:.2f}s)", file=sys.stderr)
            time.sleep(delay)
    # After retries, return a safe default
    print(f"FAILED {fname}: {last_exc}", file=sys.stderr)
    return {"name": name_no_ext, "crowdCost": 0, "fireCost": 0}


def analyze_batch_images(batch_image_paths, reference_msgs, data_url_cache, worker_count=4):
    # Bounded worker pool within a batch
    results = [None] * len(batch_image_paths)
    with ThreadPoolExecutor(max_workers=max(1, min(worker_count, len(batch_image_paths)))) as ex:
        future_to_idx = {}
        for idx, path in enumerate(batch_image_paths):
            fut = ex.submit(analyze_single_image, path, reference_msgs, data_url_cache)
            future_to_idx[fut] = idx
        for fut in as_completed(future_to_idx):
            idx = future_to_idx[fut]
            try:
                results[idx] = fut.result()
            except Exception as exc:
                fname = os.path.basename(batch_image_paths[idx])
                print(f"Batch worker error for {fname}: {exc}", file=sys.stderr)
                name_no_ext = os.path.splitext(fname)[0]
                results[idx] = {"name": name_no_ext, "crowdCost": 0, "fireCost": 0}
    return results

def extract_json_array(text):
    match = re.search(r"```(?:json)?\s*([\s\S]*?)```", text)
    if match:
        candidate = match.group(1).strip()
        try:
            return json.loads(candidate)
        except Exception:
            pass

    match = re.search(r"(\[\s*{[\s\S]*?}\s*])", text)
    if match:
        candidate = match.group(1)
        try:
            return json.loads(candidate)
        except Exception:
            pass
    try:
        return json.loads(text.strip())
    except Exception:
        pass
    raise ValueError("Could not extract JSON array from the model output.")


def extract_json_object(text):
    match = re.search(r"```(?:json)?\s*([\s\S]*?)```", text)
    if match:
        candidate = match.group(1).strip()
        try:
            return json.loads(candidate)
        except Exception:
            pass
    # Try to find a single top-level object
    match = re.search(r"(\{[\s\S]*\})", text)
    if match:
        candidate = match.group(1)
        try:
            return json.loads(candidate)
        except Exception:
            pass
    try:
        return json.loads(text.strip())
    except Exception:
        pass
    raise ValueError("Could not extract JSON object from the model output.")


def precache_images(image_paths, max_side=1024, jpeg_quality=85):
    cache = {}
    # Preprocess sequentially to avoid CPU spikes; could be parallel if needed
    for p in image_paths:
        fname = os.path.basename(p)
        try:
            data_url, _ = preprocess_and_encode_image(p, max_side=max_side, jpeg_quality=jpeg_quality)
            cache[fname] = data_url
        except Exception as exc:
            print(f"Precache failed for {fname}: {exc}", file=sys.stderr)
    return cache


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python analyze_images.py <image_folder> <reference_folder>")
        sys.exit(1)

    # Resolve absolute paths
    image_folder = os.path.abspath(sys.argv[1])
    reference_folder = os.path.abspath(sys.argv[2])

    # Validate folders exist
    if not os.path.isdir(image_folder):
        print(f"ERROR: Target image folder does not exist: {image_folder}", file=sys.stderr)
        print('[]')
        sys.exit(1)
    if not os.path.isdir(reference_folder):
        print(f"ERROR: Reference folder does not exist: {reference_folder}", file=sys.stderr)
        print('[]')
        sys.exit(1)

    # Collect files (jpg/jpeg/png) with robust error handling
    try:
        image_files = [os.path.join(image_folder, fn) for fn in os.listdir(image_folder)
                       if fn.lower().endswith((".jpg", ".jpeg", ".png"))]
    except Exception as exc:
        print(f"ERROR: Could not list target image folder {image_folder}: {exc}", file=sys.stderr)
        print('[]')
        sys.exit(1)
    try:
        ref_image_files = [os.path.join(reference_folder, fn) for fn in os.listdir(reference_folder)
                           if fn.lower().endswith((".jpg", ".jpeg", ".png"))]
    except Exception as exc:
        print(f"ERROR: Could not list reference folder {reference_folder}: {exc}", file=sys.stderr)
        ref_image_files = []

    # Stable ordering
    image_files.sort()
    ref_image_files.sort()

    # Informative logging to confirm retrieval locations and counts
    print(f"Loaded {len(image_files)} target images from: {image_folder}", file=sys.stderr)
    print(f"Loaded {len(ref_image_files)} reference images from: {reference_folder}", file=sys.stderr)
    if len(ref_image_files) == 0:
        print("WARNING: No reference images found; proceeding without reference context.", file=sys.stderr)

    if len(image_files) == 0:
        print("[]")
        sys.stdout.flush()
        sys.exit(0)

    # Build reference once
    reference_msgs = preprocess_reference_images(ref_image_files)

    # Preprocess and cache payloads before network I/O
    data_url_cache = precache_images(image_files, max_side=1024, jpeg_quality=85)

    batch_size = 6
    batches = [image_files[i:i + batch_size] for i in range(0, len(image_files), batch_size)]

    # Concurrency over batches; each batch also has per-image concurrency
    with ThreadPoolExecutor(max_workers=min(4, len(batches))) as executor:
        future_to_index = {
            executor.submit(analyze_batch_images, batch, reference_msgs, data_url_cache, 4): idx
            for idx, batch in enumerate(batches)
        }
        for future in as_completed(future_to_index):
            idx = future_to_index[future]
            try:
                batch_result = future.result()
                print(json.dumps(batch_result))
                sys.stdout.flush()
            except Exception as exc:
                print(f"ERROR processing batch {idx}: {exc}", file=sys.stderr)
                print('[]', file=sys.stdout)
                sys.stdout.flush()