using UnityEngine;

public class EliteRewardChest : MonoBehaviour
{
    [Header("Collection")]
    [SerializeField, Min(0.1f)]
    private float interactionRadius = 1.5f;

    [SerializeField]
    private float playerHeightOffset = 0.8f;

    [Header("Visual")]
    [SerializeField]
    private Transform visual;

    [SerializeField]
    private float rotationSpeed = 35f;

    [SerializeField]
    private float bobHeight = 0.15f;

    [SerializeField]
    private float bobSpeed = 2.5f;

    private Transform _player;
    private Vector3 _visualStartPosition;
    private bool _isCollected;

    private void Awake()
    {
        if (visual != null)
        {
            _visualStartPosition = visual.localPosition;
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        AnimateVisual();

        if (_isCollected)
        {
            return;
        }

        if (_player == null)
        {
            FindPlayer();

            if (_player == null)
            {
                return;
            }
        }

        TryCollect();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;
        }
    }

    private void TryCollect()
    {
        Vector3 playerTargetPosition = _player.position + Vector3.up * playerHeightOffset;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTargetPosition);

        if (distanceToPlayer > interactionRadius)
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            return;
        }

        bool selectionOpened = UIManager.Instance.OpenEliteRewardSelection();

        if (!selectionOpened)
        {
            return;
        }

        _isCollected = true;

        JuiceManager.Shake(0.25f);

        Destroy(gameObject);
    }

    private void AnimateVisual()
    {
        Transform animatedTransform = visual != null ? visual : transform;

        animatedTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (visual == null)
        {
            return;
        }

        float verticalOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        visual.localPosition = _visualStartPosition + Vector3.up * verticalOffset;
    }
}
