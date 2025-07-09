using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class TrainArrivalDisplay : MonoBehaviour
{
    public string apiUrl = "http://127.0.0.1:5000/arrivals"; // Flask API
    public TextMeshProUGUI arrivalText; // Assign in Inspector

    void Start()
    {
        InvokeRepeating("RefreshTrains", 0f, 30f); // Refresh every 30 seconds
    }

    void RefreshTrains()
    {
        StartCoroutine(FetchTrainArrivals());
    }

    IEnumerator FetchTrainArrivals()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching data: " + request.error);
            arrivalText.text = "Error loading train data.";
            yield break;
        }

        string json = FixJson(request.downloadHandler.text);
        ArrivalData data = JsonUtility.FromJson<ArrivalData>(json);

        // Build text output
        string output = "Upcoming Trains at 125 St\n\n";

        output += "Uptown:\n";
        foreach (Train t in data.Northbound)
        {
            output += $"  {t.route} - {t.in_minutes}\n";
        }

        output += "\n Downtown:\n";
        foreach (Train t in data.Southbound)
        {
            output += $"  {t.route} - {t.in_minutes}\n";
        }

        arrivalText.text = output;
    }

    // JsonUtility requires wrapping arrays in objects
    string FixJson(string raw)
    {
        return "{\"Northbound\":" + JsonHelper.ExtractArray(raw, "Northbound") +
               ",\"Southbound\":" + JsonHelper.ExtractArray(raw, "Southbound") + "}";
    }

    [System.Serializable]
    public class ArrivalData
    {
        public Train[] Northbound;
        public Train[] Southbound;
    }

    [System.Serializable]
    public class Train
    {
        public string route;
        public string arrival_time;
        public string in_minutes;
    }
}
