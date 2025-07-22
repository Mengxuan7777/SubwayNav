from flask import Flask, request, jsonify
import networkx as nx
import json
from openai import OpenAI
import re

with open("C:/Users/tower/Documents/OpenAI_ChatGPT_Key.txt", "r") as f:
    api_key = f.read().strip()

client = OpenAI(api_key=api_key)
app = Flask(__name__)

EXIT_NODES = ["EX1", "EX2", "EX3", "EX4"]
AGENT_NODE = "Agent"

# === LOAD STATION DATA ===
with open(r"C:\Users\tower\Documents\Unity Projects\SubwayNav\Assets\Nodes\station_nodes.json", "r") as f:
    node_positions = {
        node["name"]: (node["x"], node["y"], node["z"])
        for node in json.load(f)["nodes"]
    }

with open("../Nodes/navmesh_distances.json", "r") as f:
    nav_data = json.load(f)
    navmesh_distances = {
        (entry["from"], entry["to"]): entry["distance"]
        for entry in nav_data["distances"]  
    }


# === BUILD BASE GRAPH ===
G = nx.DiGraph()
for name, pos in node_positions.items():
    G.add_node(name, position=pos)

original_edges = [
    ("US1", "US2"), ("US3", "US2"), ("DS1", "DS2"), ("DS3", "DS2"),
    ("US1", "NM"), ("US2", "NM"), ("US3", "SM"),
    ("DS1", "NM"), ("DS2", "NM"), ("DS3", "SM"),
    ("NM", "EX1"), ("NM", "EX2"),
    ("SM", "EX3"), ("SM", "EX4")
]
edges = original_edges + [(v, u) for u, v in original_edges]

for u, v in edges:
    dist = navmesh_distances.get((u, v))
    if dist is None:
        print(f"⚠️ Missing NavMesh distance for edge: {u} → {v}")
        continue
    G.add_edge(u, v, distance=dist, fire=0.0, crowd=0.0)

# === DYNAMIC COST (runtime-updated by Unity) ===
dynamic_costs = {}
node_costs = {}

@app.route("/update_costs", methods=["POST"])
def update_costs():
    updates = request.get_json()
    print(f"📦 Received dynamic cost updates: {updates}")
    for edge_key, values in updates.items():
        u, v = edge_key.split("->")
        dynamic_costs[(u, v)] = values
        print(f"   {edge_key}: fire={values.get('fire', 0)}, crowd={values.get('crowd', 0)}")
    return jsonify({"status": "success"}), 200

@app.route("/update_node_costs", methods=["POST"])
def update_node_costs():
    data = request.get_json()
    print(f"📦 Received node cost updates:")
    for entry in data:
        name = entry.get("AreaName")
        fire = entry.get("FireCost", 0.0)
        crowd = entry.get("CrowdCost", 0.0)
        if name:
            node_costs[name] = {"fire": fire, "crowd": crowd}
            print(f"   {name}: fire={fire}, crowd={crowd}")
    return jsonify({"status": "success"}), 200

@app.route("/start_path_decision", methods=["POST"])
def start_path_decision():
    data = request.get_json()
    agent_pos = tuple(data["position"])
    agent_edges = data.get("agent_edges", {})
    print(f"🕸️  Received {len(agent_edges)} agent edges:")
    for edge_key, dist in agent_edges.items():
        print(f"   {edge_key} = {dist:.2f}")

    result = run_decision_cycle(agent_pos, agent_edges)
    return jsonify(result), 200

def run_decision_cycle(agent_position, agent_edges):
    temp_graph = G.copy()
    temp_graph.add_node(AGENT_NODE)

    for edge_key, dist in agent_edges.items():
        u, v = edge_key.split("->")
        temp_graph.add_edge(u, v, distance=dist, fire=0.0, crowd=0.0)

    DISTANCE_WEIGHT = 0.1  # 👈 tune this as needed

    for u, v in temp_graph.edges():
        distance = temp_graph[u][v]["distance"]
        fire = dynamic_costs.get((u, v), {}).get("fire", 0.0)
        crowd = dynamic_costs.get((u, v), {}).get("crowd", 0.0)
        # Add node-based cost (max from both ends)
        fire_u = node_costs.get(u, {}).get("fire", 0.0)
        fire_v = node_costs.get(v, {}).get("fire", 0.0)
        crowd_u = node_costs.get(u, {}).get("crowd", 0.0)
        crowd_v = node_costs.get(v, {}).get("crowd", 0.0)
        fire = max(fire, fire_u, fire_v)
        crowd = max(crowd, crowd_u, crowd_v)

        temp_graph[u][v]["fire"] = fire
        temp_graph[u][v]["crowd"] = crowd
        temp_graph[u][v]["weight"] = (distance * DISTANCE_WEIGHT) + fire + crowd

    # Compute shortest paths from Agent to exits
    all_paths = []
    for exit_node in EXIT_NODES:
        try:
            path = nx.shortest_path(temp_graph, AGENT_NODE, exit_node, weight="weight")
            dist = sum(temp_graph[u][v]["distance"] for u, v in zip(path, path[1:]))
            fire = sum(temp_graph[u][v]["fire"] for u, v in zip(path, path[1:]))
            crowd = sum(temp_graph[u][v]["crowd"] for u, v in zip(path, path[1:]))
            total = (dist * DISTANCE_WEIGHT) + fire + crowd

            all_paths.append({
                "exit": exit_node,
                "path": path,
                "distance": dist,
                "fire": fire,
                "crowd": crowd,
                "total": total
            })
        except nx.NetworkXNoPath:
            continue

    # Build prompt for LLM
    prompt = build_prompt(all_paths)
    llm_output = call_llm(prompt)

    return {
        "prompt": prompt,
        "llm_response": llm_output
    }


def build_prompt(paths):
    lines = ["You are helping an agent choose the best path to exit the subway.\n"]
    for i, p in enumerate(paths):
        lines.append(f"Option {i+1}: Exit {p['exit']}")
        lines.append(f"  Path: {p['path']}")
        lines.append(f"  Total Distance: {p['distance']:.2f}")
        lines.append(f"  Fire Cost: {p['fire']:.2f}")
        lines.append(f"  Crowd Cost: {p['crowd']:.2f}")
        lines.append(f"  Total Cost: {p['total']:.2f}\n")
    lines.append("Which option should the agent take and why?\nRespond in JSON format:")
    lines.append('{\n  "reason": "...",\n  "path": ["Node1", "Node2", "..."]\n}')
    return "\n".join(lines)


def call_llm(prompt):
    response = client.chat.completions.create(
        model="gpt-3.5-turbo",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.3
    )
    return response.choices[0].message.content.strip()



if __name__ == "__main__":
    app.run(port=5000, debug=True)
