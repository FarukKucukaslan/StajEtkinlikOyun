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

    [Tooltip("How much the XP requirement increases per level.")]
    public float xpRequirementMultiplier = 1.2f;

    [Header("XP Pickup Settings")]
    [SerializeField, Min(0.5f)]
    private float baseXPPickupRange = 4.5f;

    [SerializeField, Min(0.5f)]
    private float maximumXPPickupRange = 15f;

    public float XPPickupRange { get; private set; }

    [Header("Defense Settings")]
    [Tooltip(
        "Flat damage reduction. Incoming damage is reduced by this value, with a minimum of 1 damage."
    )]
    public float armor = 0f;

    [Header("Gold Settings")]
    public int goldCollected;

    public event Action<float, float> OnHealthChanged;
    public event Action<int, float, float> OnXPChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnLevelUp;

    private void Start()
    {
        LoadPermanentUpgrades();
        ResetRunStats();
        NotifyInitialValues();
    }

    private void LoadPermanentUpgrades()
    {
        int healthUpgradeLevel = PlayerPrefs.GetInt("Shop_MaxHealth", 0);

        maxHealth = 100f + healthUpgradeLevel * 10f;

        int armorUpgradeLevel = PlayerPrefs.GetInt("Shop_Defense", 0);

        armor = armorUpgradeLevel;
    }

    private void ResetRunStats()
    {
        currentHealth = maxHealth;
        goldCollected = 0;

        currentLevel = 1;
        currentXP = 0f;
        xpToNextLevel = 100f;

        XPPickupRange = baseXPPickupRange;
    }

    private void NotifyInitialValues()
    {
        NotifyHealthChanged();
        NotifyXPChanged();
        OnGoldChanged?.Invoke(goldCollected);
    }

    public void TakeDamage(float damage)
    {
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

    public void IncreaseXPPickupRange(float multiplier)
    {
        if (multiplier <= 1f)
        {
            return;
        }

        XPPickupRange = Mathf.Min(XPPickupRange * multiplier, maximumXPPickupRange);
    }

    public float GetNextXPPickupRange(float multiplier)
    {
        return Mathf.Min(XPPickupRange * multiplier, maximumXPPickupRange);
    }

    private void LevelUp()
    {
        currentXP -= xpToNextLevel;
        currentLevel++;

        xpToNextLevel = Mathf.Round(xpToNextLevel * xpRequirementMultiplier);

        currentHealth = maxHealth;

        NotifyHealthChanged();
        NotifyXPChanged();

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

    // Temporary testing keys.
    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TakeDamage(10f);
        }

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            Heal(10f);
        }

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            AddXP(25f);
        }
    }
}
