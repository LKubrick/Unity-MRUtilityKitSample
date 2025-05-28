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