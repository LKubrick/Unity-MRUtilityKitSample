using UnityEngine;

public class LockController : MonoBehaviour
{
    public Rigidbody boxRigidbody; // Assign the box's Rigidbody in the inspector
    public GameObject lockObject; // Assign the lock GameObject in the inspector
    public float unlockDelay = 0.5f; // Optional delay before unlocking

    private bool isUnlocked = false;

    void Start()
    {
        Debug.Log("[LockController] Starting up...");
        // Make sure the box's Rigidbody is frozen at start
        if (boxRigidbody != null)
        {
            boxRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Debug.Log("[LockController] Box Rigidbody found and frozen");
        }
        else
        {
            Debug.LogError("[LockController] No box Rigidbody assigned!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[LockController] Trigger entered by: {other.gameObject.name} with tag: {other.tag}");
        // Check if it's the key and we haven't unlocked yet
        if (!isUnlocked && other.CompareTag("Key"))
        {
            Debug.Log("[LockController] Key detected! Unlocking box...");
            UnlockBox();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[LockController] Collision with: {collision.gameObject.name} with tag: {collision.gameObject.tag}");
        // Also check for collisions
        if (!isUnlocked && collision.gameObject.CompareTag("Key"))
        {
            Debug.Log("[LockController] Key collision detected! Unlocking box...");
            UnlockBox();
        }
    }

    void UnlockBox()
    {
        isUnlocked = true;
        Debug.Log("[LockController] Starting unlock sequence...");

        // Unfreeze the box's Rigidbody
        if (boxRigidbody != null)
        {
            boxRigidbody.constraints = RigidbodyConstraints.None;
            Debug.Log("[LockController] Box Rigidbody unfrozen");
        }
        else
        {
            Debug.LogError("[LockController] No box Rigidbody to unfreeze!");
        }

        // Hide or destroy the lock
        if (lockObject != null)
        {
            lockObject.SetActive(false);
            Debug.Log("[LockController] Lock object hidden");
        }
        else
        {
            Debug.LogError("[LockController] No lock object to hide!");
        }

        // Optional: Add some visual or sound effects here
        Debug.Log("Box unlocked!");
    }
} 