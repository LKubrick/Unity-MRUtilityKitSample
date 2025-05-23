using UnityEngine;

public class Monster : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float floatAmplitude = 1f;
    public float floatFrequency = 1f;
    public float shootInterval = 2f;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    private float shootTimer = 0f;
    private Vector3 startPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Flying movement (sine wave)
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        // transform.position += Vector3.right * moveSpeed * Time.deltaTime; // Removed horizontal movement
        transform.position = new Vector3(startPos.x, newY, transform.position.z);

        // Shooting
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootInterval)
        {
            ShootAtPlayer();
            shootTimer = 0f;
        }
    }

    void ShootAtPlayer()
    {
        if (projectilePrefab == null) return;
        Camera mainCam = Camera.main;
        if (mainCam == null) return;
        Vector3 targetPos = mainCam.transform.position;
        Vector3 dir = (targetPos - transform.position).normalized;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        // Ensure the projectile has a Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = proj.AddComponent<Rigidbody>();
            rb.useGravity = false; // Optional: projectiles usually don't use gravity
        }

        rb.linearVelocity = dir * projectileSpeed;
    }
}
