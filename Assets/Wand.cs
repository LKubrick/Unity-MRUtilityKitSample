using UnityEngine;

public class Wand : MonoBehaviour
{
    public Transform launchPoint;
    public GameObject projectilePrefab;
    private Vector3 prevPosition;
    private Vector3 prevRotation;
    private float movementThreshold = 0.5f; // Threshold for position change
    private float rotationThreshold = 2.50f; // Lowered threshold for easier wrist flicks
    private float spellCooldown = 0.70f; // Increased cooldown
    private bool spellOnCooldown = false;
    private float lastSpellTime;
    public float force = 1f; // Force to apply to projectiles
    public float spellSpeed = 1f; // New constant spell speed

    void Start()
    {
        prevPosition = transform.position;
        prevRotation = transform.eulerAngles;
        lastSpellTime = Time.time;
    }

    void Update()
    {
        // Check for spell casting condition
        Vector3 currentPosition = transform.position;
        Vector3 currentRotation = transform.eulerAngles;
        
        Vector3 positionChange = currentPosition - prevPosition;
        Vector3 rotationChange = currentRotation - prevRotation;
        
        // Ensure rotation changes stay within the range of -180 to 180 degrees
        for (int i = 0; i < 3; i++)
        {
            if (rotationChange[i] > 180)
                rotationChange[i] -= 360;
            else if (rotationChange[i] < -180)
                rotationChange[i] += 360;
        }

        float movementMagnitude = positionChange.magnitude;
        float rotationMagnitude = rotationChange.magnitude;

        prevPosition = currentPosition;
        prevRotation = currentRotation;

        if (!spellOnCooldown)
        {
            bool shouldCast = false;

            // Check for arm movement
            if (movementMagnitude > movementThreshold && IsCastingGesture(positionChange))
            {
                shouldCast = true;
            }
            // Check for wrist rotation
            else if (rotationMagnitude > rotationThreshold && IsRotationGesture(rotationChange))
            {
                shouldCast = true;
            }

            if (shouldCast)
            {
                CastSpell();
                spellOnCooldown = true;
                lastSpellTime = Time.time;
            }
        }

        // Reset cooldown
        if (spellOnCooldown && Time.time > lastSpellTime + spellCooldown)
        {
            spellOnCooldown = false;
        }
    }

    bool IsCastingGesture(Vector3 positionChange)
    {
        // Get the forward direction of the wand
        Vector3 wandForward = transform.forward;
        
        // Project the movement onto the forward direction
        float forwardMovement = Vector3.Dot(positionChange, wandForward);
        
        // Return true if the movement is primarily forward
        return forwardMovement > 0;
    }

    bool IsRotationGesture(Vector3 rotationChange)
    {
        // Check if the rotation is primarily around the X-axis (wrist flick)
        float wristRotation = Mathf.Abs(rotationChange.x);
        float totalRotation = rotationChange.magnitude;
        
        // Return true if at least 50% of the rotation is from the wrist
        return wristRotation / totalRotation > 0.5f;
    }

    void CastSpell()
    {
        // Calculate target point 2 meters in front of camera
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 targetPoint = cameraPosition + (cameraForward * 4f);
        
        // Calculate direction from launch point to target point
        Vector3 spellDirection = (targetPoint - launchPoint.position).normalized;
        ShootProjectile(spellDirection);
    }

    void ShootProjectile(Vector3 direction)
    {
        // Spawn at launch point but use calculated direction
        GameObject spell = Instantiate(projectilePrefab, launchPoint.position, Quaternion.LookRotation(direction));
        
        Rigidbody rb = spell.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spell.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.mass = 0.1f;
        // Use constant spell speed
        rb.AddForce(direction * force * spellSpeed, ForceMode.Impulse);
    }
}
