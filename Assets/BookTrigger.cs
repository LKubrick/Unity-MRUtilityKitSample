using UnityEngine;

public class BookTrigger : MonoBehaviour
{
    private Animator animator;
    private const string ANIM_OPEN = "book-opening";
    private const string ANIM_OPEN_IDLE = "book-openidle";

    [Header("Key Settings")]
    public GameObject key;  // Assign in editor
    public Transform cameraRig;  // Assign in editor
    public float keyFloatDistance = 1f;  // Distance in front of camera
    private bool isKeyActive = false;
    private bool isKeyFollowing = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[BookTrigger] No Animator component found!");
        }

        if (key != null)
        {
            key.SetActive(false);
        }
        else
        {
            Debug.LogError("[BookTrigger] No key assigned!");
        }
    }

    void Update()
    {
        if (isKeyFollowing && cameraRig != null)
        {
            // Make key float in front of the camera
            Vector3 targetPosition = cameraRig.position + cameraRig.forward * keyFloatDistance;
            key.transform.position = targetPosition;
            key.transform.rotation = cameraRig.rotation;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wand"))
        {
            if (!isKeyActive)
            {
                // First interaction: Open book and show key
                Debug.Log("[BookTrigger] Wand entered trigger - Opening book");
                animator.Play(ANIM_OPEN);
                ActivateKey();
            }
            else if (isKeyActive && !isKeyFollowing)
            {
                // Second interaction: Make key follow camera
                Debug.Log("[BookTrigger] Wand touched key - Making it follow camera");
                MakeKeyFollowCamera();
            }
        }
    }

    void ActivateKey()
    {
        if (key != null)
        {
            key.SetActive(true);
            isKeyActive = true;
            Debug.Log("[BookTrigger] Key activated");
        }
    }

    void MakeKeyFollowCamera()
    {
        if (key != null && cameraRig != null)
        {
            isKeyFollowing = true;
            key.transform.SetParent(cameraRig);
            Debug.Log("[BookTrigger] Key now following camera");
        }
    }
} 