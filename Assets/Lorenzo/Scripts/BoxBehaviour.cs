using UnityEngine;

public class BoxBehaviour : MonoBehaviour
{
    // Destroy the box if it collides with the spell
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Spell"))
        {
            Destroy(gameObject);
        }
    }
}
