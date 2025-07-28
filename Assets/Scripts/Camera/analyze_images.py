import openai
import base64
import os
import sys
import json
from concurrent.futures import ThreadPoolExecutor, as_completed

# Get the API key
with open("C:/Users/tower/Documents/API Keys/OpenAI_ChatGPT_Key.txt", "r") as f:
    openai.api_key = f.read().strip()

# Encode images
def encode_image(image_path):
    with open(image_path, "rb") as img_file:
        return base64.b64encode(img_file.read()).decode('utf-8')

# Analyze batches of images with the same prompt
def analyze_images(image_paths, prompt):
    image_contents = [
        {
            "type": "image_url",
            "image_url": {"url": f"data:image/jpeg;base64,{encode_image(img)}"}
        }
        for img in image_paths
    ]
    response = openai.ChatCompletion.create(
        model="gpt-4-vision-preview",
        messages=[
            {
                "role": "user",
                "content": [{"type": "text", "text": prompt}] + image_contents
            }
        ],
        max_tokens=500,
    )
    return response['choices'][0]['message']['content']

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

    # Run parallel batch analysis
    with ThreadPoolExecutor(max_workers=min(4, len(batches))) as executor:  # limit to a reasonable number of parallel requests
        future_to_index = {
            executor.submit(analyze_images, batch, prompt): idx 
            for idx, batch in enumerate(batches)
        }
        for future in as_completed(future_to_index):
            idx = future_to_index[future]
            try:
                results[idx] = future.result()
            except Exception as exc:
                results[idx] = f'Error during analysis: {exc}'

    print(json.dumps(results))