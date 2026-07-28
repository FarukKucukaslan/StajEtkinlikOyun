using UnityEngine;

public class EnemySimpleAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of this enemy.")]
    public float speed = 3f;

    [Tooltip("Rotation speed of this enemy when looking at the player.")]
    public float rotationSpeed = 360f;

    [Header("Attack Settings")]
    [Tooltip("Damage dealt to the player per attack.")]
    public float damage = 10f;

    [Tooltip("Cooldown time between attacks (in seconds).")]
    public float attackCooldown = 0.5f;

    [Tooltip("Distance at which the enemy stops and attacks the player.")]
    public float attackRange = 1.3f;

    private Transform _player;
    private PlayerStats _playerStats;
    private float _attackTimer;

    private void Start()
    {
        // Try to find the player automatically using the Player tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerStats = playerObj.GetComponent<PlayerStats>();
        }

        // Set timer so the enemy attacks immediately upon first contact
        _attackTimer = attackCooldown;
    }

    private void Update()
    {
        if (_player == null) return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // If outside of attack range, walk towards the player
        if (distanceToPlayer > attackRange)
        {
            // Reset attack cooldown while chasing so they hit instantly on contact
            if (_attackTimer < attackCooldown)
            {
                _attackTimer = attackCooldown;
            }

            // Calculate direction to player (ignoring Y/height difference)
            Vector3 direction = (_player.position - transform.position).normalized;
            direction.y = 0f; 

            // Calculate next position on the flat plane
            Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;

            // Snap to ground height using a Raycast (ignoring our own collider to prevent flying upward infinitely)
            Ray ray = new Ray(new Vector3(nextPosition.x, nextPosition.y + 15f, nextPosition.z), Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 30f);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
                {
                    nextPosition.y = hit.point.y;
                    break; // Found the ground, stop checking further hits
                }
            }

            // Apply position
            transform.position = nextPosition;

            // Smoothly rotate towards the player
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Inside attack range: stop walking and attack the player
            _attackTimer += Time.deltaTime;
            
            if (_attackTimer >= attackCooldown)
            {
                _attackTimer = 0f;
                if (_playerStats != null)
                {
                    _playerStats.TakeDamage(damage);
                }
            }

            // Keep rotating to face the player even while attacking
            Vector3 direction = (_player.position - transform.position).normalized;
            direction.y = 0f;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
