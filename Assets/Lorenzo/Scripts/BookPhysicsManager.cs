using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BookPhysicsManager : MonoBehaviour
{
    private Rigidbody rb;
    private bool isBeingInteracted = false;
    private float sleepTimer = 0f;
    private const float SLEEP_DELAY = 2f; // Time in seconds before putting the book to sleep

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Initial setup
            rb.sleepThreshold = 0.005f; // Lower threshold to sleep sooner
            rb.interpolation = RigidbodyInterpolation.None; // Disable interpolation when not needed
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // Use discrete collision detection
        }
    }

    void Update()
    {
        if (!isBeingInteracted && rb != null)
        {
            // If the book is moving very slowly, start the sleep timer
            if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.1f)
            {
                sleepTimer += Time.deltaTime;
                if (sleepTimer >= SLEEP_DELAY)
                {
                    PutBookToSleep();
                }
            }
            else
            {
                sleepTimer = 0f;
            }
        }
    }

    // Call this when the book is being grabbed or interacted with
    public void OnBookInteracted()
    {
        isBeingInteracted = true;
        sleepTimer = 0f;
        if (rb != null)
        {
            rb.WakeUp();
            // Enable interpolation while being interacted with
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            // Use continuous collision detection while being interacted with
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    // Call this when the book is released
    public void OnBookReleased()
    {
        isBeingInteracted = false;
        sleepTimer = 0f;
        if (rb != null)
        {
            // Disable interpolation when not being interacted with
            rb.interpolation = RigidbodyInterpolation.None;
            // Use discrete collision detection when not being interacted with
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
    }

    private void PutBookToSleep()
    {
        if (rb != null && !isBeingInteracted)
        {
            rb.Sleep();
            // Disable interpolation when sleeping
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    // Optional: Add this to your OVRGrabbable component's events
    void OnGrabStart()
    {
        OnBookInteracted();
    }

    void OnGrabEnd()
    {
        OnBookReleased();
    }
} 