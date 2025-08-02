import openai
import base64
import os
import sys
import json
import re
import csv
from concurrent.futures import ThreadPoolExecutor, as_completed
from threading import Lock
from datetime import datetime

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
with open("C:/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key.txt", "r") as f:
    api_key = f.read().strip()
client = openai.OpenAI(api_key=api_key)

# --- Image encoding ---
def encode_image(image_path):
    with open(image_path, "rb") as img_file:
        return base64.b64encode(img_file.read()).decode('utf-8')

def make_image_message(img_path):
    fname = os.path.basename(img_path)
    # Determine mime type
    ext = os.path.splitext(img_path)[-1].lower()
    if ext == ".png":
        mime = "png"
    else:
        mime = "jpeg"
    return [
        {"type": "text", "text": f"Filename: {fname}"},
        {
            "type": "image_url",
            "image_url": f"data:image/{mime};base64,{encode_image(img_path)}"
        }
    ]
# --- Pre-Process Images ---
def preprocess_reference_images(ref_image_paths):
    ref_msgs = []
    for img_path in ref_image_paths:
        ref_msgs.extend(make_image_message(img_path))
    return ref_msgs

def preprocess_batch_images(batch_image_paths):
    messages = []
    filename_to_image = {}
    for image_path in batch_image_paths:
        with open(image_path, "rb") as f:
            b64_image = base64.b64encode(f.read()).decode("utf-8")
        image_type = "jpeg" if image_path.lower().endswith((".jpg", ".jpeg")) else "png"
        b64_data = f"data:image/{image_type};base64,{b64_image}"
        fname = os.path.basename(image_path)

        # Add to dict
        filename_to_image[fname] = b64_data

        # Keep building OpenAI messages as normal
        messages.append({
            "type": "text",
            "text": f"Filename: {fname}"
        })
        messages.append({
            "type": "image_url",
            "image_url": { "url": b64_data }
        })

    return messages, filename_to_image

def build_message_sequence(reference_msgs, batch_msgs, filename_to_image):
    messages = [{
        "role": "system",
            "content": (
                "You are provided with several reference images for context only. These images are captured by "
                "surveillance cameras in a subway station to monitor fire emergencies and crowd density. "
                "The image filenames indicate the locations where they were taken. Do not generate any analysis "
                "for these images. Instead, use them as visual references for comparison when needed."
        )
    }]
    if reference_msgs:
        messages.append({
            "role": "assistant",
            "content": reference_msgs
        })

    user_content = [
        {"type": "text","text": (
            "You are now provided with a set of images captured by surveillance cameras in a subway station to monitor fire emergencies and crowd levels. "
            "Each image file name indicates the location where it was taken. Use the file name (without extension) as the value for the 'name' field. "
            "Analyze ONLY the following images. For each image, return a JSON object with the following fields:\n"
            "- name: the image file name without extension (as a string)\n"
            "- crowdCost: an integer from 0 to 10 representing the crowd level (0 = least crowded, 10 = most crowded)\n"
            "- fireCost: an integer from 0 to 10 representing the fire intensity (0 = no fire, 10 = extreme fire)\n\n"
            "Respond with a JSON array containing these objects, and provide no additional text."
            )
        }
    ] + batch_msgs
    messages.append({
        "role": "user",
        "content": user_content
    })
    return messages

def log_token_usage_csv(token_count, log_file=LogFile):
    log_exists = os.path.exists(log_file)
    with token_log_lock, open(log_file, "a", newline="") as f:
        writer = csv.writer(f)
        if not log_exists:
            writer.writerow(["timestamp", "total_tokens"])
        writer.writerow([datetime.now().isoformat(), token_count])

def analyze_images_with_refs(batch_image_paths, reference_msgs):
    batch_msgs, filename_to_image = preprocess_batch_images(batch_image_paths)
    messages = build_message_sequence(reference_msgs, batch_msgs, filename_to_image)
    try:
        response = client.chat.completions.create(
            model="gpt-4.1-mini",
            messages=messages,
            response_format={"type": "text"},
            max_tokens=500
        )

        # --- Log usage in CSV ---
        if hasattr(response, "usage") and response.usage:
            total_tokens = getattr(response.usage, "total_tokens", None)
            if total_tokens is not None:
                log_token_usage_csv(total_tokens)

        content = response.choices[0].message.content
        try:
            result = extract_json_array(content)
        except Exception as exc:
            print("ERROR parsing response as JSON:", exc, file=sys.stderr)
            result = None
        return result
    except Exception as exc:
        print("ERROR during OpenAI API call:", exc, file=sys.stderr)
        return None

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

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python analyze_images.py <image_folder> <reference_folder>")
        sys.exit(1)

    image_folder = sys.argv[1]
    reference_folder = sys.argv[2]

    image_files = [os.path.join(image_folder, fn) for fn in os.listdir(image_folder)
                   if fn.lower().endswith(('.jpg', '.jpeg', '.png'))]
    ref_image_files = [os.path.join(reference_folder, fn) for fn in os.listdir(reference_folder)
                       if fn.lower().endswith(('.jpg', '.jpeg', '.png'))]

    reference_msgs = preprocess_reference_images(ref_image_files)

    batch_size = 6
    batches = [image_files[i:i + batch_size] for i in range(0, len(image_files), batch_size)]

    with ThreadPoolExecutor(max_workers=min(4, len(batches))) as executor:
        future_to_index = {
            executor.submit(analyze_images_with_refs, batch, reference_msgs): idx
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