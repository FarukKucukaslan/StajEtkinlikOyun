using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of this enemy.")]
    public float maxHealth = 20f;
    private float _currentHealth;

    [Header("XP Settings")]
    [Tooltip("Amount of XP rewarded to the player when this enemy dies.")]
    public float xpReward = 15f;

    [Header("Gold Settings")]
    [Tooltip("Amount of Gold rewarded to the player when this enemy dies.")]
    public int goldReward = 5;

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Award XP and Gold to the player stats when the enemy dies
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            PlayerStats stats = playerObj.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.AddXP(xpReward);
                stats.AddGold(goldReward);
            }
        }

        // Destroy the enemy GameObject
        Destroy(gameObject);
    }
}
