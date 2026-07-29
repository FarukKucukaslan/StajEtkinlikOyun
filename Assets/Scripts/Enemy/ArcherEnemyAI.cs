using System.Collections;
using UnityEngine;

public class ArcherEnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed of the archer.")]
    public float moveSpeed = 2.5f;

    [Tooltip("Rotation speed when looking toward the player.")]
    public float rotationSpeed = 360f;

    [Tooltip("The archer retreats when the player is closer than this.")]
    public float preferredMinDistance = 6.5f;

    [Tooltip("The archer approaches when the player is farther than this.")]
    public float preferredMaxDistance = 10f;

    [Tooltip("Movement multiplier used while retreating.")]
    public float retreatSpeedMultiplier = 1.1f;

    [Header("Ranged Attack")]
    [SerializeField]
    private EnemyProjectile projectilePrefab;

    [SerializeField]
    private Transform projectileSpawnPoint;

    [Tooltip("Damage dealt by each projectile.")]
    public float projectileDamage = 8f;

    [Tooltip("Time between ranged attacks.")]
    public float attackCooldown = 2.2f;

    [Tooltip("Delay between beginning the attack and releasing the projectile.")]
    public float aimDuration = 0.55f;

    [Tooltip("Vertical point on the player that the archer aims toward.")]
    public float playerAimHeight = 0.8f;

    [Header("Ground Detection")]
    [SerializeField]
    private LayerMask groundMask;

    [SerializeField, Min(0.1f)]
    private float groundProbeHeight = 5f;

    [SerializeField, Min(0.1f)]
    private float groundProbeDistance = 15f;

    [SerializeField, Min(0f)]
    private float maximumStepHeight = 2f;

    [SerializeField]
    private float groundOffset;

    [Header("Out of Bounds Safety")]
    [SerializeField, Min(1f)]
    private float maximumDistanceFromPlayer = 60f;

    [SerializeField, Min(1f)]
    private float maximumVerticalDistance = 20f;

    private Transform _player;

    private float _attackCooldownTimer;
    private bool _isAiming;

    private void Start()
    {
        FindPlayer();

        // Prevent every archer from firing at exactly the same moment.
        _attackCooldownTimer = Random.Range(0f, attackCooldown);

        if (groundMask.value == 0)
        {
            Debug.LogWarning($"{name}: ArcherEnemyAI has no Ground Mask assigned.", this);
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
            Destroy(gameObject);
            return;
        }

        UpdateAttackCooldown();

        Vector3 directionToPlayer = _player.position - transform.position;

        directionToPlayer.y = 0f;

        float distanceToPlayer = directionToPlayer.magnitude;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            RotateTowards(directionToPlayer.normalized);
        }

        if (_isAiming)
        {
            return;
        }

        if (distanceToPlayer > preferredMaxDistance)
        {
            MoveAlongGround(directionToPlayer.normalized, moveSpeed);

            return;
        }

        if (distanceToPlayer < preferredMinDistance)
        {
            MoveAlongGround(-directionToPlayer.normalized, moveSpeed * retreatSpeedMultiplier);

            return;
        }

        if (_attackCooldownTimer <= 0f)
        {
            StartCoroutine(ShootRoutine());
        }
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
    }

    private void UpdateAttackCooldown()
    {
        if (_attackCooldownTimer <= 0f)
        {
            return;
        }

        _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
    }

    private IEnumerator ShootRoutine()
    {
        if (_isAiming || projectilePrefab == null)
        {
            yield break;
        }

        _isAiming = true;

        float remainingAimTime = aimDuration;

        while (remainingAimTime > 0f && _player != null)
        {
            Vector3 directionToPlayer = _player.position - transform.position;

            directionToPlayer.y = 0f;

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                RotateTowards(directionToPlayer.normalized);
            }

            remainingAimTime -= Time.deltaTime;

            yield return null;
        }

        if (_player != null)
        {
            FireProjectile();
        }

        _attackCooldownTimer = attackCooldown;

        _isAiming = false;
    }

    private void FireProjectile()
    {
        Vector3 spawnPosition =
            projectileSpawnPoint != null
                ? projectileSpawnPoint.position
                : transform.position + Vector3.up;

        Vector3 targetPosition = _player.position + Vector3.up * playerAimHeight;

        Vector3 projectileDirection = (targetPosition - spawnPosition).normalized;

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(projectileDirection)
        );

        projectile.Initialize(projectileDirection, projectileDamage, transform);
    }

    private void MoveAlongGround(Vector3 direction, float movementSpeed)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.y = 0f;
        direction.Normalize();

        Vector3 nextPosition = transform.position + direction * movementSpeed * Time.deltaTime;

        if (TryGetGroundHeight(nextPosition, out float groundHeight))
        {
            nextPosition.y = groundHeight + groundOffset;
        }
        else
        {
            nextPosition.y = transform.position.y;
        }

        transform.position = nextPosition;

        RotateTowards(direction);
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

        float upwardDifference = hit.point.y - transform.position.y;

        if (upwardDifference > maximumStepHeight)
        {
            groundHeight = transform.position.y;

            return false;
        }

        groundHeight = hit.point.y;
        return true;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
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
}
