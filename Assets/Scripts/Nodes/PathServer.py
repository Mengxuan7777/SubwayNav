# path_server.py
from flask import Flask, request, jsonify
import networkx as nx
import json
from math import dist
import uuid

app = Flask(__name__)

# Load JSON node data
with open("station_nodes.json", "r") as f:
    data = json.load(f)

# Build node position lookup
node_positions = {
    node["name"]: (node["x"], node["y"], node["z"])
    for node in data["nodes"]
}

# Create directed graph
G = nx.DiGraph()

# Add nodes with positions
for name, pos in node_positions.items():
    G.add_node(name, position=pos)

# Define edge list
original_edges = [
    ("US1", "US2"), ("US1", "US3"), ("US3", "US2"), ("DS1", "DS2"), ("DS1", "DS3"), ("DS3", "DS2"),
    ("US1", "NM"), ("US2", "NM"), ("US3", "SM"),
    ("DS1", "NM"), ("DS2", "NM"), ("DS3", "SM"),
    ("NM", "EX1"), ("NM", "EX2"),
    ("SM", "EX3"), ("SM", "EX4")
]

# Make bidirectional edges
edges = original_edges + [(v, u) for u, v in original_edges]

# Compute and store base distances
raw_distances = []
for u, v in edges:
    if u in node_positions and v in node_positions:
        d = dist(node_positions[u], node_positions[v])
        raw_distances.append((u, v, d))

for u, v, d in raw_distances:
    G.add_edge(u, v, base_distance=d, crowd=0.0, smoke=0.0, fire=0.0)

@app.route("/path", methods=["POST"])
def get_path():
    try:
        data = request.get_json()
        pos = [data["x"], data["y"], data["z"]]
        crowd_levels = data.get("crowd", {})  # {"US1": 0.5, "SM": 1.0, ...}
        smoke_levels = data.get("smoke", {})
        fire_levels = data.get("fire", {})

        # Update edge weights
        alpha, beta, gamma = 1.0, 2.0, 3.0  # weight multipliers
        
        # Precompute max base distance for normalization
        max_base_distance = max((G[u][v]["base_distance"] for u, v in G.edges()), default=1.0)

        # Update edge weights
        for u, v in G.edges():
            c = max(crowd_levels.get(u, 0.0), crowd_levels.get(v, 0.0))
            s = max(smoke_levels.get(u, 0.0), smoke_levels.get(v, 0.0))
            f = max(fire_levels.get(u, 0.0), fire_levels.get(v, 0.0))
            base = G[u][v]["base_distance"] / max_base_distance  # Normalize to [0,1]
            total = base + alpha * c + beta * s + gamma * f
            G[u][v].update(weight=total, crowd=c, smoke=s, fire=f)

        # Add temporary start node
        start_node = f"agent_{uuid.uuid4()}"
        G.add_node(start_node, position=tuple(pos))
        connection_radius = 5.0  # how far the agent can link to real nodes

        for node_name, node_pos in node_positions.items():
            d = dist(node_pos, pos)
            if d <= connection_radius:
                G.add_edge(start_node, node_name, base_distance=d, crowd=0, smoke=0, fire=0, weight=d)
                G.add_edge(node_name, start_node, base_distance=d, crowd=0, smoke=0, fire=0, weight=d)

        goal_nodes = ["EX1", "EX2", "EX3", "EX4"]
        paths = []

        for goal in goal_nodes:
            try:
                path = nx.shortest_path(G, start_node, goal, weight="weight")
                cost = nx.path_weight(G, path, weight="weight")
                d = sum(G[u][v]["base_distance"] for u, v in zip(path[:-1], path[1:]))
                c = sum(G[u][v]["crowd"] for u, v in zip(path[:-1], path[1:]))
                s = sum(G[u][v]["smoke"] for u, v in zip(path[:-1], path[1:]))
                f = sum(G[u][v]["fire"] for u, v in zip(path[:-1], path[1:]))
                paths.append({
                    "nodes": path,
                    "cost": round(cost, 3),
                    "factors": {
                        "distance": round(d, 3),
                        "crowd": round(c, 3),
                        "smoke": round(s, 3),
                        "fire": round(f, 3)
                    }
                })
            except nx.NetworkXNoPath:
                continue

        G.remove_node(start_node)  # clean up
        return jsonify({"paths": paths})

    except Exception as e:
        print("❌ Error:", e)
        return jsonify({"error": str(e)}), 500

if __name__ == "__main__":
    app.run(port=5001, debug=True)
