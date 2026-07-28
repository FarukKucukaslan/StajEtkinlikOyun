using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public struct EnemySpawnConfig
    {
        [Tooltip("Name of the enemy (only used to organize in the Inspector).")]
        public string enemyName;

        [Tooltip("The prefab of the enemy to instantiate.")]
        public GameObject enemyPrefab;

        [Tooltip("Spawn chance weight. Higher weight means this enemy spawns more often.")]
        [Range(0f, 100f)]
        public float spawnChanceWeight;

        [Header("Stat Multipliers (0 = Auto-Calculated)")]
        [Tooltip("Health and Rewards multiplier (e.g. 1.5 = 150% health/XP/gold).")]
        public float healthAndRewardMultiplier;

        [Tooltip("Damage multiplier (e.g. 1.3 = 130% damage).")]
        public float damageMultiplier;

        [Tooltip("Speed multiplier (e.g. 1.1 = 110% speed).")]
        public float speedMultiplier;
    }

    [Header("Target Player")]
    [Tooltip("The player transform to spawn enemies around. If left empty, it will auto-find GameObject with 'Player' tag.")]
    public Transform player;

    [Header("Spawner Config")]
    [Tooltip("List of all spawnable enemy types. Press the '+' button to add new ones in the Inspector.")]
    public List<EnemySpawnConfig> enemyTypes = new List<EnemySpawnConfig>();

    [Tooltip("Time interval between enemy spawns (in seconds).")]
    public float spawnInterval = 1.5f;

    [Header("Spawn Distance settings")]
    [Tooltip("Minimum distance from the player to spawn enemies (off-screen recommended).")]
    public float minSpawnDistance = 12f;

    [Tooltip("Maximum distance from the player to spawn enemies.")]
    public float maxSpawnDistance = 20f;

    [HideInInspector]
    public bool isSpawning = false;

    private float _spawnTimer;

    private void Update()
    {
        if (!isSpawning) return;
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                return; // Can't spawn without a player reference
            }
        }

        // Timer control
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyTypes == null || enemyTypes.Count == 0) return;

        // Select an enemy index using weighted random selection
        int selectedIndex = GetRandomEnemyIndex();
        if (selectedIndex == -1) return;

        EnemySpawnConfig config = enemyTypes[selectedIndex];
        if (config.enemyPrefab == null) return;

        // Find a random position on a circle around the player
        Vector3 spawnPosition = GetRandomSpawnPositionAroundPlayer();

        // Spawn the enemy
        GameObject enemyInstance = Instantiate(config.enemyPrefab, spawnPosition, Quaternion.identity);

        // Scale stats dynamically based on index in the list or custom inspector overrides
        float scaleFactor = config.healthAndRewardMultiplier;
        float damageScale = config.damageMultiplier;
        float speedScale = config.speedMultiplier;

        // Fallback to auto-calculated index values if left at 0 in Inspector
        if (scaleFactor <= 0.05f) scaleFactor = 1f + (selectedIndex * 0.5f);
        if (damageScale <= 0.05f) damageScale = 1f + (selectedIndex * 0.3f);
        if (speedScale <= 0.05f) speedScale = 1f + (selectedIndex * 0.1f);

        EnemyHealth healthComp = enemyInstance.GetComponent<EnemyHealth>();
        if (healthComp != null)
        {
            healthComp.maxHealth = Mathf.Round(healthComp.maxHealth * scaleFactor);
            healthComp.xpReward = Mathf.Round(healthComp.xpReward * scaleFactor);
            healthComp.goldReward = Mathf.RoundToInt(healthComp.goldReward * scaleFactor);
        }

        EnemySimpleAI aiComp = enemyInstance.GetComponent<EnemySimpleAI>();
        if (aiComp != null)
        {
            aiComp.speed *= speedScale;
            aiComp.damage = Mathf.Round(aiComp.damage * damageScale);
        }
    }

    private int GetRandomEnemyIndex()
    {
        float totalWeight = 0f;
        foreach (var config in enemyTypes)
        {
            if (config.enemyPrefab != null)
            {
                totalWeight += config.spawnChanceWeight;
            }
        }

        if (totalWeight <= 0f) return -1;

        // Choose a random value within the range of total weight
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeightSum = 0f;

        for (int i = 0; i < enemyTypes.Count; i++)
        {
            if (enemyTypes[i].enemyPrefab != null)
            {
                currentWeightSum += enemyTypes[i].spawnChanceWeight;
                if (randomValue <= currentWeightSum)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private Vector3 GetRandomSpawnPositionAroundPlayer()
    {
        // Choose a random angle (in radians)
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // Choose a random distance between min and max range
        float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);

        // Convert polar coordinates to Cartesian (X, Z) coordinates
        float xOffset = Mathf.Cos(randomAngle) * randomDistance;
        float zOffset = Mathf.Sin(randomAngle) * randomDistance;

        // Place on the horizontal plane relative to the player's position
        Vector3 spawnPosition = player.position + new Vector3(xOffset, 0f, zOffset);
        
        // Snap spawn position Y to ground level using a raycast (so they spawn on top of hills/terrain)
        Ray ray = new Ray(new Vector3(spawnPosition.x, spawnPosition.y + 25f, spawnPosition.z), Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 50f))
        {
            spawnPosition.y = hit.point.y;
        }
        else
        {
            // Fallback to player's current height if no ground is detected
            spawnPosition.y = player.position.y;
        }

        return spawnPosition;
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
    }
}
