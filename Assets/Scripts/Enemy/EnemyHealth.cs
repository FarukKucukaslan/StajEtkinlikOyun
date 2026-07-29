using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of this enemy.")]
    public float maxHealth = 20f;

    public float CurrentHealth => _currentHealth;
    public bool IsElite { get; private set; }

    [Header("XP Settings")]
    [Tooltip("Total XP rewarded when this enemy dies.")]
    public float xpReward = 15f;

    [SerializeField]
    private ExperiencePickup experiencePickupPrefab;

    [SerializeField]
    private float experienceDropHeight = 0.6f;

    [SerializeField]
    private float experienceDropScatter = 0.35f;

    [SerializeField, Min(1)]
    private int experienceDropCount = 1;

    [Header("Gold Settings")]
    [Tooltip("Gold rewarded when this enemy dies.")]
    public int goldReward = 5;

    [Header("Hit Flash")]
    [Tooltip("Color used when the enemy takes damage.")]
    public Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("Duration of the hit flash.")]
    public float flashDuration = 0.08f;

    public static event System.Action<EnemyHealth> OnEliteSpawned;

    public static event System.Action<EnemyHealth, float, float> OnEliteHealthChanged;

    public static event System.Action<EnemyHealth> OnEliteRemoved;

    public string DisplayName
    {
        get
        {
            return gameObject.name.Replace("[ELITE]", "").Replace("(Clone)", "").Trim().ToUpper();
        }
    }

    private float _currentHealth;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propertyBlock;
    private Coroutine _flashRoutine;

    private Color _eliteTint = new Color(0.75f, 0.15f, 1f, 1f);

    private bool _hasStarted;
    private bool _eliteSpawnReported;
    private bool _eliteRemovalReported;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Start()
    {
        _currentHealth = maxHealth;

        _renderers = GetComponentsInChildren<Renderer>();

        _propertyBlock = new MaterialPropertyBlock();

        RestoreVisualColor();

        _hasStarted = true;
        ReportEliteSpawn();
    }

    public void ConfigureAsElite(int xpDropCount, Color tint)
    {
        IsElite = true;

        experienceDropCount = Mathf.Max(1, xpDropCount);

        _eliteTint = tint;

        // Supports being configured after Start as well.
        if (_renderers != null)
        {
            RestoreVisualColor();
        }

        if (_hasStarted)
        {
            ReportEliteSpawn();
        }
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0f, _currentHealth);

        if (IsElite)
        {
            OnEliteHealthChanged?.Invoke(this, _currentHealth, maxHealth);
        }

        JuiceManager.DamageNumber(transform.position, damage);

        if (_currentHealth <= 0f)
        {
            Die();
            return;
        }

        JuiceManager.HitStop(IsElite ? 0.045f : 0.03f);

        JuiceManager.Shake(IsElite ? 0.2f : 0.12f);

        if (_renderers == null || _renderers.Length == 0)
        {
            return;
        }

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }

        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    private void Die()
    {
        JuiceManager.DeathPop(transform.position, GetEnemyColor());

        JuiceManager.Shake(IsElite ? 0.65f : 0.3f);

        RewardPlayer();
        ReportEliteRemoval();

        Destroy(gameObject);
    }

    private void RewardPlayer()
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();

        if (experiencePickupPrefab != null)
        {
            SpawnExperiencePickups();
        }
        else if (playerStats != null)
        {
            // Fallback if the XP prefab is not assigned.
            playerStats.AddXP(xpReward);
        }

        if (playerStats != null)
        {
            playerStats.AddGold(goldReward);
        }
    }

    private void SpawnExperiencePickups()
    {
        int dropCount = Mathf.Max(1, experienceDropCount);

        float xpPerPickup = xpReward / dropCount;

        float scatterRadius = IsElite ? experienceDropScatter * 2.5f : experienceDropScatter;

        for (int i = 0; i < dropCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;

            Vector3 spawnPosition =
                transform.position
                + new Vector3(randomOffset.x, experienceDropHeight, randomOffset.y);

            ExperiencePickup pickup = Instantiate(
                experiencePickupPrefab,
                spawnPosition,
                Quaternion.identity
            );

            pickup.Initialize(xpPerPickup);
        }
    }

    private IEnumerator FlashRoutine()
    {
        ApplyColorOverride(flashColor);

        yield return new WaitForSeconds(flashDuration);

        RestoreVisualColor();
        _flashRoutine = null;
    }

    private void RestoreVisualColor()
    {
        if (_renderers == null)
        {
            return;
        }

        if (IsElite)
        {
            ApplyColorOverride(_eliteTint);
            return;
        }

        foreach (Renderer enemyRenderer in _renderers)
        {
            if (enemyRenderer != null)
            {
                enemyRenderer.SetPropertyBlock(null);
            }
        }
    }

    private void ApplyColorOverride(Color color)
    {
        if (_renderers == null || _propertyBlock == null)
        {
            return;
        }

        foreach (Renderer enemyRenderer in _renderers)
        {
            if (enemyRenderer == null || enemyRenderer.sharedMaterial == null)
            {
                continue;
            }

            _propertyBlock.Clear();

            if (enemyRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _propertyBlock.SetColor(BaseColorId, color);
            }

            if (enemyRenderer.sharedMaterial.HasProperty(ColorId))
            {
                _propertyBlock.SetColor(ColorId, color);
            }

            enemyRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private Color GetEnemyColor()
    {
        if (IsElite)
        {
            return _eliteTint;
        }

        if (_renderers == null)
        {
            return Color.white;
        }

        foreach (Renderer enemyRenderer in _renderers)
        {
            if (enemyRenderer == null || enemyRenderer.sharedMaterial == null)
            {
                continue;
            }

            if (enemyRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                return enemyRenderer.sharedMaterial.GetColor(BaseColorId);
            }

            if (enemyRenderer.sharedMaterial.HasProperty(ColorId))
            {
                return enemyRenderer.sharedMaterial.GetColor(ColorId);
            }
        }

        return Color.white;
    }

    private void ReportEliteSpawn()
    {
        if (!IsElite || _eliteSpawnReported)
        {
            return;
        }

        _eliteSpawnReported = true;

        OnEliteSpawned?.Invoke(this);

        OnEliteHealthChanged?.Invoke(this, _currentHealth, maxHealth);
    }

    private void ReportEliteRemoval()
    {
        if (!IsElite || _eliteRemovalReported)
        {
            return;
        }

        _eliteRemovalReported = true;
        OnEliteRemoved?.Invoke(this);
    }

    private void OnDestroy()
    {
        ReportEliteRemoval();
    }
}
