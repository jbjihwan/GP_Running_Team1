using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Movement Speed")]
    public float speed = 2f;

    [Header("Damage Settings")]
    public int contactDamage = 1;

    private void Update()
    {
        // Move the trap forward (relative to its facing direction)
        transform.position -= transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only interact with the Player
        if (!other.CompareTag("Player"))
            return;

        // Try to get PlayerEvent component
        PlayerEvent playerEvent = other.GetComponent<PlayerEvent>();
        if (playerEvent == null)
            playerEvent = other.GetComponentInParent<PlayerEvent>();

        // Apply damage if PlayerEvent exists
        if (playerEvent != null)
        {
            playerEvent.OnTrapHit(contactDamage);
        }

        // Destroy the trap after contact
        Destroy(gameObject);
    }
}
