import requests
from google.transit import gtfs_realtime_pb2
from datetime import datetime
from collections import defaultdict
import json

# Constants
STOP_IDS = ["A15N", "A15S"]  # 125 St, Northbound/Southbound
FEED_URLS = [
    "https://api-endpoint.mta.info/Dataservice/mtagtfsfeeds/nyct%2Fgtfs-bdfm",
    "https://api-endpoint.mta.info/Dataservice/mtagtfsfeeds/nyct%2Fgtfs-ace"
]

def unix_to_readable(timestamp):
    """Convert Unix timestamp to HH:MM:SS"""
    return datetime.fromtimestamp(timestamp).strftime('%H:%M:%S')

def minutes_from_now(arrival_str):
    """Convert HH:MM:SS to 'in X min' or 'departed'"""
    now = datetime.now()
    try:
        arrival_time = datetime.strptime(arrival_str, "%H:%M:%S").replace(
            year=now.year, month=now.month, day=now.day)
        delta = (arrival_time - now).total_seconds()
        diff = int(delta // 60)
        if diff < 0:
            return "departed"
        elif diff == 0:
            return "in 0 min"
        else:
            return f"in {diff} min"
    except Exception:
        return "unknown"

def fetch_next_trains(limit_per_direction=2):
    """Fetch and return next N trains per direction"""
    all_arrivals = {"Northbound": [], "Southbound": []}

    for url in FEED_URLS:
        try:
            response = requests.get(url)
            response.raise_for_status()
        except Exception as e:
            print(f"Error fetching {url}: {e}")
            continue

        feed = gtfs_realtime_pb2.FeedMessage()
        feed.ParseFromString(response.content)

        for entity in feed.entity:
            if not entity.HasField('trip_update'):
                continue

            trip = entity.trip_update.trip
            for stu in entity.trip_update.stop_time_update:
                if stu.stop_id in STOP_IDS and stu.HasField("arrival"):
                    stop_id = stu.stop_id
                    direction = "Northbound" if stop_id.endswith("N") else "Southbound"
                    arrival_time = unix_to_readable(stu.arrival.time)
                    minutes_text = minutes_from_now(arrival_time)

                    # Skip trains that already departed
                    if minutes_text == "departed":
                        continue

                    all_arrivals[direction].append({
                        "route": trip.route_id,
                        "arrival_time": arrival_time,
                        "in_minutes": minutes_text
                    })

    # Sort and trim to next N per direction
    result = {}
    for direction in all_arrivals:
        all_arrivals[direction].sort(key=lambda x: x["arrival_time"])
        result[direction] = all_arrivals[direction][:limit_per_direction]

    return result

if __name__ == "__main__":
    arrivals = fetch_next_trains(limit_per_direction=2)
    print(json.dumps(arrivals, indent=2))
