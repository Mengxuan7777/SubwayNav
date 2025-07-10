using Newtonsoft.Json;
using UnityEngine;

namespace User_Emergency_Exit {
    public class AreaCostManager : MonoBehaviour {
        public UpdateAreaCost[] areas;
    
        // Serializes all the area costs into a JSON string
        // The order of the costs is the same as the order of nodes in the nodes 
        // in the hierarchy. 
        public string SerializeAreas() {
            // Get all the area cost info for all locations
            AreaInformation[] areaInformation = new AreaInformation[areas.Length];
            for (int i = 0; i < areas.Length; i++) {
                areaInformation[i] = areas[i].GetAreaCost();
            }
            return JsonConvert.SerializeObject(areaInformation);
        }

        private void Start() {
            //StartCoroutine(Test());
        }
        /*
        private IEnumerator Test() {
            yield return new WaitForSeconds(1f);
            Debug.Log(SerializeAreas());
        }
        */
    }
}
