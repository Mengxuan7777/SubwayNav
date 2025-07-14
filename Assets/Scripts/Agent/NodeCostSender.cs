using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using User_Emergency_Exit; // Required to access AreaCostManager

public class NodeCostSender : MonoBehaviour
{
    public AreaCostManager costManager;
    public float updateInterval = 15f; // Seconds between sends
    public string serverUrl = "http://localhost:5000/update_node_costs";

    private void Start()
    {
        if (costManager == null)
        {
            costManager = GetComponent<AreaCostManager>();
        }

        if (costManager != null)
        {
            StartCoroutine(SendNodeCostsRoutine());
        }
        else
        {
            Debug.LogError("❌ NodeCostSender: No AreaCostManager found!");
        }
    }

    private IEnumerator SendNodeCostsRoutine()
    {
        yield return new WaitForSeconds(1f);
        while (true)
        {
            string json = costManager.SerializeAreas();
            Debug.Log($"📤 Sending node costs:\n{json}");

            using (UnityWebRequest req = UnityWebRequest.Put(serverUrl, json))
            {
                req.method = UnityWebRequest.kHttpVerbPOST;
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"❌ Failed to send node costs: {req.error}");
                }
                else
                {
                    Debug.Log($"✅ Node costs sent at {System.DateTime.Now:T}");
                }
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }
}
