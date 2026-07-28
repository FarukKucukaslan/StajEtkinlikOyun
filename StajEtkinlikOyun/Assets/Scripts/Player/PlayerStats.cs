using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the player.")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("XP & Level Settings")]
    [Tooltip("Starting level.")]
    public int currentLevel = 1;
    [Tooltip("Current experience points.")]
    public float currentXP = 0f;
    [Tooltip("XP required to level up to the next level.")]
    public float xpToNextLevel = 100f;
    [Tooltip("How much the XP requirement increases per level (e.g., 1.2 = 20% increase).")]
    public float xpRequirementMultiplier = 1.2f;

    [Header("Defense Settings")]
    [Tooltip("Flat damage reduction. Incoming damage is reduced by this value (minimum 1 damage).")]
    public float armor = 0f;

    [Header("Gold Settings")]
    public int goldCollected;

    // Events to notify the UI when stats change (Event-driven UI)
    public event Action<float, float> OnHealthChanged;
    public event Action<int, float, float> OnXPChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnLevelUp;

    private void Start()
    {
        // Load baseline upgrades from PlayerPrefs (meta-progression)
        int hpLevel = PlayerPrefs.GetInt("Shop_MaxHealth", 0);
        maxHealth = 100f + (hpLevel * 10f);

        int armorLevel = PlayerPrefs.GetInt("Shop_Defense", 0);
        armor = armorLevel * 1f;

        currentHealth = maxHealth;
        goldCollected = 0;

        // Reset in-game run stats
        currentLevel = 1;
        currentXP = 0f;
        xpToNextLevel = 100f;
        
        // Trigger initial UI updates
        NotifyHealthChanged();
        NotifyXPChanged();
        OnGoldChanged?.Invoke(goldCollected);
    }

    public void TakeDamage(float damage)
    {
        // Apply armor damage reduction (minimum 1 damage taken)
        float finalDamage = Mathf.Max(1f, damage - armor);
        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        NotifyHealthChanged();
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        
        // Handle multiple level ups if huge amount of XP is added
        while (currentXP >= xpToNextLevel)
        {
            LevelUp();
        }
        NotifyXPChanged();
    }

    public void AddGold(int amount)
    {
        goldCollected += amount;
        OnGoldChanged?.Invoke(goldCollected);
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;

        // Increase required XP for next level
        xpToNextLevel = Mathf.Round(xpToNextLevel * xpRequirementMultiplier);

        // Classic roguelite mechanic: fully heal the player on level up
        currentHealth = maxHealth;
        NotifyHealthChanged();
        NotifyXPChanged();

        // Freeze game time on level up
        Time.timeScale = 0f;
        OnLevelUp?.Invoke();

        Debug.Log($"Level Up! Current Level: {currentLevel}");
    }

    private void Die()
    {
        Debug.Log("Player has died!");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDeath(goldCollected);
        }
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void NotifyXPChanged()
    {
        OnXPChanged?.Invoke(currentLevel, currentXP, xpToNextLevel);
    }

    // --- Temporary Testing Keys ---
    private void Update()
    {
        if (Keyboard.current == null) return;

        // Press T to take damage
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TakeDamage(10f);
        }

        // Press H to heal
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            Heal(10f);
        }

        // Press X to gain XP
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            AddXP(25f);
        }
    }
}
