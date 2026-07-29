using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    [Header("Experience")]
    [SerializeField]
    private float experienceAmount = 10f;

    [Header("Attraction")]
    [SerializeField]
    private float collectionRadius = 0.6f;

    [SerializeField]
    private float maximumMoveSpeed = 12f;

    [SerializeField]
    private float acceleration = 25f;

    [SerializeField]
    private float playerHeightOffset = 0.8f;

    [Header("Visual")]
    [SerializeField]
    private Transform visual;

    [SerializeField]
    private float rotationSpeed = 140f;

    [SerializeField]
    private float bobHeight = 0.12f;

    [SerializeField]
    private float bobSpeed = 3f;

    private PlayerStats playerStats;
    private Vector3 visualStartPosition;

    private float currentMoveSpeed;
    private bool isCollected;

    private void Awake()
    {
        if (visual != null)
        {
            visualStartPosition = visual.localPosition;
        }
    }

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void Update()
    {
        AnimateVisual();

        if (isCollected || playerStats == null)
        {
            return;
        }

        MoveTowardsPlayer();
    }

    public void Initialize(float amount)
    {
        experienceAmount = amount;
    }

    private void MoveTowardsPlayer()
    {
        Vector3 targetPosition = playerStats.transform.position + Vector3.up * playerHeightOffset;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPosition);

        if (distanceToPlayer <= collectionRadius)
        {
            Collect();
            return;
        }

        if (distanceToPlayer > playerStats.XPPickupRange)
        {
            currentMoveSpeed = 0f;
            return;
        }

        currentMoveSpeed = Mathf.MoveTowards(
            currentMoveSpeed,
            maximumMoveSpeed,
            acceleration * Time.deltaTime
        );

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentMoveSpeed * Time.deltaTime
        );
    }

    private void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        playerStats.AddXP(experienceAmount);

        Destroy(gameObject);
    }

    private void AnimateVisual()
    {
        Transform rotatingTransform = visual != null ? visual : transform;

        rotatingTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (visual == null)
        {
            return;
        }

        float verticalOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        visual.localPosition = visualStartPosition + Vector3.up * verticalOffset;
    }
}
