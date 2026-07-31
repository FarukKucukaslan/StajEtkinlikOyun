using System.Collections;
using UnityEngine;

// Walks toward the player, stops at throwRange, and throws an item at them on a cooldown.
public class ItemThrowerEnemy : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed while approaching the player.")]
    public float moveSpeed = 3f;

    [Tooltip("Rotation speed when turning toward the player.")]
    public float rotationSpeed = 360f;

    [Tooltip("Distance from the player at which this enemy stops and starts throwing.")]
    public float throwRange = 8f;

    [Header("Ranged Attack")]
    [SerializeField]
    private EnemyProjectile itemPrefab;

    [SerializeField]
    private Transform throwSpawnPoint;

    [Tooltip("Damage dealt by each thrown item.")]
    public float itemDamage = 10f;

    [Tooltip("Time between throws.")]
    public float throwCooldown = 2.5f;

    [Tooltip("Delay between starting the throw and releasing the item.")]
    public float aimDuration = 0.4f;

    [Tooltip("Vertical point on the player that the item aims toward.")]
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

    private float _throwCooldownTimer;
    private bool _isAiming;

    private void Start()
    {
        FindPlayer();

        // Prevent every thrower from throwing at exactly the same moment.
        _throwCooldownTimer = Random.Range(0f, throwCooldown);

        if (groundMask.value == 0)
        {
            Debug.LogWarning($"{name}: ItemThrowerEnemy has no Ground Mask assigned.", this);
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

        UpdateThrowCooldown();

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

        if (distanceToPlayer > throwRange)
        {
            MoveAlongGround(directionToPlayer.normalized, moveSpeed);

            return;
        }

        if (_throwCooldownTimer <= 0f)
        {
            StartCoroutine(ThrowRoutine());
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

    private void UpdateThrowCooldown()
    {
        if (_throwCooldownTimer <= 0f)
        {
            return;
        }

        _throwCooldownTimer = Mathf.Max(0f, _throwCooldownTimer - Time.deltaTime);
    }

    private IEnumerator ThrowRoutine()
    {
        if (_isAiming || itemPrefab == null)
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
            ThrowItem();
        }

        _throwCooldownTimer = throwCooldown;

        _isAiming = false;
    }

    private void ThrowItem()
    {
        Vector3 spawnPosition =
            throwSpawnPoint != null ? throwSpawnPoint.position : transform.position + Vector3.up;

        Vector3 targetPosition = _player.position + Vector3.up * playerAimHeight;

        Vector3 itemDirection = (targetPosition - spawnPosition).normalized;

        EnemyProjectile item = Instantiate(
            itemPrefab,
            spawnPosition,
            Quaternion.LookRotation(itemDirection)
        );

        item.Initialize(itemDirection, itemDamage, transform);
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
