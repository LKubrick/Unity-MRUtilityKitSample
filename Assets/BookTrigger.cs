using UnityEngine;
using System.Collections;

public class BookTrigger : MonoBehaviour
{
    private const string ANIM_OPEN = "book-opening";
    private const string ANIM_OPEN_IDLE = "book-openidle";
    private const string ANIM_IDLE = "Book-Idle";

    [Header("Key Settings")]
    public GameObject keyPrefab; // Assign the key prefab in the inspector
    public Transform keySpawnPoint; // Assign a transform in front of the book where the key should appear
    public float keyPopForce = 2f; // Force to make the key pop out
    public float keyPopUpwardForce = 1f; // Additional upward force

    [Header("Lock Settings")]
    public Transform lockTarget; // Assign the Lock object's transform
    public float flightDelay = 1f; // How long to wait before flying to lock
    public float flightDuration = 3f; // How long the flight should take in seconds
    public float rotationSpeed = 180f; // How fast the key spins while flying
    public float bobbleAmount = 0.2f; // How much the key bobs up and down
    public float bobbleSpeed = 2f; // How fast the key bobs
    public float arrivalDelay = 0.5f; // How long to wait at the lock before disappearing

    [Header("Box Settings")]
    public GameObject boxLid; // Reference to the box lid
    public float lidOpenAngle = 75f; // How far the lid opens in degrees
    public float lidOpenDuration = 1f; // How long it takes to open the lid

    [Header("Rune Settings")]
    public GameObject runePrefab; // Assign the rune prefab in the inspector
    public Transform runeSpawnPoint; // Where the rune should spawn
    public float runeLevitateHeight = 0.15f; // How high the rune levitates (in meters)
    public float runeLevitateSpeed = 1f; // How fast the rune bobs up and down
    public float runeRotationSpeed = 45f; // How fast the rune rotates
    public float runePopForce = 3f; // Force to make the rune pop out

    private Animator animator;
    private bool hasPlayedOpening = false;
    private GameObject spawnedKey = null;
    private GameObject spawnedRune = null;
    private Vector3 runeOriginalPosition;
    private float runeTime = 0f;
    private bool hasOpenedBox = false;
    private Coroutine lidOpenCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator found on the book!");
            enabled = false;
            return;
        }

        if (boxLid == null)
        {
            Debug.LogWarning("No box lid assigned! Rune won't be able to push it open.");
        }
    }

    void Update()
    {
        if (hasPlayedOpening)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(ANIM_OPEN) && stateInfo.normalizedTime >= 0.9f)
            {
                // When opening animation is almost done, switch to open idle
                animator.Play(ANIM_OPEN_IDLE);
                hasPlayedOpening = false;

                // Spawn the key if it hasn't been spawned yet
                if (spawnedKey == null && keyPrefab != null)
                {
                    SpawnKey();
                }
            }
        }

        // Update rune levitation if it exists
        if (spawnedRune != null)
        {
            UpdateRuneLevitation();
        }
    }

    void SpawnKey()
    {
        if (keySpawnPoint == null)
        {
            // If no spawn point is set, spawn in front of the book
            Vector3 spawnPos = transform.position + transform.forward * 0.01f + Vector3.up * 0.05f;
            spawnedKey = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            spawnedKey = Instantiate(keyPrefab, keySpawnPoint.position, keySpawnPoint.rotation);
        }

        // Add a pop effect
        Rigidbody keyRb = spawnedKey.GetComponent<Rigidbody>();
        if (keyRb != null)
        {
            // Add force in the direction the book is facing, plus some upward force
            Vector3 popDirection = transform.forward + Vector3.up * keyPopUpwardForce;
            keyRb.AddForce(popDirection * keyPopForce, ForceMode.Impulse);

            // Start flying to lock after delay
            if (lockTarget != null)
            {
                StartCoroutine(FlyToLock(keyRb));
            }
            else
            {
                Debug.LogWarning("No Lock target assigned!");
            }
        }
    }

    public void OpenBox()
    {
        Debug.Log("[BookTrigger] OpenBox called");
        Debug.Log($"[BookTrigger] Box lid reference: {(boxLid != null ? "Found" : "Missing!")}");
        Debug.Log($"[BookTrigger] Rune prefab reference: {(runePrefab != null ? "Found" : "Missing!")}");
        Debug.Log($"[BookTrigger] Has opened box before: {hasOpenedBox}");

        if (!hasOpenedBox && boxLid != null)
        {
            Debug.Log("[BookTrigger] Starting box opening sequence");
            hasOpenedBox = true;
            
            // Start the lid opening coroutine
            if (lidOpenCoroutine != null)
            {
                Debug.Log("[BookTrigger] Stopping existing lid open coroutine");
                StopCoroutine(lidOpenCoroutine);
            }
            lidOpenCoroutine = StartCoroutine(OpenLid());
            Debug.Log("[BookTrigger] Started lid open coroutine");

            // Spawn the rune
            if (spawnedRune == null && runePrefab != null)
            {
                Debug.Log("[BookTrigger] Attempting to spawn rune");
                SpawnRune();
            }
            else
            {
                Debug.Log($"[BookTrigger] Cannot spawn rune - Spawned: {(spawnedRune != null ? "Yes" : "No")}, Prefab: {(runePrefab != null ? "Found" : "Missing")}");
            }
        }
        else
        {
            Debug.Log($"[BookTrigger] Cannot open box - Has opened: {hasOpenedBox}, Lid: {(boxLid != null ? "Found" : "Missing")}");
        }
    }

    private IEnumerator OpenLid()
    {
        Debug.Log("[BookTrigger] Starting lid opening animation");
        Debug.Log($"[BookTrigger] Initial lid rotation: {boxLid.transform.localRotation.eulerAngles}");
        
        float startTime = Time.time;
        Quaternion startRotation = boxLid.transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(lidOpenAngle, 0, 0);
        
        Debug.Log($"[BookTrigger] Target lid rotation: {targetRotation.eulerAngles}");

        while (Time.time - startTime < lidOpenDuration)
        {
            float t = (Time.time - startTime) / lidOpenDuration;
            // Smooth step interpolation
            t = t * t * (3f - 2f * t);
            
            boxLid.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            Debug.Log($"[BookTrigger] Current lid rotation: {boxLid.transform.localRotation.eulerAngles}");
            yield return null;
        }

        // Ensure we reach the exact target rotation
        boxLid.transform.localRotation = targetRotation;
        Debug.Log("[BookTrigger] Lid opening animation complete");
    }

    void SpawnRune()
    {
        Debug.Log("[BookTrigger] Starting rune spawn");
        
        Vector3 spawnPos;
        if (runeSpawnPoint == null)
        {
            // If no spawn point is set, spawn inside the box
            spawnPos = transform.position + Vector3.up * 0.2f;
            Debug.Log($"[BookTrigger] No spawn point set, using default position: {spawnPos}");
        }
        else
        {
            spawnPos = runeSpawnPoint.position;
            Debug.Log($"[BookTrigger] Using spawn point position: {spawnPos}");
        }

        spawnedRune = Instantiate(runePrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[BookTrigger] Rune instantiated at: {spawnedRune.transform.position}");

        runeOriginalPosition = spawnedRune.transform.position;
        runeTime = 0f;

        // Add a collider and rigidbody if they don't exist
        if (spawnedRune.GetComponent<Collider>() == null)
        {
            Debug.Log("[BookTrigger] Adding SphereCollider to rune");
            spawnedRune.AddComponent<SphereCollider>();
        }
        if (spawnedRune.GetComponent<Rigidbody>() == null)
        {
            Debug.Log("[BookTrigger] Adding Rigidbody to rune");
            Rigidbody rb = spawnedRune.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Add initial pop force
        Rigidbody runeRb = spawnedRune.GetComponent<Rigidbody>();
        if (runeRb != null)
        {
            Debug.Log("[BookTrigger] Adding pop force to rune");
            runeRb.isKinematic = false;
            runeRb.AddForce(Vector3.up * runePopForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("[BookTrigger] No Rigidbody found on rune for pop force!");
        }

        Debug.Log("[BookTrigger] Rune spawn complete");
    }

    void UpdateRuneLevitation()
    {
        runeTime += Time.deltaTime * runeLevitateSpeed;
        // Use Mathf.Abs to ensure we only move upward
        float verticalOffset = Mathf.Abs(Mathf.Sin(runeTime)) * runeLevitateHeight;
        spawnedRune.transform.position = runeOriginalPosition + Vector3.up * verticalOffset;
        spawnedRune.transform.Rotate(Vector3.up, runeRotationSpeed * Time.deltaTime);
    }

    private IEnumerator FlyToLock(Rigidbody keyRb)
    {
        // Wait for the initial delay
        yield return new WaitForSeconds(flightDelay);

        Vector3 startPosition = keyRb.transform.position;
        float journeyLength = Vector3.Distance(startPosition, lockTarget.position);
        float startTime = Time.time;
        float bobbleTime = 0f;

        // Disable physics while flying
        keyRb.isKinematic = true;

        while (Vector3.Distance(keyRb.transform.position, lockTarget.position) > 0.1f)
        {
            float elapsedTime = Time.time - startTime;
            float fractionOfJourney = elapsedTime / flightDuration;

            // Calculate base position
            Vector3 basePosition = Vector3.Lerp(startPosition, lockTarget.position, fractionOfJourney);
            
            // Add bobbing motion
            bobbleTime += Time.deltaTime * bobbleSpeed;
            float bobbleOffset = Mathf.Sin(bobbleTime) * bobbleAmount;
            Vector3 bobblePosition = basePosition + Vector3.up * bobbleOffset;

            // Move the key
            keyRb.transform.position = bobblePosition;
            
            // Rotate the key
            keyRb.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        // Ensure the key arrives exactly at the lock
        keyRb.transform.position = lockTarget.position;
        keyRb.transform.rotation = lockTarget.rotation;

        // Wait a moment at the lock before disappearing
        yield return new WaitForSeconds(arrivalDelay);

        // Destroy the key after arriving
        Destroy(spawnedKey);
        spawnedKey = null;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the wand or hand
        if (other.CompareTag("Wand") || other.GetComponent<OVRControllerHelper>() != null || other.GetComponent<OVRHand>() != null)
        {
            if (!hasPlayedOpening)
            {
                OpenBook();
            }
        }
    }

    public void OpenBook()
    {
        animator.Play(ANIM_OPEN);
        hasPlayedOpening = true;
    }
} 