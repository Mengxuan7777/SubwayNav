from flask import Flask, jsonify
from mta_gtfs import fetch_next_trains

app = Flask(__name__)

@app.route("/arrivals", methods=["GET"])
def arrivals():
    result = fetch_next_trains(limit_per_direction=2)
    return jsonify(result)

if __name__ == "__main__":
    app.run(debug=True, port=5000)
