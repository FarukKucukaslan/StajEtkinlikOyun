using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwordProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of the sword projectile.")]
    public float speed = 15f;

    [Tooltip("How long the sword lives before being destroyed automatically (if it doesn't hit anything).")]
    public float lifetime = 3f;

    [Header("Damage Settings")]
    [Tooltip("Damage dealt to the enemy upon impact.")]
    public float damage = 20f;

    [Tooltip("How many enemies the sword can pass through before destroying itself.")]
    public int pierceCount = 1;

    private void Start()
    {
        // Automatically configure the Rigidbody to be kinematic
        // This guarantees collision triggers work properly in Unity
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Destroy the sword after its lifetime expires
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move the sword forward relative to its orientation
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit an enemy
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // Apply damage to the enemy
            enemyHealth.TakeDamage(damage);

            // Decrease pierce count and destroy only if no pierce left
            pierceCount--;
            if (pierceCount <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
