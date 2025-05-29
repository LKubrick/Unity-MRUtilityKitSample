using UnityEngine;
using System.Collections;
using Oculus.Interaction;

public class BookOfEvil : MonoBehaviour
{
    [Header("Key Settings")]
    [SerializeField] private Transform lockTarget; // Reference to the Lock object
    [SerializeField] private float flightSpeed = 5f; // Speed at which the key flies to the lock
    [SerializeField] private float flightDelay = 1f; // Delay before key starts flying
    [SerializeField] private float rotationSpeed = 180f; // Speed at which the key rotates while flying
    [SerializeField] private float arrivalDistance = 0.1f; // Distance at which the key is considered to have arrived

    private Animator animator;
    private GameObject key;
    private Vector3 keyOriginalPosition;
    private float keyHoverHeight = 0.5f;
    private float keyHoverSpeed = 2f;
    private bool isKeyActive = false;
    private float keyHoverTime = 0f;
    private Collider bookCollider;
    private bool hasPlayedAnimation = false;
    private bool isFlyingToLock = false;

    // Animation names
    private const string ANIM_OPEN = "book-opening";
    private const string ANIM_OPEN_IDLE = "book-openidle";

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[BookOfEvil] No Animator component found!");
        }
        else
        {
            Debug.Log("[BookOfEvil] Animator found. Available animations:");
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                Debug.Log($"[BookOfEvil] - {clip.name} (Length: {clip.length}s)");
            }
        }

        key = transform.Find("Key")?.gameObject;
        if (key != null)
        {
            keyOriginalPosition = key.transform.localPosition;
            key.SetActive(false);
            Debug.Log("[BookOfEvil] Key found and initialized");
        }
        else
        {
            Debug.LogError("[BookOfEvil] No Key child object found!");
        }

        bookCollider = GetComponent<Collider>();
        if (bookCollider == null)
        {
            Debug.LogError("[BookOfEvil] No Collider found!");
        }
        else
        {
            Debug.Log($"[BookOfEvil] Collider found: {bookCollider.GetType().Name}");
        }
    }

    void Update()
    {
        if (isKeyActive && key != null && !isFlyingToLock)
        {
            keyHoverTime += Time.deltaTime * keyHoverSpeed;
            float hoverOffset = Mathf.Sin(keyHoverTime) * 0.1f;
            key.transform.localPosition = keyOriginalPosition + new Vector3(0, keyHoverHeight + hoverOffset, 0);
            key.transform.Rotate(Vector3.up, Time.deltaTime * 45f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[BookOfEvil] Collision with: {collision.gameObject.name}");
        
        // Check if the colliding object is the wand
        if (collision.gameObject.CompareTag("Wand") && !hasPlayedAnimation)
        {
            Debug.Log("[BookOfEvil] Wand collision detected!");
            PlayOpenAnimation();
            hasPlayedAnimation = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[BookOfEvil] Trigger with: {other.gameObject.name} (Tag: {other.gameObject.tag})");
        
        // Check if the triggering object is the wand
        if (other.CompareTag("Wand"))
        {
            Debug.Log("[BookOfEvil] Wand trigger detected!");
            if (!hasPlayedAnimation)
            {
                ActivateKey();
                PlayOpenAnimation();
                hasPlayedAnimation = true;
            }
        }
    }

    void PlayOpenAnimation()
    {
        if (animator != null)
        {
            Debug.Log("[BookOfEvil] Playing opening animation");
            animator.Play(ANIM_OPEN, 0, 0f);
            
            // After opening animation, play the open idle
            StartCoroutine(PlayOpenIdle());
        }
    }

    private System.Collections.IEnumerator PlayOpenIdle()
    {
        yield return new WaitForSeconds(1.5f);
        animator.Play(ANIM_OPEN_IDLE);
    }

    public void ActivateKey()
    {
        Debug.Log("[BookOfEvil] ActivateKey called");
        if (key != null && !isKeyActive)
        {
            isKeyActive = true;
            key.SetActive(true);
            keyHoverTime = 0f;
            Debug.Log("[BookOfEvil] Key activated");

            // Start flying to lock after delay
            if (lockTarget != null)
            {
                StartCoroutine(FlyToLock());
            }
            else
            {
                Debug.LogWarning("[BookOfEvil] No Lock target assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"[BookOfEvil] Cannot activate key - Key: {(key == null ? "null" : "found")}, IsActive: {isKeyActive}");
        }
    }

    private IEnumerator FlyToLock()
    {
        // Wait for the initial delay
        yield return new WaitForSeconds(flightDelay);

        isFlyingToLock = true;
        Vector3 startPosition = key.transform.position;
        Quaternion startRotation = key.transform.rotation;
        float journeyLength = Vector3.Distance(startPosition, lockTarget.position);
        float startTime = Time.time;

        while (Vector3.Distance(key.transform.position, lockTarget.position) > arrivalDistance)
        {
            float distanceCovered = (Time.time - startTime) * flightSpeed;
            float fractionOfJourney = distanceCovered / journeyLength;

            // Move the key
            key.transform.position = Vector3.Lerp(startPosition, lockTarget.position, fractionOfJourney);
            
            // Rotate the key
            key.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            yield return null;
        }

        // Ensure the key arrives exactly at the lock
        key.transform.position = lockTarget.position;
        key.transform.rotation = lockTarget.rotation;

        // Deactivate the key after arriving
        DeactivateKey();
    }

    public void DeactivateKey()
    {
        Debug.Log("[BookOfEvil] DeactivateKey called");
        if (key != null && isKeyActive)
        {
            isKeyActive = false;
            key.SetActive(false);
            key.transform.localPosition = keyOriginalPosition;
            key.transform.localRotation = Quaternion.identity;
            Debug.Log("[BookOfEvil] Key deactivated");
        }
        else
        {
            Debug.LogWarning($"[BookOfEvil] Cannot deactivate key - Key: {(key == null ? "null" : "found")}, IsActive: {isKeyActive}");
        }
    }
} 