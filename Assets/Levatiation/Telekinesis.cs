using UnityEngine;
using System.Collections;

public class Telekenises : MonoBehaviour
{
    public Transform launchPoint;
    public GameObject projectilePrefab;
    private Vector3 prevPosition;
    private Vector3 prevRotation;
    private float movementThreshold = 0.5f; // Threshold for position change
    private float rotationThreshold = 3.3f; // Less sensitive to wrist flicks
    private float spellCooldown = 0.70f; // Increased cooldown
    private bool spellOnCooldown = false;
    private float lastSpellTime;
    public float force = 0.1f; // Reduced from 0.5f to 0.1f
    public float spellSpeed = 0.2f; // Reduced from 0.5f to 0.2f
    public float castRange = 1f; // Changed to 1 meter
    public float grabLerpSpeed = 8f; // Increased for faster response
    public float minGrabDistance = 0.5f; // Minimum distance when tilted towards
    public float maxGrabDistance = 1.5f; // Maximum distance when tilted away
    public float distanceLerpSpeed = 5f; // Speed of distance changes
    private float currentGrabDistance = 1f; // Current target distance
    public float tiltSensitivity = 0.2f; // New parameter for tilt sensitivity
    public AudioClip hoverClip;
    public AudioClip grabClip;
    public AudioClip tossClip;
    private AudioSource audioSource;
    private Camera mainCamera;

    private LineRenderer lineRenderer; // LineRenderer for the trail
    private GameObject grabbedObject = null;
    private Vector3 grabOffset = Vector3.zero;
    private Material grabbedOriginalMaterial = null;
    private Vector3 grabbedLocalOffset = Vector3.zero;
    private GameObject hoveredObject = null;
    private Vector3 hoveredOriginalScale = Vector3.one;
    private float hoverAnimTime = 0f;
    private bool wasGravityEnabled = false;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            enabled = false;
            return;
        }

        prevPosition = transform.position;
        prevRotation = transform.eulerAngles;
        lastSpellTime = Time.time;

        // Initialize LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        // Configure LineRenderer appearance
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.01f; // Very narrow
        lineRenderer.endWidth = 0.003f; // Even narrower at the end
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        Color subtleGray = new Color(0.7f, 0.7f, 0.7f, 0.25f); // Light gray, low alpha
        lineRenderer.startColor = subtleGray;
        lineRenderer.endColor = subtleGray;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = true; // Make visible

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Set default volume for all sounds
        if (audioSource != null)
        {
            audioSource.volume = 0.3f; // Set default volume to 30%
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Check for fist position to release object without force
        if (grabbedObject != null && IsFistPosition())
        {
            ReleaseObject(false);
            return;
        }

        // Only show line and hover when not grabbing
        if (grabbedObject == null)
        {
            // Enable the line
            if (lineRenderer != null) lineRenderer.enabled = true;

            // Draw the trail from launchPoint to target point
            if (launchPoint != null)
            {
                Vector3 cameraPosition = mainCamera.transform.position;
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 targetPoint = cameraPosition + (cameraForward * castRange);
                lineRenderer.SetPosition(0, launchPoint.position);
                lineRenderer.SetPosition(1, targetPoint);
            }

            // HOVER EFFECT: Animate the object being pointed at
            GameObject pointed = GetPointedObject();
            if (pointed != null)
            {
                if (hoveredObject != pointed)
                {
                    // Play hover sound
                    if (hoverClip != null && audioSource != null)
                        audioSource.PlayOneShot(hoverClip);

                    // Restore previous hovered object's scale
                    if (hoveredObject != null)
                        hoveredObject.transform.localScale = hoveredOriginalScale;
                    hoveredObject = pointed;
                    hoveredOriginalScale = hoveredObject.transform.localScale;
                    hoverAnimTime = 0f; // Reset animation time for new object
                }
                // Animate scale between 0.85x and 1.15x
                hoverAnimTime += Time.deltaTime * 1.5f; // Slower animation speed
                float scaleMultiplier = 1.0f + 0.15f * Mathf.Sin(hoverAnimTime * Mathf.PI); // Oscillates between 0.85 and 1.15
                hoveredObject.transform.localScale = hoveredOriginalScale * scaleMultiplier;
            }
            else if (hoveredObject != null)
            {
                // Restore scale if no longer hovered
                hoveredObject.transform.localScale = hoveredOriginalScale;
                hoveredObject = null;
            }
        }
        else // grabbedObject != null
        {
            // Disable the line
            if (lineRenderer != null) lineRenderer.enabled = false;
            
            // Restore hovered object scale if needed
            if (hoveredObject != null)
            {
                hoveredObject.transform.localScale = hoveredOriginalScale;
                hoveredObject = null;
            }

            // Only update position for non-book objects
            if (grabbedObject != null)
            {
                // Update grab distance based on wand tilt
                UpdateGrabDistance();

                // Move the grabbed object with the wand
                if (launchPoint != null)
                {
                    // Calculate target position in front of the camera
                    Vector3 cameraPosition = mainCamera.transform.position;
                    Vector3 cameraForward = mainCamera.transform.forward;
                    Vector3 targetPos = cameraPosition + (cameraForward * currentGrabDistance);
                    
                    // Add a slight upward offset to make it hover
                    targetPos += Vector3.up * 0.2f;
                    
                    // Smoothly move the grabbed object
                    grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, targetPos, Time.deltaTime * grabLerpSpeed);
                    
                    // Add a slight rotation to make it look more magical
                    grabbedObject.transform.Rotate(Vector3.up, Time.deltaTime * 30f);
                }
            }
        }

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
        // If already holding an object, release it with force
        if (grabbedObject != null)
        {
            ReleaseObject(true);
            return;
        }

        // Try to grab an object
        GameObject pointed = GetPointedObject();
        if (pointed != null)
        {
            // Only grab objects with Rigidbody
            Rigidbody rb = pointed.GetComponent<Rigidbody>();
            if (rb != null)
            {
                grabbedObject = pointed;
                wasGravityEnabled = rb.useGravity;
                rb.useGravity = false;
                
                // Play grab sound
                if (grabClip != null && audioSource != null)
                    audioSource.PlayOneShot(grabClip);
            }
        }
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
        if (tossClip != null && audioSource != null)
        {
            audioSource.volume = 0.3f; // Reduce volume to 30%
            audioSource.PlayOneShot(tossClip);
        }
    }

    GameObject GetPointedObject()
    {
        RaycastHit hit;
        if (Physics.Raycast(launchPoint.position, launchPoint.forward, out hit, castRange))
        {
            // Only return objects with Rigidbody
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                return hit.collider.gameObject;
            }
        }
        return null;
    }

    // Helper to get the current rotation change (for IsRotationGesture)
    Vector3 GetRotationChange()
    {
        Vector3 currentRotation = transform.eulerAngles;
        Vector3 rotationChange = currentRotation - prevRotation;
        for (int i = 0; i < 3; i++)
        {
            if (rotationChange[i] > 180)
                rotationChange[i] -= 360;
            else if (rotationChange[i] < -180)
                rotationChange[i] += 360;
        }
        return rotationChange;
    }

    void UpdateGrabDistance()
    {
        // Get the angle between wand's forward direction and camera's forward direction
        float angle = Vector3.Angle(transform.forward, mainCamera.transform.forward);
        
        // Calculate target distance based on angle with more sensitivity
        float targetDistance;
        if (angle > 90f)
        {
            // Wand pointing towards camera - closer distance
            // More sensitive: use a smaller angle range
            float t = Mathf.Clamp01((angle - 90f) / (90f * tiltSensitivity));
            targetDistance = Mathf.Lerp(castRange, minGrabDistance, t);
        }
        else
        {
            // Wand pointing away from camera - further distance
            // More sensitive: use a smaller angle range
            float t = Mathf.Clamp01(angle / (90f * tiltSensitivity));
            targetDistance = Mathf.Lerp(maxGrabDistance, castRange, t);
        }

        // Smoothly interpolate current distance to target distance
        currentGrabDistance = Mathf.Lerp(currentGrabDistance, targetDistance, Time.deltaTime * distanceLerpSpeed);
    }

    bool IsFistPosition()
    {
        // Check if the wand is in a fist-like position
        // This is a simple check - you might want to adjust these values
        float angle = Vector3.Angle(transform.forward, mainCamera.transform.forward);
        return angle > 150f; // Wand pointing almost directly at camera
    }

    void ReleaseObject(bool applyForce)
    {
        if (grabbedObject == null) return;

        // Handle physics
        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = wasGravityEnabled;
            
            if (applyForce)
            {
                // Calculate force based on wand movement
                Vector3 forceDirection = transform.forward;
                rb.AddForce(forceDirection * force, ForceMode.Impulse);
                
                // Play toss sound
                if (tossClip != null && audioSource != null)
                    audioSource.PlayOneShot(tossClip);
            }
        }

        grabbedObject = null;
    }

    void OnDisable()
    {
        // Restore original material if needed
        if (grabbedObject != null && grabbedOriginalMaterial != null && grabbedObject.GetComponent<Renderer>() != null)
        {
            grabbedObject.GetComponent<Renderer>().material = grabbedOriginalMaterial;
        }
        // Restore hovered object scale if needed
        if (hoveredObject != null)
        {
            hoveredObject.transform.localScale = hoveredOriginalScale;
            hoveredObject = null;
        }
        // Restore grabbed object scale if needed
        if (grabbedObject != null)
        {
            grabbedObject.transform.localScale = hoveredOriginalScale;
        }
    }

    void SetWorldScale(Transform target, Vector3 worldScale)
    {
        if (target.parent == null)
        {
            target.localScale = worldScale;
        }
        else
        {
            Vector3 parentScale = target.parent.lossyScale;
            target.localScale = new Vector3(
                worldScale.x / parentScale.x,
                worldScale.y / parentScale.y,
                worldScale.z / parentScale.z
            );
        }
    }
}
