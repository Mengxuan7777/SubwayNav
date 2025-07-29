import openai
import base64
import os
import sys
import json
from concurrent.futures import ThreadPoolExecutor, as_completed

# Get the API key & set client
with open("C:/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key.txt", "r") as f:
    api_key = f.read().strip()
client = openai.OpenAI(api_key=api_key)

# Encode images
def encode_image(image_path):
    # Open the image as bytes and encode to base64 string
    with open(image_path, "rb") as img_file:
        return base64.b64encode(img_file.read()).decode('utf-8')

# Analyze batches of images with the same prompt
def analyze_images(image_paths, prompt):
    image_contents = []
    for img_path in image_paths:
        fname = os.path.basename(img_path)
        image_contents.append({"type": "text", "text": f"Filename: {fname}"})
        image_contents.append({
            "type": "image_url",
            "image_url": {"url": f"data:image/jpeg;base64,{encode_image(img_path)}"}
        })
    messages = [{
        "role": "user",
        "content": [{"type": "text", "text": prompt}] + image_contents
    }]
    try:
        response = client.chat.completions.create(
            model="gpt-4.1-mini",
            messages=messages,
            response_format={"type": "json_object"},  # <- enforce JSON output
            max_tokens=500
        )
        # Parse the response content as JSON array or dict
        content = response.choices[0].message.content
        try:
            result = json.loads(content)
        except Exception as exc:
            print("ERROR parsing response as JSON:", exc, file=sys.stderr)
            result = None  # fallback to None for clarity
        return result
    except Exception as exc:
        print("ERROR during OpenAI API call:", exc, file=sys.stderr)
        return None

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python analyze_images.py <image_folder> <prompt>")
        sys.exit(1)

    image_folder = sys.argv[1]
    prompt = sys.argv[2]

    image_files = [os.path.join(image_folder, fn) for fn in os.listdir(image_folder)
                   if fn.lower().endswith(('.jpg', '.jpeg', '.png'))]

    batch_size = 10
    batches = [image_files[i:i+batch_size] for i in range(0, len(image_files), batch_size)]

    results = [None] * len(batches)

    # Run parallel batch analysis & return as soon as a batch is done
    with ThreadPoolExecutor(max_workers=min(4, len(batches))) as executor:
        future_to_index = {
            executor.submit(analyze_images, batch, prompt): idx
            for idx, batch in enumerate(batches)
        }
        for future in as_completed(future_to_index):
            idx = future_to_index[future]
            try:
                batch_result = future.result()
                # Print THIS batch as soon as finished
                print(json.dumps(batch_result))
                sys.stdout.flush()  # Ensure immediate output
            except Exception as exc:
                # Print a descriptive error for debugging (stderr is safe for error info)
                print(f"ERROR processing batch {idx}: {exc}", file=sys.stderr)
                # Output an empty result so that downstream doesn't break
                print('[]', file=sys.stdout)
                sys.stdout.flush()