using UnityEngine;
using System.Collections;
using Oculus.Interaction;

public class BookOfEvil : MonoBehaviour
{
    private Animator animator;
    private GameObject key;
    private Vector3 keyOriginalPosition;
    private float keyHoverHeight = 0.5f;
    private float keyHoverSpeed = 2f;
    private bool isKeyActive = false;
    private float keyHoverTime = 0f;
    private Collider bookCollider;
    private bool hasPlayedAnimation = false;

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
        if (isKeyActive && key != null)
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
        }
        else
        {
            Debug.LogWarning($"[BookOfEvil] Cannot activate key - Key: {(key == null ? "null" : "found")}, IsActive: {isKeyActive}");
        }
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