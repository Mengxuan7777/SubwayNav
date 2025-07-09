using UnityEngine;
using Random = System.Random;
public class FireInformation : MonoBehaviour {
    // Fire Information
    public float FireSize = 1f;
    
    // Random Number Generator
    private readonly Random rnd = new Random();
    
    void Start() {
        // Get random float between 0.1-1.5
        int rndFloat = rnd.Next(1,10);
        FireSize = rndFloat / 10f;
        
        // Scale up fire
        gameObject.transform.localScale = new Vector3(FireSize, FireSize, FireSize);
    }

    
}
