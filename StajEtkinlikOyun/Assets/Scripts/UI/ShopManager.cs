using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Shop UI Panel")]
    public GameObject shopPanel;
    public TextMeshProUGUI shopGoldText;

    [Header("Left Panel - 9 Upgrade Buttons")]
    public Button btnMaxHealth;
    public Button btnMoveSpeed;
    public Button btnAttackSpeed;
    public Button btnSwordDamage;
    public Button btnSwordCount;
    public Button btnPierceCount;
    public Button btnDefense;
    public Button btnSearchRange;
    public Button btnProjSpeed;

    [Header("Right Panel - Detail Displays")]
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailStatsText;
    public TextMeshProUGUI detailCostText;
    public Button buyButton;

    private string _selectedKey = "";
    private int _selectedCurrentLevel = 0;

    private void Start()
    {
        // Hide shop panel initially
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // Add Listeners to left buttons
        if (btnMaxHealth != null) btnMaxHealth.onClick.AddListener(() => SelectUpgrade("Shop_MaxHealth"));
        if (btnMoveSpeed != null) btnMoveSpeed.onClick.AddListener(() => SelectUpgrade("Shop_MoveSpeed"));
        if (btnAttackSpeed != null) btnAttackSpeed.onClick.AddListener(() => SelectUpgrade("Shop_AttackSpeed"));
        if (btnSwordDamage != null) btnSwordDamage.onClick.AddListener(() => SelectUpgrade("Shop_SwordDamage"));
        if (btnSwordCount != null) btnSwordCount.onClick.AddListener(() => SelectUpgrade("Shop_SwordCount"));
        if (btnPierceCount != null) btnPierceCount.onClick.AddListener(() => SelectUpgrade("Shop_PierceCount"));
        if (btnDefense != null) btnDefense.onClick.AddListener(() => SelectUpgrade("Shop_Defense"));
        if (btnSearchRange != null) btnSearchRange.onClick.AddListener(() => SelectUpgrade("Shop_SearchRange"));
        if (btnProjSpeed != null) btnProjSpeed.onClick.AddListener(() => SelectUpgrade("Shop_ProjectileSpeed"));

        // Setup buy button listener
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuySelectedUpgrade);
        }

        ClearRightPanel();
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
        
        // Hide character stats panel when shop is open to prevent UI overlap
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetStatsPanelActive(false);
        }

        UpdateGoldText();
        ClearRightPanel();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // Force player components to reload their newly purchased stats from PlayerPrefs
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // Show character stats panel again when shop closes (returns to main menu)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetStatsPanelActive(true);
        }
    }

    private void UpdateGoldText()
    {
        if (shopGoldText != null)
        {
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            shopGoldText.text = $"Gold: {currentGold}";
        }
    }

    private void ClearRightPanel()
    {
        _selectedKey = "";
        if (detailTitleText != null) detailTitleText.text = "Select an Upgrade";
        if (detailStatsText != null) detailStatsText.text = "Click one of the attributes on the left to see stats.";
        if (detailCostText != null) detailCostText.text = "";
        if (buyButton != null) buyButton.interactable = false;
    }

    private void SelectUpgrade(string key)
    {
        _selectedKey = key;
        _selectedCurrentLevel = PlayerPrefs.GetInt(key, 0);
        UpdateRightPanel();
    }

    private void UpdateRightPanel()
    {
        if (string.IsNullOrEmpty(_selectedKey)) return;

        string title = GetUpgradeTitle(_selectedKey);
        int cost = CalculateCost(_selectedKey, _selectedCurrentLevel);
        int nextLevel = _selectedCurrentLevel + 1;
        int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);

        if (detailTitleText != null)
        {
            detailTitleText.text = title;
        }

        if (detailStatsText != null)
        {
            string currentValStr = GetStatValueString(_selectedKey, _selectedCurrentLevel);
            
            if (_selectedCurrentLevel >= 5)
            {
                detailStatsText.text = $"Level: 5/5 (MAXED)\nValue: {currentValStr}";
            }
            else
            {
                string nextValStr = GetStatValueString(_selectedKey, nextLevel);
                detailStatsText.text = $"Level: {_selectedCurrentLevel}/5 -> {nextLevel}/5\nValue: {currentValStr} -> {nextValStr}";
            }
        }

        if (detailCostText != null)
        {
            if (_selectedCurrentLevel >= 5)
            {
                detailCostText.text = "Maxed Out";
            }
            else
            {
                detailCostText.text = $"Cost: {cost} Gold";
            }
        }

        if (buyButton != null)
        {
            // Can buy if under lvl 5 and have enough gold
            buyButton.interactable = (_selectedCurrentLevel < 5) && (currentGold >= cost);
        }
    }

    private void BuySelectedUpgrade()
    {
        if (string.IsNullOrEmpty(_selectedKey)) return;

        _selectedCurrentLevel = PlayerPrefs.GetInt(_selectedKey, 0);
        if (_selectedCurrentLevel >= 5) return;

        int cost = CalculateCost(_selectedKey, _selectedCurrentLevel);
        int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);

        if (currentGold >= cost)
        {
            // Deduct gold
            PlayerPrefs.SetInt("PlayerGold", currentGold - cost);
            
            // Increment upgrade level
            PlayerPrefs.SetInt(_selectedKey, _selectedCurrentLevel + 1);
            PlayerPrefs.Save();

            _selectedCurrentLevel++;
            
            // Refresh displays
            UpdateGoldText();
            UpdateRightPanel();

            Debug.Log($"Purchased upgrade for {_selectedKey}. New level: {_selectedCurrentLevel}");
        }
    }

    private int CalculateCost(string key, int level)
    {
        if (level >= 5) return 0;

        int baseCost = 100;
        int multiplier = 100;

        switch (key)
        {
            // COMMON (MoveSpeed, MaxHealth, SearchRange, ProjectileSpeed)
            case "Shop_MoveSpeed":
                baseCost = 100;
                multiplier = 100;
                break;
            case "Shop_MaxHealth":
                baseCost = 100;
                multiplier = 100;
                break;
            case "Shop_SearchRange":
                baseCost = 100;
                multiplier = 120;
                break;
            case "Shop_ProjectileSpeed":
                baseCost = 100;
                multiplier = 120;
                break;

            // RARE-ADJUSTED (AttackSpeed, SwordDamage, Defense)
            case "Shop_AttackSpeed":
                baseCost = 150;
                multiplier = 180;
                break;
            case "Shop_SwordDamage":
                baseCost = 200;
                multiplier = 250;
                break;
            case "Shop_Defense":
                baseCost = 250;
                multiplier = 300;
                break;

            // EPIC (SwordCount, PierceCount)
            case "Shop_SwordCount":
                baseCost = 1000; // high base cost for massive power
                multiplier = 1200; // high scaling factor
                break;
            case "Shop_PierceCount":
                baseCost = 500;
                multiplier = 600;
                break;
        }

        // Non-linear scaling formula: BaseCost + (Level * Multiplier) * (1.0 + Level * 0.25)
        // Level 0 -> BaseCost
        // Level 1 -> BaseCost + Multiplier * 1.25
        // Level 2 -> BaseCost + (2 * Multiplier) * 1.50
        // Level 3 -> BaseCost + (3 * Multiplier) * 1.75
        // Level 4 -> BaseCost + (4 * Multiplier) * 2.00
        return Mathf.RoundToInt(baseCost + (level * multiplier) * (1.0f + level * 0.25f));
    }

    private string GetUpgradeTitle(string key)
    {
        switch (key)
        {
            case "Shop_MaxHealth": return "Max Health";
            case "Shop_MoveSpeed": return "Movement Speed";
            case "Shop_AttackSpeed": return "Attack Cooldown";
            case "Shop_SwordDamage": return "Sword Damage";
            case "Shop_SwordCount": return "Sword Multi-Throw";
            case "Shop_PierceCount": return "Piercing Edge";
            case "Shop_Defense": return "Armor Defense";
            case "Shop_SearchRange": return "Target Search Range";
            case "Shop_ProjectileSpeed": return "Sword Projectile Speed";
            default: return "Unknown Stat";
        }
    }

    private string GetStatValueString(string key, int level)
    {
        switch (key)
        {
            case "Shop_MaxHealth":
                return $"{100f + (level * 10f)} HP";
            case "Shop_MoveSpeed":
                return $"{6f + (level * 0.5f)} m/s";
            case "Shop_AttackSpeed":
                return $"{1.5f - (level * 0.1f)}s";
            case "Shop_SwordDamage":
                return $"{20f + (level * 4f)} Damage";
            case "Shop_SwordCount":
                return $"{1 + level} Swords";
            case "Shop_PierceCount":
                return $"{1 + level} Pierce";
            case "Shop_Defense":
                return $"+{level} Armor";
            case "Shop_SearchRange":
                return $"{15f + (level * 1.5f)} Range";
            case "Shop_ProjectileSpeed":
                return $"{15f + (level * 2f)} m/s";
            default:
                return "0";
        }
    }
}
