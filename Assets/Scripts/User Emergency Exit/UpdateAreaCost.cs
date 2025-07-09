using System.Collections;
using UnityEngine;

public class UpdateAreaCost : MonoBehaviour {
    // Settings
    public string areaName;
    public bool EnableOnEntry = true, EnableOnExit = true;
    
    // Data storage
    private const int crowd = 0,  fire = 1, fireCost = 25;
    private readonly float[] AreaCost = new float[2];
    
    // Start is called before the first frame update
    void Start() {
        // Set area costs to 1
        AreaCost[crowd] = 1f;
        AreaCost[fire] = 1f;
        
        // Testing
        //StartCoroutine(UpdateAreaCostRoutine());
    }
    
    private void OnTriggerEnter(Collider other) {
        // Increase the area cost when pedestrians trigger it.  
        if (EnableOnEntry) {
            var layer = other.gameObject.layer;
            Debug.Log($"{name}: In contact with later {layer}");
            if (layer == 8) {
                // Increase cost if there is a fire
                var fireInfo = other.gameObject.GetComponent<FireInformation>();
                float cost = AreaCost[crowd];
                AreaCost[crowd] = cost + fireInfo.FireSize * fireCost;
            } else if (layer == 9) {
                // Increase cost if there is a pedestrian
                float cost = AreaCost[crowd];
                AreaCost[crowd] = cost + 0.1f;
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        // Decrease the area cost for this
        if (EnableOnExit) {
            var layer = other.gameObject.layer;
            if (layer == 8) {
                // Decrease cost if there is a fire
                var fireInfo = other.gameObject.GetComponent<FireInformation>();
                float cost = AreaCost[crowd];
                AreaCost[crowd] = cost - fireInfo.FireSize * fireCost;
            } else if (layer == 9) {
                // Decrease cost if there is a pedestrian
                float cost = AreaCost[crowd];
                AreaCost[crowd] = cost - 0.1f;
            }
        }
    }
    
    /// <summary>
    /// Returns the area costs for crowd and fire
    /// </summary>
    /// <returns></returns>
    public float[] GetAreaCost() {
        return AreaCost;
    }
    
    // For testing purposes
    private IEnumerator UpdateAreaCostRoutine() {

        while (true) {
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"{name}: {AreaCost[crowd]}, {AreaCost[fire]}");
        }
    }
    
}
