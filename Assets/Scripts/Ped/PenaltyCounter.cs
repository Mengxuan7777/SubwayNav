using UnityEngine;

public class PenaltyCounter : MonoBehaviour {
    // Start is called before the first frame update
    [Header("Penalty Counters")]
    public int collisionWithFire, collisionWithPedestrian;
    
    // Increase counters it collides with a fire or 
    private void OnTriggerEnter(Collider other) {
        var layer = other.gameObject.layer;
        if (layer == 8) {
            collisionWithFire++;
        } else if (layer == 9) {
            collisionWithPedestrian++;
        }
    }
}
