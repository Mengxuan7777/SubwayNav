using System.Collections;
using UnityEngine;
using User_Emergency_Exit;

public class UpdateAreaCost : MonoBehaviour {
    // Settings
    public string areaName;
    public bool EnableOnEntry = true, EnableOnExit = true;
    
    // Data storage
    private const int fireCost = 25;
    private AreaInformation Area;
    
    // Start is called before the first frame update
    void Start() {
        // Initialize Area
        Area = new AreaInformation(areaName);  
        
        // Testing
        //StartCoroutine(UpdateAreaCostRoutine());
    }
    
    private void OnTriggerEnter(Collider other) {
        // Increase the area cost when pedestrians trigger it.  
        if (EnableOnEntry) {
            var layer = other.gameObject.layer;
            //Debug.Log($"{name}: In contact with later {layer}");
            if (layer == 8) {
                // Increase cost if there is a fire
                var fireInfo = other.gameObject.GetComponent<FireInformation>();
                float cost = Area.FireCost;
                Area.FireCost = cost + fireInfo.FireSize * fireCost;
            } else if (layer == 9) {
                // Increase cost if there is a pedestrian
                float cost = Area.CrowdCost;
                Area.CrowdCost = cost + 0.1f;
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
                float cost = Area.FireCost;
                Area.FireCost = cost - fireInfo.FireSize * fireCost;
            } else if (layer == 9) {
                // Decrease cost if there is a pedestrian
                float cost = Area.CrowdCost;
                Area.CrowdCost = cost - 0.1f;
            }
        }
    }
    
    /// <summary>
    /// Returns the area costs for crowd and fire
    /// </summary>
    /// <returns></returns>
    public AreaInformation GetAreaCost() {
        return Area;
    }
    
    // For testing purposes
    private IEnumerator UpdateAreaCostRoutine() {
        while (true) {
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"{Area.AreaName}: {Area.CrowdCost}, {Area.FireCost}");
        }
    }
    
}
