using System.Collections.Generic;
using UnityEngine;

public class SwordThrower : MonoBehaviour
{
    [Header("Weapon Configuration")]
    [Tooltip("The sword projectile prefab to throw.")]
    public GameObject swordPrefab;

    [Tooltip("Base damage of the thrown sword.")]
    public float swordDamage = 20f;

    [Tooltip("Travel speed of the thrown swords.")]
    public float projectileSpeed = 15f;

    [Tooltip("Number of swords thrown simultaneously in a fan shape.")]
    public int swordCount = 1;

    [Tooltip("How many enemies each sword can pierce.")]
    public int pierceCount = 1;

    [Tooltip("Angle separation (in degrees) between swords when throwing multiple.")]
    public float spreadAngle = 12f;

    [Tooltip("Cooldown time between throws (in seconds).")]
    public float throwCooldown = 1.5f;

    [Tooltip("Maximum range to detect and target enemies.")]
    public float searchRange = 15f;

    [Header("Spawn Position")]
    [Tooltip("Offset relative to the player's position to spawn the sword.")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    private float _cooldownTimer;

    private void Start()
    {
        // Load weapon upgrades from PlayerPrefs (meta-progression)
        int attackSpeedLevel = PlayerPrefs.GetInt("Shop_AttackSpeed", 0);
        throwCooldown = 1.5f - (attackSpeedLevel * 0.1f); // Reduce cooldown by 0.1s per level

        int damageLevel = PlayerPrefs.GetInt("Shop_SwordDamage", 0);
        swordDamage = 20f + (damageLevel * 4f); // +4 damage per level

        int countLevel = PlayerPrefs.GetInt("Shop_SwordCount", 0);
        swordCount = 1 + countLevel; // +1 sword per level

        int pierceLevel = PlayerPrefs.GetInt("Shop_PierceCount", 0);
        pierceCount = 1 + pierceLevel; // +1 pierce per level

        int rangeLevel = PlayerPrefs.GetInt("Shop_SearchRange", 0);
        searchRange = 15f + (rangeLevel * 1.5f); // +1.5 range per level

        int projSpeedLevel = PlayerPrefs.GetInt("Shop_ProjectileSpeed", 0);
        projectileSpeed = 15f + (projSpeedLevel * 2f); // +2 speed per level
    }

    private void Update()
    {
        _cooldownTimer += Time.deltaTime;
        
        if (_cooldownTimer >= throwCooldown)
        {
            _cooldownTimer = 0f;
            TryThrowSword();
        }
    }

    private void TryThrowSword()
    {
        // Find closest targets (up to swordCount)
        List<Transform> targets = FindNearestEnemies(swordCount);
        if (targets.Count == 0) return;

        // Calculate the projectile spawn position in world space
        Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnOffset);

        for (int i = 0; i < swordCount; i++)
        {
            // Cycle through available targets if there are fewer enemies than swordCount
            Transform target = targets[i % targets.Count];
            if (target == null) continue;

            // Target the chest/center of the enemy (assumes pivot is at feet)
            Vector3 targetCenter = target.position + new Vector3(0f, 1f, 0f);

            // Calculate 3D direction to the enemy's center (retaining height difference)
            Vector3 targetDirection = targetCenter - spawnPosition;

            if (targetDirection == Vector3.zero) continue;

            // Instantiate the sword projectile looking directly at the target center in 3D
            GameObject swordInstance = Instantiate(swordPrefab, spawnPosition, Quaternion.LookRotation(targetDirection));

            // Set the damage and pierce properties dynamically on the projectile
            SwordProjectile projectile = swordInstance.GetComponent<SwordProjectile>();
            if (projectile != null)
            {
                projectile.damage = swordDamage;
                projectile.pierceCount = pierceCount;
                projectile.speed = projectileSpeed;
            }
        }
    }

    private List<Transform> FindNearestEnemies(int maxCount)
    {
        // Find all active game objects tagged "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<Transform> validEnemies = new List<Transform>();

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);

            // Add enemy to list if within range
            if (distanceToEnemy <= searchRange)
            {
                validEnemies.Add(enemy.transform);
            }
        }

        // Sort the enemies by distance to the player (closest first)
        validEnemies.Sort((a, b) => 
        {
            float distA = Vector3.Distance(transform.position, a.position);
            float distB = Vector3.Distance(transform.position, b.position);
            return distA.CompareTo(distB);
        });

        // Limit the list to maxCount and return
        if (validEnemies.Count > maxCount)
        {
            return validEnemies.GetRange(0, maxCount);
        }

        return validEnemies;
    }
}
