using UnityEngine;

public class Poison : MonoBehaviour
{
    // Assign this in the Inspector with your particle prefab
    public GameObject particleEffectPrefab;

    // Tag or layer for objects to trigger the effect
    public string targetTag = "Plane";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the specified tag
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Instantiate the particle effect at the collision point
            Instantiate(particleEffectPrefab, collision.contacts[0].point, Quaternion.identity);
        }
    }
}
