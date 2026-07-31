using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct EnemySpawnConfig
    {
        [Tooltip("Name of the enemy, used only for organization in the Inspector.")]
        public string enemyName;

        [Tooltip("The prefab of the enemy to instantiate.")]
        public GameObject enemyPrefab;

        [Tooltip("Spawn chance weight. Higher values make this enemy more common.")]
        [Range(0f, 100f)]
        public float spawnChanceWeight;

        [Header("Stat Multipliers (0 = Default Value)")]
        [Tooltip("Health and reward multiplier.")]
        public float healthAndRewardMultiplier;

        [Tooltip("Damage multiplier.")]
        public float damageMultiplier;

        [Tooltip("Movement speed multiplier.")]
        public float speedMultiplier;
    }

    public static event System.Action OnEliteIncoming;

    [Header("Target Player")]
    [Tooltip(
        "Player transform used as the center of the spawn area. "
            + "Automatically finds the Player tag when left empty."
    )]
    public Transform player;

    [Header("Spawner Config")]
    public List<EnemySpawnConfig> enemyTypes = new List<EnemySpawnConfig>();

    [Tooltip("Time between normal enemy spawns.")]
    [Min(0.05f)]
    public float spawnInterval = 1.5f;

    [Header("Spawn Distance Settings")]
    [Tooltip("Minimum spawn distance from the player.")]
    public float minSpawnDistance = 12f;

    [Tooltip("Maximum spawn distance from the player.")]
    public float maxSpawnDistance = 20f;

    [Header("Ground Detection")]
    [Tooltip("Only these layers are considered valid enemy spawn ground.")]
    public LayerMask groundMask;

    [Header("Elite Enemy Settings")]
    [Tooltip("Determines whether elite enemies can spawn.")]
    public bool eliteEnemiesEnabled = true;

    [Tooltip("How many seconds before the elite spawn the warning appears.")]
    [Min(0f)]
    public float eliteWarningLeadTime = 3f;

    [Tooltip("Delay before the first elite enemy appears.")]
    [Min(1f)]
    public float firstEliteDelay = 40f;

    [Tooltip(
        "Time between elite enemies. " + "The timer starts again after the previous elite dies."
    )]
    [Min(1f)]
    public float eliteSpawnInterval = 45f;

    [Tooltip("Visual size multiplier applied to elite enemies.")]
    [Min(1f)]
    public float eliteScaleMultiplier = 1.6f;

    [Tooltip("Health multiplier applied to elite enemies.")]
    [Min(1f)]
    public float eliteHealthMultiplier = 5f;

    [Tooltip("Damage multiplier applied to elite enemies.")]
    [Min(0.1f)]
    public float eliteDamageMultiplier = 1.5f;

    [Tooltip("Movement speed multiplier applied to elite enemies.")]
    [Min(0.1f)]
    public float eliteSpeedMultiplier = 0.85f;

    [Tooltip("XP and gold multiplier applied to elite enemies.")]
    [Min(1f)]
    public float eliteRewardMultiplier = 5f;

    [Tooltip("Number of XP crystals dropped by an elite enemy.")]
    [Min(1)]
    public int eliteXPDropCount = 5;

    [Tooltip("Color tint used to visually distinguish elite enemies.")]
    public Color eliteTint = new Color(0.75f, 0.15f, 1f, 1f);

    [Header("Performance")]
    [Tooltip("Maximum number of enemies alive at the same time.")]
    public int maxEnemies = 150;

    [HideInInspector]
    public bool isSpawning;

    // Stacking multipliers granted by the "Challenge" level-up upgrade.
    // Applied on top of the per-type and elite multipliers below.
    private float _challengeHealthMultiplier = 1f;
    private float _challengeDamageMultiplier = 1f;
    private float _challengeSpeedMultiplier = 1f;

    private float _spawnTimer;
    private float _eliteTimer;

    private bool _firstEliteSpawned;
    private bool _eliteWarningShown;

    private GameObject _activeElite;

    private void Update()
    {
        if (!isSpawning)
        {
            return;
        }

        if (!TryFindPlayer())
        {
            return;
        }

        UpdateNormalSpawnTimer();
        UpdateEliteSpawnTimer();
    }

    private bool TryFindPlayer()
    {
        if (player != null)
        {
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return false;
        }

        player = playerObject.transform;
        return true;
    }

    private void UpdateNormalSpawnTimer()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < spawnInterval)
        {
            return;
        }

        _spawnTimer = 0f;
        SpawnNormalEnemy();
    }

    private void UpdateEliteSpawnTimer()
    {
        if (!eliteEnemiesEnabled)
        {
            return;
        }

        // Unity destroys the reference when the elite enemy is removed.
        if (_activeElite != null)
        {
            return;
        }

        _eliteTimer += Time.deltaTime;

        float requiredDelay = _firstEliteSpawned ? eliteSpawnInterval : firstEliteDelay;

        float warningTime = Mathf.Max(0f, requiredDelay - eliteWarningLeadTime);

        if (!_eliteWarningShown && _eliteTimer >= warningTime)
        {
            _eliteWarningShown = true;
            OnEliteIncoming?.Invoke();
        }

        if (_eliteTimer < requiredDelay)
        {
            return;
        }

        if (SpawnEliteEnemy())
        {
            _eliteTimer = 0f;
            _eliteWarningShown = false;
            _firstEliteSpawned = true;
        }
    }

    private void SpawnNormalEnemy()
    {
        if (!CanSpawnEnemy())
        {
            return;
        }

        int selectedIndex = GetRandomEnemyIndex();

        if (selectedIndex < 0)
        {
            return;
        }

        SpawnEnemy(enemyTypes[selectedIndex], false);
    }

    private bool SpawnEliteEnemy()
    {
        if (!CanSpawnEnemy())
        {
            return false;
        }

        int selectedIndex = GetRandomEnemyIndex();

        if (selectedIndex < 0)
        {
            return false;
        }

        _activeElite = SpawnEnemy(enemyTypes[selectedIndex], true);

        return _activeElite != null;
    }

    private bool CanSpawnEnemy()
    {
        if (enemyTypes == null || enemyTypes.Count == 0)
        {
            return false;
        }

        int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        return currentEnemyCount < maxEnemies;
    }

    private GameObject SpawnEnemy(EnemySpawnConfig config, bool spawnAsElite)
    {
        if (config.enemyPrefab == null)
        {
            return null;
        }

        Vector3 spawnPosition = GetRandomSpawnPositionAroundPlayer();

        GameObject enemyInstance = Instantiate(
            config.enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        ApplyEnemyTypeMultipliers(enemyInstance, config);

        if (spawnAsElite)
        {
            ApplyEliteModifiers(enemyInstance);
        }

        return enemyInstance;
    }

    private void ApplyEnemyTypeMultipliers(GameObject enemyInstance, EnemySpawnConfig config)
    {
        float healthAndRewardScale = GetMultiplierOrDefault(config.healthAndRewardMultiplier);

        float damageScale = GetMultiplierOrDefault(config.damageMultiplier);

        float speedScale = GetMultiplierOrDefault(config.speedMultiplier);

        EnemyHealth enemyHealth = enemyInstance.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.maxHealth = Mathf.Round(
                enemyHealth.maxHealth * healthAndRewardScale * _challengeHealthMultiplier
            );

            enemyHealth.xpReward = Mathf.Round(enemyHealth.xpReward * healthAndRewardScale);

            enemyHealth.goldReward = Mathf.RoundToInt(
                enemyHealth.goldReward * healthAndRewardScale
            );
        }

        EnemySimpleAI enemyAI = enemyInstance.GetComponent<EnemySimpleAI>();

        ArcherEnemyAI archerAI = enemyInstance.GetComponent<ArcherEnemyAI>();

        if (archerAI != null)
        {
            archerAI.moveSpeed *= speedScale * _challengeSpeedMultiplier;

            archerAI.projectileDamage = Mathf.Round(
                archerAI.projectileDamage * damageScale * _challengeDamageMultiplier
            );
        }

        if (enemyAI != null)
        {
            enemyAI.speed *= speedScale * _challengeSpeedMultiplier;

            enemyAI.damage = Mathf.Round(
                enemyAI.damage * damageScale * _challengeDamageMultiplier
            );
        }
    }

    private void ApplyEliteModifiers(GameObject enemyInstance)
    {
        enemyInstance.name = $"[ELITE] {enemyInstance.name}";

        enemyInstance.transform.localScale *= eliteScaleMultiplier;

        EnemyHealth enemyHealth = enemyInstance.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.maxHealth = Mathf.Round(enemyHealth.maxHealth * eliteHealthMultiplier);

            enemyHealth.xpReward = Mathf.Round(enemyHealth.xpReward * eliteRewardMultiplier);

            enemyHealth.goldReward = Mathf.RoundToInt(
                enemyHealth.goldReward * eliteRewardMultiplier
            );

            enemyHealth.ConfigureAsElite(eliteXPDropCount, eliteTint);
        }

        EnemySimpleAI enemyAI = enemyInstance.GetComponent<EnemySimpleAI>();

        ArcherEnemyAI archerAI = enemyInstance.GetComponent<ArcherEnemyAI>();

        if (archerAI != null)
        {
            archerAI.moveSpeed *= eliteSpeedMultiplier;

            archerAI.projectileDamage = Mathf.Round(
                archerAI.projectileDamage * eliteDamageMultiplier
            );
        }

        if (enemyAI != null)
        {
            enemyAI.damage = Mathf.Round(enemyAI.damage * eliteDamageMultiplier);

            enemyAI.speed *= eliteSpeedMultiplier;

            // Larger enemies need a larger attack distance.
            enemyAI.attackRange *= eliteScaleMultiplier;
        }

        Debug.Log($"Elite enemy spawned: {enemyInstance.name}");
    }

    private float GetMultiplierOrDefault(float multiplier)
    {
        return multiplier <= 0.05f ? 1f : multiplier;
    }

    private int GetRandomEnemyIndex()
    {
        float totalWeight = 0f;

        foreach (EnemySpawnConfig config in enemyTypes)
        {
            if (config.enemyPrefab != null)
            {
                totalWeight += config.spawnChanceWeight;
            }
        }

        if (totalWeight <= 0f)
        {
            return -1;
        }

        float randomValue = Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            EnemySpawnConfig config = enemyTypes[i];

            if (config.enemyPrefab == null)
            {
                continue;
            }

            currentWeight += config.spawnChanceWeight;

            if (randomValue <= currentWeight)
            {
                return i;
            }
        }

        return -1;
    }

    private Vector3 GetRandomSpawnPositionAroundPlayer()
    {
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);

        float xOffset = Mathf.Cos(randomAngle) * randomDistance;

        float zOffset = Mathf.Sin(randomAngle) * randomDistance;

        Vector3 spawnPosition = player.position + new Vector3(xOffset, 0f, zOffset);

        Ray groundRay = new Ray(spawnPosition + Vector3.up * 25f, Vector3.down);

        if (
            Physics.Raycast(
                groundRay,
                out RaycastHit hit,
                50f,
                groundMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            spawnPosition.y = hit.point.y;
        }
        else
        {
            spawnPosition.y = player.position.y;
        }

        return spawnPosition;
    }

    // Called by the "Challenge" upgrade. Stacks on every pick.
    public void ApplyChallengeUpgrade(
        float healthMultiplierIncrease,
        float damageMultiplierIncrease,
        float speedMultiplierIncrease
    )
    {
        _challengeHealthMultiplier += healthMultiplierIncrease;
        _challengeDamageMultiplier += damageMultiplierIncrease;
        _challengeSpeedMultiplier += speedMultiplierIncrease;
    }

    // Spawns an elite immediately, bypassing the elite timer. Used for testing (e.g. from the pause button).
    public void ForceSpawnElite()
    {
        if (SpawnEliteEnemy())
        {
            _eliteTimer = 0f;
            _eliteWarningShown = false;
            _firstEliteSpawned = true;
        }
    }

    public void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        ResetSpawnTimers();
    }

    private void ResetSpawnTimers()
    {
        _spawnTimer = 0f;
        _eliteTimer = 0f;

        _firstEliteSpawned = false;
        _eliteWarningShown = false;

        _activeElite = null;
    }
}
