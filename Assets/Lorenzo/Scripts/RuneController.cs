using UnityEngine;
using System.Collections;

public class RuneController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySpeed = 5f; // How fast the rune flies to the wand target
    public float rotationSpeed = 180f; // How fast it spins while flying
    // Removed: public Transform targetWandPosition; // NEW: The specific target transform on the wand

    [Header("Effects")]
    public ParticleSystem trailParticles; // Particle system for the trail

    [Header("Absorption Animation")]
    public float shrinkDuration = 0.3f; // Duration of the shrinking animation
    public Vector3 endScale = Vector3.zero; // End scale for the shrinking animation

    private bool isFlyingToWand = false;
    private Vector3 startPosition;
    private float startTime;
    private Transform currentTargetWandPosition;
    private Rigidbody rb;
    private bool wasKinematic = false;
    private bool absorptionStarted = false; // NEW: Flag to track if absorption started

    void Start()
    {
        Debug.Log("[RuneController] Rune spawned at position: " + transform.position);

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("[RuneController] Found Rigidbody");
            wasKinematic = rb.isKinematic;
        }

        // Get the target wand position from the WandManager
        if (WandManager.Instance != null)
        {
            currentTargetWandPosition = WandManager.Instance.GetRuneTargetTransform();
            if (currentTargetWandPosition != null)
            {
                 Debug.Log("[RuneController] Using target wand position from WandManager: " + currentTargetWandPosition.position);
            }
            else
            {
                 Debug.LogError("[RuneController] WandManager exists but returned a null target transform!");
            }
        }
        else
        {
            Debug.LogError("[RuneController] No WandManager found in scene!");
        }

        // Set up particle system if it exists
        if (trailParticles != null)
        {
            Debug.Log("[RuneController] Found particle system, stopping it initially");
            trailParticles.Stop();
        }
        else
        {
            Debug.LogError("[RuneController] No particle system assigned!");
        }
    }

    // NEW: Check if arrived and force absorption if needed
    void Update()
    {
        if (isFlyingToWand && !absorptionStarted && currentTargetWandPosition != null)
        {
            // Check if the rune is very close to the target position
            if (Vector3.Distance(transform.position, currentTargetWandPosition.position) <= 0.02f) // Using a slightly larger threshold for the check
            {
                Debug.Log("[RuneController] Update: Detected close to target. Forcing absorption start.");
                StopAllCoroutines(); // Stop the flight coroutine
                transform.position = currentTargetWandPosition.position; // Ensure it's exactly at the target
                 // Restore original Rigidbody kinematic state if it wasn't kinematic initially
                if (rb != null && !wasKinematic)
                {
                     rb.isKinematic = wasKinematic;
                      Debug.Log("[RuneController] Update: Restored Rigidbody kinematic state");
                 }
                StartCoroutine(AbsorbAnimation());
                absorptionStarted = true;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("[RuneController] Trigger entered by: " + other.name + " (Tag: " + other.tag + ")");
        
        // Check if it's the specific target wand position or the main wand transform
        bool isWand = (currentTargetWandPosition != null && other.transform == currentTargetWandPosition) ||
                      (WandManager.Instance != null && WandManager.Instance.wandTransform != null && other.transform == WandManager.Instance.wandTransform);

        if (!isFlyingToWand && isWand)
        {
            Debug.Log("[RuneController] Wand (or wand tip) detected! Starting flight sequence");
            StartFlightToWand();
        }
    }

    void StartFlightToWand()
    {
        Debug.Log("[RuneController] StartFlightToWand called");
        if (currentTargetWandPosition == null)
        {
            Debug.LogError("[RuneController] No wand target available to fly to!");
            return;
        }

        // Disable Rigidbody physics during flight
        if (rb != null)
        {
            wasKinematic = rb.isKinematic; // Store original kinematic state
            rb.isKinematic = true;
            Debug.Log("[RuneController] Set Rigidbody to kinematic for flight");
        }

        Debug.Log("[RuneController] Starting flight to wand target at position: " + currentTargetWandPosition.position);
        isFlyingToWand = true;
        startPosition = transform.position;
        startTime = Time.time;
        absorptionStarted = false; // Ensure this is false when starting flight

        // Start the particle trail
        if (trailParticles != null)
        {
            Debug.Log("[RuneController] Starting particle trail");
            trailParticles.Play();
             if (!trailParticles.isPlaying)
            {
                Debug.LogError("[RuneController] Particle system failed to start!");
            }
        }

        // Start the flight coroutine
        StartCoroutine(FlyToTargetWandPosition());
        Debug.Log("[RuneController] Started FlyToTargetWandPosition coroutine");
    }

    private IEnumerator FlyToTargetWandPosition()
    {
        Debug.Log("[RuneController] FlyToTargetWandPosition coroutine started");
        Transform target = currentTargetWandPosition;
        float journeyLength = Vector3.Distance(startPosition, target.position);
        Debug.Log("[RuneController] Flight distance: " + journeyLength + " units");
        float flightStartTime = Time.time; // Use a different name to avoid conflict if needed

        while (true) // Changed to an infinite loop, we will break out manually
        {
            float elapsed = Time.time - flightStartTime;
            float fractionOfJourney = elapsed * flySpeed / journeyLength;

            // Move the rune using Lerp
            transform.position = Vector3.Lerp(startPosition, target.position, fractionOfJourney);
            
            // Rotate the rune
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            float currentDistance = Vector3.Distance(transform.position, target.position);

            // Debug log to see if the flight loop is running
            if (Time.frameCount % 30 == 0) // Log less frequently to avoid spam
            {
                 Debug.Log("[RuneController] Flying... Distance to target: " + currentDistance);
            }

            // Check if we are close enough to the target to break and start absorption
            if (currentDistance <= 0.01f)
            {
                Debug.Log("[RuneController] Close enough to target, breaking flight loop.");
                break; // Exit the while loop
            }

            yield return null;
        }

        // This code should now always be reached after the break
        Debug.Log("[RuneController] Exited flight while loop.");

        Debug.Log("[RuneController] Reached wand target position! (End of Coroutine)");
        // Ensure we reach the exact target position
        transform.position = target.position; // Force position to the target

        // Restore original Rigidbody kinematic state
         if (rb != null)
        {
            rb.isKinematic = wasKinematic; // Restore original kinematic state
             Debug.Log("[RuneController] Restored Rigidbody kinematic state (End of Coroutine)");
        }

        // Start the absorption animation
        Debug.Log("[RuneController] Calling StartCoroutine(AbsorbAnimation()) (End of Coroutine)");
        StartCoroutine(AbsorbAnimation());
        absorptionStarted = true; // Set the flag here too, in case the Update check isn't needed

        // Stop the particle trail
        if (trailParticles != null)
        {
            Debug.Log("[RuneController] Stopping particle trail (End of Coroutine)");
            trailParticles.Stop();
        }

        // Destroy the rune will happen after absorption animation
    }

    private IEnumerator AbsorbAnimation()
    {
        Debug.Log("[RuneController] Starting absorption animation coroutine");
        Vector3 initialScale = transform.localScale;
        float animationStartTime = Time.time;

        while (Time.time < animationStartTime + shrinkDuration)
        {
            float elapsed = Time.time - animationStartTime;
            float t = elapsed / shrinkDuration;

            // Lerp the scale
            transform.localScale = Vector3.Lerp(initialScale, endScale, t);

            // Debug log to see if the animation loop is running
             if (Time.frameCount % 10 == 0) // Log less frequently
            {
                 Debug.Log("[RuneController] Absorbing... current scale: " + transform.localScale);
            }

            yield return null;
        }

        // Ensure final scale is set
        transform.localScale = endScale;

        Debug.Log("[RuneController] Absorption animation complete, destroying rune");
        // Destroy the rune after the animation
        Destroy(gameObject);
    }
} 