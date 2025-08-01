namespace Utils {
    // Helper class to update edge costs on the graph
    public class GPTMessage {
        public string name;
        public int crowdCost;
        public int fireCost;
        
        // Constructor
        public GPTMessage(string name, int crowdCost, int fireCost) {
            this.name = name;
            this.crowdCost = crowdCost;
            this.fireCost = fireCost;
        }

    }
}