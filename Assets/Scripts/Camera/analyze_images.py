import openai
import base64
import os
import sys
import json
import re
from concurrent.futures import ThreadPoolExecutor, as_completed

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
    return [
        {"type": "text", "text": f"Filename: {fname}"},
        {
            "type": "image_url",
            "image_url": {"url": f"data:image/jpeg;base64,{encode_image(img_path)}"}
        }
    ]

# --- Reference image preprocessing ---
def preprocess_reference_images(ref_image_paths):
    ref_msgs = []
    for img_path in ref_image_paths:
        ref_msgs.extend(make_image_message(img_path))
    return ref_msgs

# --- Batch image preprocessing ---
def preprocess_batch_images(batch_image_paths):
    batch_msgs = []
    for img_path in batch_image_paths:
        batch_msgs.extend(make_image_message(img_path))
    return batch_msgs

def build_message_sequence(reference_msgs, batch_msgs):
   # Create prompt for reference images
    messages = [{
            "role": "system",
            "content": "You are provided with several example images as reference only. Do not output any analysis for these, but use them for comparison if needed."
        }]
    # Bundle all reference images in a single context/assistant message, so they are not analyzed
    if reference_msgs:
        messages.append({
            "role": "assistant",
            "content": reference_msgs
        })

    # Create prompt for images to be analyzed (i.e. batch images)
    user_content = [
        {"type": "text", "text": (
            "Now analyze ONLY the following images. For each, return a JSON object with the fields: "
            "name (filename without extension, string), crowdCost (integer 0-10), fireCost (integer 0-10). "
            "Respond with a JSON array and nothing else."
        )}
    ] + batch_msgs
    messages.append({
        "role": "user",
        "content": user_content
    })
    return messages

# --- Main batch analyzer ---
def analyze_images_with_refs(batch_image_paths, reference_msgs):
    batch_msgs = preprocess_batch_images(batch_image_paths)
    messages = build_message_sequence(reference_msgs, batch_msgs)
    try:
        response = client.chat.completions.create(
            model="gpt-4.1-mini",
            messages=messages,
            response_format={"type": "text"},
            max_tokens=500
        )
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

# --- Usage: python analyze_images.py <image_folder> <reference_folder> ---
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