using System;
using UnityEngine;
using UnityEngine.AI;

public class UpdateAreaCost : MonoBehaviour {
    // Settings
    public string areaName;
    private static int AreaIndex;
    public bool EnableOnEntry = true, EnableOnExit = true;
    
    // Start is called before the first frame update
    void Start() {
        //Get index of area
        AreaIndex = NavMesh.GetAreaFromName(areaName);
    }
    
    private void OnTriggerEnter(Collider other) {
        // Increase the area cost when pedestrians trigger it.  
        
        if (EnableOnEntry) {
            var layer = other.gameObject.layer;
            if (layer == 8) {
                // Increase cost if there is a fire
                // NavMesh.GetAreaCost(NavMesh.GetAreaFromName(areaName));
                float cost = NavMesh.GetAreaCost(AreaIndex);
                NavMesh.SetAreaCost(AreaIndex, cost + 5f);
            } else if (layer == 9) {
                // Increase cost if there is a pedestrian
                float cost = NavMesh.GetAreaCost(AreaIndex);
                NavMesh.SetAreaCost(AreaIndex, cost + 0.1f);
            }
        }
        
    }

    private void OnTriggerExit(Collider other) {
        // Decrease the area cost for this
        if (EnableOnExit) {
            var layer = other.gameObject.layer;
            if (layer == 8) {
                // Increase cost if there is a fire
                float cost = NavMesh.GetAreaCost(AreaIndex);
                NavMesh.SetAreaCost(AreaIndex, cost - 5f);
            } else if (layer == 9) {
                // Increase cost if there is a pedestrian
                float cost = NavMesh.GetAreaCost(AreaIndex);
                NavMesh.SetAreaCost(AreaIndex, cost - 0.1f);
            }
        }
        
    }
}
