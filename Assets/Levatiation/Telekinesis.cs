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
    public float force = 1f; // Force to apply to projectiles
    public float spellSpeed = 1f; // New constant spell speed
    public float castRange = 8f; // Adjustable cast range for grabbing
    public float grabLerpSpeed = 3.6f; // 3/5 of previous speed for magnet effect
    public float grabOffsetDistance = 3f; // Offset in front of wand tip
    public AudioClip hoverClip;
    public AudioClip grabClip;
    public AudioClip tossClip;
    private AudioSource audioSource;

    private LineRenderer lineRenderer; // LineRenderer for the trail
    private GameObject grabbedObject = null; // The currently grabbed object
    private Vector3 grabOffset = Vector3.zero; // Offset from the grab point to the object's center
    private Material grabbedOriginalMaterial = null;
    private Vector3 grabbedLocalOffset = Vector3.zero;
    private GameObject hoveredObject = null;
    private Vector3 hoveredOriginalScale = Vector3.one;
    private float hoverAnimTime = 0f; // Animation timer for hover effect
    private Vector3 grabbedOriginalScale = Vector3.one;

    void Start()
    {
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
    }

    void Update()
    {
        // Only show line and hover when not grabbing
        if (grabbedObject == null)
        {
            // Enable the line
            if (lineRenderer != null) lineRenderer.enabled = true;

            // Draw the trail from launchPoint to target point (mechanics only, not visible)
            if (launchPoint != null && Camera.main != null)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                Vector3 cameraForward = Camera.main.transform.forward;
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
        }

        // Move the grabbed object with the wand
        if (grabbedObject != null && launchPoint != null)
        {
            // Smoothly move the grabbed object toward a point in front of the wand tip (magnet effect)
            Vector3 targetPos = launchPoint.position + launchPoint.forward * grabOffsetDistance;
            grabbedObject.transform.position = Vector3.Lerp(grabbedObject.transform.position, targetPos, Time.deltaTime * grabLerpSpeed);
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
        // If already holding an object, release it (toss)
        if (grabbedObject != null)
        {
            // Restore scale before releasing
            grabbedObject.transform.localScale = grabbedOriginalScale;

            Debug.Log("Grabbed object: " + grabbedObject.name + " | Scale: " + grabbedObject.transform.localScale);

            Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // Ensure physics is enabled
                Vector3 shootCameraPosition = Camera.main.transform.position;
                Vector3 shootCameraForward = Camera.main.transform.forward;
                Vector3 shootTargetPoint = shootCameraPosition + (shootCameraForward * castRange);
                Vector3 shootDirection = (shootTargetPoint - launchPoint.position).normalized;
                rb.useGravity = false; // Do not let it fall after being shot
                rb.AddForce(shootDirection * force * spellSpeed * 20f, ForceMode.Impulse); // Double the multiplier for more force
                if (tossClip != null && audioSource != null)
                    audioSource.PlayOneShot(tossClip); // Only toss sound here
            }
            grabbedObject = null;
            grabbedOriginalMaterial = null;
            grabOffset = Vector3.zero;
            grabbedLocalOffset = Vector3.zero;
            return;
        }

        // Calculate target point castRange meters in front of camera
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 targetPoint = cameraPosition + (cameraForward * castRange);

        // Raycast from launchPoint to targetPoint
        RaycastHit hit;
        Vector3 direction = (targetPoint - launchPoint.position).normalized;
        float distance = Vector3.Distance(launchPoint.position, targetPoint);
        if (Physics.Raycast(launchPoint.position, direction, out hit, distance))
        {
            if (hit.collider != null && hit.collider.attachedRigidbody != null && grabbedObject == null)
            {
                grabbedObject = hit.collider.gameObject;
                grabbedLocalOffset = Vector3.zero;
                hit.collider.attachedRigidbody.useGravity = false;
                Renderer rend = grabbedObject.GetComponent<Renderer>();
                if (rend != null)
                {
                    grabbedOriginalMaterial = rend.material;
                }
                // If the grabbed object is currently hovered, stop the hover effect
                if (hoveredObject == grabbedObject)
                {
                    hoveredObject.transform.localScale = hoveredOriginalScale; // Restore to original before grab
                    hoveredObject = null;
                }
                // Store original scale and set to 20%
                grabbedOriginalScale = grabbedObject.transform.localScale;
                SetWorldScale(grabbedObject.transform, Vector3.one * 0.2f);

                Debug.Log("Grabbed object: " + grabbedObject.name + " | Scale: " + grabbedObject.transform.localScale);

                if (grabClip != null && audioSource != null)
                    audioSource.PlayOneShot(grabClip); // Only grab sound here
            }
        }
        // If nothing is hit, do nothing
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
            audioSource.PlayOneShot(tossClip);
    }

    // Helper to get the object currently pointed at by the trail
    GameObject GetPointedObject()
    {
        if (launchPoint == null || Camera.main == null) return null;
        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 targetPoint = cameraPosition + (cameraForward * castRange);
        Vector3 direction = (targetPoint - launchPoint.position).normalized;
        float distance = Vector3.Distance(launchPoint.position, targetPoint);
        RaycastHit hit;
        if (Physics.Raycast(launchPoint.position, direction, out hit, distance))
        {
            if (hit.collider != null && hit.collider.attachedRigidbody != null)
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
            grabbedObject.transform.localScale = grabbedOriginalScale;
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
