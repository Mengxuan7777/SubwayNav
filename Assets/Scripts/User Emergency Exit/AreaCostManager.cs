using UnityEngine;
using Newtonsoft.Json;

public class AreaCostManager : MonoBehaviour {
    public UpdateAreaCost[] areas;

    public string SerializeAreas() {
        return JsonConvert.SerializeObject(areas);
    }


}
