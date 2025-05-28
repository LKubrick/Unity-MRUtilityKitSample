using UnityEngine;

public class LockController : MonoBehaviour
{
    public Rigidbody boxRigidbody; // Assign the box's Rigidbody in the inspector
    public GameObject lockObject; // Assign the lock GameObject in the inspector
    public float unlockDelay = 0.5f; // Optional delay before unlocking

    private bool isUnlocked = false;

    void Start()
    {
        // Make sure the box's Rigidbody is frozen at start
        if (boxRigidbody != null)
        {
            boxRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the key and we haven't unlocked yet
        if (!isUnlocked && other.CompareTag("Key"))
        {
            UnlockBox();
        }
    }

    void UnlockBox()
    {
        isUnlocked = true;

        // Unfreeze the box's Rigidbody
        if (boxRigidbody != null)
        {
            boxRigidbody.constraints = RigidbodyConstraints.None;
        }

        // Hide or destroy the lock
        if (lockObject != null)
        {
            lockObject.SetActive(false);
        }

        // Optional: Add some visual or sound effects here
        Debug.Log("Box unlocked!");
    }
} 