from openai import OpenAI
from flask import Flask, request, jsonify
import os
import json
import re

with open("C:/Users/tower/Documents/OpenAI_ChatGPT_Key.txt", "r") as f:
    api_key = f.read().strip()

client = OpenAI(api_key=api_key)
app = Flask(__name__)

def build_llm_prompt(start, paths):
    prompt = f"The agent is located at '{start}'. Based on current station conditions, here are possible exit paths:\n\n"
    labels = ['A', 'B', 'C', 'D']
    for i, path in enumerate(paths):
        p = path["nodes"]
        if p[0].startswith("agent_"):
            p = p[1:]
        f = path["factors"]
        c = path["cost"]
        prompt += (
            f"Path {labels[i]}:\n"
            f"- Nodes: {p}\n"
            f"- Cost: {c}\n"
            f"- Distance: {f['distance']}, Crowd: {f['crowd']}, "
            f"Smoke: {f['smoke']}, Fire: {f['fire']}\n\n"
        )
    prompt += (
        "Which path should the agent take and why?\n"
        "Respond in valid JSON format like this:\n"
        "{\n"
        "  \"reason\": \"[reason for choosing this path]\",\n"
        "  \"path\": [\"Node1\", \"Node2\", \"Node3\"]\n"
        "}"
    )
    
    return prompt

@app.route("/query", methods=["POST"])
def query():
    data = request.get_json()
    start_node = data.get("start_node")
    paths = data.get("paths")

    if not start_node or not paths:
        return jsonify({"error": "Missing 'start_node' or 'paths' in request."}), 400

    prompt = build_llm_prompt(start_node, paths)
    print("📤 Sending prompt to OpenAI:\n", prompt)

    try:
        response = client.chat.completions.create(
            model="gpt-3.5-turbo",
            messages=[{"role": "user", "content": prompt}],
            temperature=0.4,
            max_tokens=150
        )
        reply = response.choices[0].message.content.strip()
        print("✅ LLM reply:\n", reply)

        # 👇 Clean and extract JSON block
        match = re.search(r"\{.*?\}", reply, re.DOTALL)
        if not match:
            raise ValueError("❌ No JSON object found in LLM reply.")

        cleaned_json = match.group(0)
        parsed = json.loads(cleaned_json)
        print("✅ Parsed LLM reply:", parsed)
        return jsonify(parsed)

    except Exception as e:
        print("❌ Error parsing reply:", e)
        return jsonify({"error": f"Invalid LLM JSON reply: {str(e)}"}), 500

if __name__ == "__main__":
    app.run(debug=True, port=5000)
