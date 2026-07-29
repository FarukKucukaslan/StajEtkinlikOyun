using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float speed = 12f;

    [SerializeField, Min(0.1f)]
    private float lifetime = 5f;

    [Header("Collision")]
    [SerializeField, Min(0.01f)]
    private float hitRadius = 0.15f;

    [Tooltip(
        "Layers the projectile can collide with. "
            + "Include Player, Ground and environment layers. Exclude Enemy."
    )]
    [SerializeField]
    private LayerMask collisionMask = ~0;

    private Vector3 _direction;
    private float _damage;
    private float _remainingLifetime;
    private Transform _owner;

    private bool _isInitialized;

    public void Initialize(Vector3 direction, float damage, Transform owner)
    {
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        _direction = direction.normalized;
        _damage = damage;
        _owner = owner;

        _remainingLifetime = lifetime;
        _isInitialized = true;

        transform.rotation = Quaternion.LookRotation(_direction);
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        float travelDistance = speed * Time.deltaTime;

        if (TryFindClosestCollision(travelDistance, out RaycastHit hit))
        {
            HandleCollision(hit);
            return;
        }

        transform.position += _direction * travelDistance;

        _remainingLifetime -= Time.deltaTime;

        if (_remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private bool TryFindClosestCollision(float travelDistance, out RaycastHit closestHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            hitRadius,
            _direction,
            travelDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        bool foundValidHit = false;
        float closestDistance = float.MaxValue;
        closestHit = default;

        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnoreHit(hit))
            {
                continue;
            }

            if (hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            closestHit = hit;
            foundValidHit = true;
        }

        return foundValidHit;
    }

    private bool ShouldIgnoreHit(RaycastHit hit)
    {
        if (hit.collider == null)
        {
            return true;
        }

        Transform hitTransform = hit.collider.transform;

        if (_owner != null && (hitTransform == _owner || hitTransform.IsChildOf(_owner)))
        {
            return true;
        }

        // Arrows should not collide with other enemies.
        EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();

        return enemy != null;
    }

    private void HandleCollision(RaycastHit hit)
    {
        PlayerStats playerStats = hit.collider.GetComponentInParent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(_damage);
        }

        transform.position = hit.point;

        Destroy(gameObject);
    }
}
