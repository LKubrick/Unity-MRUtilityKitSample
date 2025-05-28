using UnityEngine;
using System.Collections;

public class BookTrigger : MonoBehaviour
{
    private const string ANIM_OPEN = "book-opening";
    private const string ANIM_OPEN_IDLE = "book-openidle";
    private const string ANIM_IDLE = "Book-Idle";

    public GameObject keyPrefab; // Assign the key prefab in the inspector
    public Transform keySpawnPoint; // Assign a transform in front of the book where the key should appear
    public float keyPopForce = 2f; // Force to make the key pop out
    public float keyPopUpwardForce = 1f; // Additional upward force

    private Animator animator;
    private bool hasPlayedOpening = false;
    private GameObject spawnedKey = null;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator found on the book!");
            enabled = false;
            return;
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
    }

    void SpawnKey()
    {
        if (keySpawnPoint == null)
        {
            // If no spawn point is set, spawn in front of the book
            Vector3 spawnPos = transform.position + transform.forward * 0.2f + Vector3.up * 0.1f;
            spawnedKey = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            spawnedKey = Instantiate(keyPrefab, keySpawnPoint.position, keySpawnPoint.rotation);
        }

        // Ensure the key has all necessary components for grabbing
        if (spawnedKey != null)
        {
            // Add Rigidbody if it doesn't exist
            Rigidbody keyRb = spawnedKey.GetComponent<Rigidbody>();
            if (keyRb == null)
            {
                keyRb = spawnedKey.AddComponent<Rigidbody>();
            }
            keyRb.useGravity = true;
            keyRb.isKinematic = false;
            keyRb.interpolation = RigidbodyInterpolation.Interpolate;
            keyRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Add collider if it doesn't exist
            Collider keyCollider = spawnedKey.GetComponent<Collider>();
            if (keyCollider == null)
            {
                // Add a box collider as default
                keyCollider = spawnedKey.AddComponent<BoxCollider>();
            }
            keyCollider.isTrigger = false; // Make sure it's not a trigger

            // Add the "Key" tag
            spawnedKey.tag = "Key";

            // Add pop effect
            Vector3 popDirection = transform.forward + Vector3.up * keyPopUpwardForce;
            keyRb.AddForce(popDirection * keyPopForce, ForceMode.Impulse);
        }
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