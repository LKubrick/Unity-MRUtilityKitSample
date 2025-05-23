using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject prefab; // Assign this in the Inspector
    private float spawnTimer = 0f;
    public float spawnInterval = 1f; // seconds
    public float force = 2f; // Adjust force in Inspector

    // Update is called once per frame
    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            GameObject spawned = Instantiate(prefab, transform.position, Quaternion.identity);
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = spawned.AddComponent<Rigidbody>();
            }
            rb.AddForce(transform.forward * force, ForceMode.Impulse);
            spawnTimer = 0f;
        }
    }
}
