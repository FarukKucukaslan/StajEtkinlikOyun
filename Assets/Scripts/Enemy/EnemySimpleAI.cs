using UnityEngine;

public class EnemySimpleAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of this enemy.")]
    public float speed = 3f;

    [Tooltip("Rotation speed when turning toward the player.")]
    public float rotationSpeed = 360f;

    [Header("Attack Settings")]
    [Tooltip("Damage dealt to the player per attack.")]
    public float damage = 10f;

    [Tooltip("Cooldown time between attacks.")]
    public float attackCooldown = 0.5f;

    [Tooltip("Distance at which the enemy stops and attacks.")]
    public float attackRange = 1.3f;

    [Header("Ground Detection")]
    [Tooltip("Only layers selected here are treated as walkable ground.")]
    [SerializeField]
    private LayerMask groundMask;

    [Tooltip("Height above the enemy used to start the ground raycast.")]
    [SerializeField, Min(0.1f)]
    private float groundProbeHeight = 5f;

    [Tooltip("Maximum distance checked below the enemy for ground.")]
    [SerializeField, Min(0.1f)]
    private float groundProbeDistance = 15f;

    [Tooltip(
        "Maximum height the enemy can climb upward in one movement step. "
            + "Prevents snapping onto tall colliders."
    )]
    [SerializeField, Min(0f)]
    private float maximumStepHeight = 2f;

    [Tooltip("Vertical offset applied after finding the ground.")]
    [SerializeField]
    private float groundOffset = 0f;

    [Header("Out of Bounds Safety")]
    [Tooltip("Enemies farther than this horizontal distance from the player are removed.")]
    [SerializeField, Min(1f)]
    private float maximumDistanceFromPlayer = 60f;

    [Tooltip("Enemies too far above or below the player are removed.")]
    [SerializeField, Min(1f)]
    private float maximumVerticalDistance = 20f;

    private Transform _player;
    private PlayerStats _playerStats;
    private float _attackTimer;

    private void Start()
    {
        FindPlayer();

        // Allows the enemy to attack immediately after reaching the player.
        _attackTimer = attackCooldown;

        if (groundMask.value == 0)
        {
            Debug.LogWarning($"{name}: EnemySimpleAI has no Ground Mask assigned.", this);
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            FindPlayer();

            if (_player == null)
            {
                return;
            }
        }

        if (IsOutsideValidArea())
        {
            RemoveOutOfBoundsEnemy();
            return;
        }

        Vector3 directionToPlayer = _player.position - transform.position;

        // Movement and attack range are calculated horizontally.
        directionToPlayer.y = 0f;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > attackRange)
        {
            ChasePlayer(directionToPlayer);
        }
        else
        {
            AttackPlayer(directionToPlayer);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        _player = playerObject.transform;
        _playerStats = playerObject.GetComponent<PlayerStats>();
    }

    private void ChasePlayer(Vector3 directionToPlayer)
    {
        // Reset the cooldown while chasing so the first contact
        // can deal damage immediately.
        if (_attackTimer < attackCooldown)
        {
            _attackTimer = attackCooldown;
        }

        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 movementDirection = directionToPlayer.normalized;

        Vector3 nextPosition = transform.position + movementDirection * speed * Time.deltaTime;

        if (TryGetGroundHeight(nextPosition, out float groundHeight))
        {
            nextPosition.y = groundHeight + groundOffset;
        }
        else
        {
            // Do not modify Y when no valid ground is found.
            nextPosition.y = transform.position.y;
        }

        transform.position = nextPosition;

        RotateTowards(movementDirection);
    }

    private void AttackPlayer(Vector3 directionToPlayer)
    {
        _attackTimer += Time.deltaTime;

        if (_attackTimer >= attackCooldown)
        {
            _attackTimer = 0f;

            if (_playerStats != null)
            {
                _playerStats.TakeDamage(damage);
            }
        }

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            RotateTowards(directionToPlayer.normalized);
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private bool TryGetGroundHeight(Vector3 nextPosition, out float groundHeight)
    {
        Vector3 rayOrigin = nextPosition + Vector3.up * groundProbeHeight;

        float rayDistance = groundProbeHeight + groundProbeDistance;

        bool foundGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!foundGround)
        {
            groundHeight = transform.position.y;
            return false;
        }

        float upwardHeightDifference = hit.point.y - transform.position.y;

        // Do not allow the enemy to snap upward onto a tall object
        // or an unreachable terrain section.
        if (upwardHeightDifference > maximumStepHeight)
        {
            groundHeight = transform.position.y;
            return false;
        }

        groundHeight = hit.point.y;
        return true;
    }

    private bool IsOutsideValidArea()
    {
        Vector3 offset = transform.position - _player.position;

        Vector2 horizontalOffset = new Vector2(offset.x, offset.z);

        bool tooFarAway =
            horizontalOffset.sqrMagnitude > maximumDistanceFromPlayer * maximumDistanceFromPlayer;

        bool invalidHeight = Mathf.Abs(offset.y) > maximumVerticalDistance;

        return tooFarAway || invalidHeight;
    }

    private void RemoveOutOfBoundsEnemy()
    {
        Debug.LogWarning($"Removed out-of-bounds enemy: {name}", this);

        Destroy(gameObject);
    }
}
