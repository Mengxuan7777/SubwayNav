namespace User_Emergency_Exit {
    [System.Serializable]
    public class AreaInformation {
        
        // Area Cost Information
        public string AreaName;
        public float CrowdCost;
        public float FireCost;

        public AreaInformation(string name) {
            AreaName = name;
            CrowdCost = 1;
            FireCost = 1;
        }
    }
}