using UnityEngine;

public class WandManager : MonoBehaviour
{
    public static WandManager Instance { get; private set; }
    public Transform wandTransform; // The root transform of the wand
    public Transform wandTipTransform; // NEW: The specific transform for the wand tip

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: Keep the manager alive across scenes
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Multiple WandManager instances found. Destroying this one.");
            Destroy(gameObject);
        }
    }

    // Optional: Add a method to easily get the target transform
    public Transform GetRuneTargetTransform()
    {
        if (wandTipTransform != null)
        {
            return wandTipTransform;
        }
        Debug.LogWarning("Wand tip transform not assigned in WandManager, using root wand transform instead.");
        return wandTransform;
    }
} 