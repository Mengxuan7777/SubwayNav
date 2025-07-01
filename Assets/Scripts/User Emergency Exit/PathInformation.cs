namespace User_Emergency_Exit {
    [System.Serializable]
    public class PathInformation {
        public int ID;
        public float Distance;
        public float[] DistanceToFire;
        
        public PathInformation(int id, float distance, float[] distanceToFire) {
            ID = id;
            Distance = distance;
            DistanceToFire = distanceToFire;
        }
    }
}