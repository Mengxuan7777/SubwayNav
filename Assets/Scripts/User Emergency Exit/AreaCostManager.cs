using UnityEngine;
using Newtonsoft.Json;

public class AreaCostManager : MonoBehaviour {
    public UpdateAreaCost[] areas;
    
    // Serializes all the area costs into a JSON string
    // The order of the costs is the same as the order of nodes in the nodes 
    // in the hierarchy. 
    public string SerializeAreas() {
        return JsonConvert.SerializeObject(areas);
    }


}
