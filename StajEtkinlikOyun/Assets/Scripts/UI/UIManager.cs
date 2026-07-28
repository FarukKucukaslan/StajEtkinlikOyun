using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public enum UpgradeType
    {
        MoveSpeed,
        MaxHealth,
        AttackSpeed,
        SwordDamage,
        FirstAid,
        SwordCount,
        PierceCount,
        Defense,
        SearchRange,
        ProjectileSpeed
    }

    [System.Serializable]
    public struct UpgradeOption
    {
        public UpgradeType type;
        public string title;
        public string description;
        public int weight; // Higher weight = more common, lower = rarer
        public string rarity; // "Common", "Rare", "Epic"
    }

    [Header("Player Reference")]
    [Tooltip("Reference to the PlayerStats script. If left empty, it will auto-detect from the GameObject tagged 'Player'.")]
    public PlayerStats playerStats;

    [Header("Health UI (Bottom Left)")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;

    [Header("Level & XP UI (Top)")]
    public Slider xpSlider;
    public TextMeshProUGUI levelText;

    [Header("Gameplay HUD Settings")]
    public TextMeshProUGUI hudGoldText;

    [Header("Level Up UI Panel")]
    public GameObject levelUpPanel;
    
    [Header("Option 1 UI")]
    public Button optionButton1;
    public TextMeshProUGUI optionTitle1;
    public TextMeshProUGUI optionDesc1;

    [Header("Option 2 UI")]
    public Button optionButton2;
    public TextMeshProUGUI optionTitle2;
    public TextMeshProUGUI optionDesc2;

    [Header("Option 3 UI")]
    public Button optionButton3;
    public TextMeshProUGUI optionTitle3;
    public TextMeshProUGUI optionDesc3;

    [Header("UI Visual Polish")]
    [Tooltip("If checked, UI bars will smoothly slide to the target value instead of snapping.")]
    public bool smoothTransition = true;
    [Tooltip("Speed of the smooth slider transitions.")]
    public float smoothSpeed = 10f;

    private float _targetHealthPct = 1f;
    private float _targetXPPct = 0f;
    private TextMeshProUGUI _statsContentText;
    private GameObject _statsPanelObj;

    // Available upgrades pool with weights (Total: 10 options)
    private readonly List<UpgradeOption> _availableUpgrades = new List<UpgradeOption>()
    {
        // COMMON (Weight: 80)
        new UpgradeOption { type = UpgradeType.MoveSpeed, title = "Speed Boost", description = "Increase movement speed by 15% (+1.0 speed).", weight = 80, rarity = "Common" },
        new UpgradeOption { type = UpgradeType.MaxHealth, title = "Vitality", description = "Increase maximum health by 20 and heal fully.", weight = 80, rarity = "Common" },
        new UpgradeOption { type = UpgradeType.AttackSpeed, title = "Fast Hands", description = "Throw swords 15% faster (-0.2s cooldown).", weight = 80, rarity = "Common" },
        new UpgradeOption { type = UpgradeType.SearchRange, title = "Eagle Eye", description = "Increase sword target search range by 25% (+4.0 range).", weight = 80, rarity = "Common" },
        new UpgradeOption { type = UpgradeType.ProjectileSpeed, title = "Swift Blades", description = "Swords travel 30% faster (+5.0 projectile speed).", weight = 80, rarity = "Common" },
        
        // RARE (Weight: 30)
        new UpgradeOption { type = UpgradeType.SwordDamage, title = "Sharp Blade", description = "Increase sword damage by 8 points.", weight = 30, rarity = "Rare" },
        new UpgradeOption { type = UpgradeType.FirstAid, title = "First Aid", description = "Heal to full health instantly.", weight = 30, rarity = "Rare" },
        new UpgradeOption { type = UpgradeType.Defense, title = "Heavy Armor", description = "Reduce incoming damage from all enemies by 2 points.", weight = 30, rarity = "Rare" },
        
        // EPIC (Weight: 10)
        new UpgradeOption { type = UpgradeType.SwordCount, title = "Multi-Throw", description = "Throw one additional sword at nearest enemies.", weight = 10, rarity = "Epic" },
        new UpgradeOption { type = UpgradeType.PierceCount, title = "Piercing Edge", description = "Swords pass through one more enemy.", weight = 10, rarity = "Epic" }
    };

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Create stats panel programmatically in Awake to prevent race conditions
        CreateStatsPanel();
    }

    private void Start()
    {
        // Hide only the level up selection box initially
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        // Show stats panel initially since we start in the Main Menu
        if (_statsPanelObj != null)
        {
            _statsPanelObj.SetActive(true);
            UpdateStatsPanelText();
        }

        // Try to automatically find PlayerStats if not assigned
        if (playerStats == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerStats = playerObj.GetComponent<PlayerStats>();
            }
        }

        if (playerStats != null)
        {
            // Subscribe to the PlayerStats events
            playerStats.OnHealthChanged += UpdateHealthUI;
            playerStats.OnXPChanged += UpdateXPUI;
            playerStats.OnGoldChanged += UpdateGoldUI;
            playerStats.OnLevelUp += OpenLevelUpSelection;

            // Initialize UI values
            UpdateHealthUI(playerStats.currentHealth, playerStats.maxHealth);
            UpdateXPUI(playerStats.currentLevel, playerStats.currentXP, playerStats.xpToNextLevel);
            UpdateGoldUI(playerStats.goldCollected);
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events on destroy to avoid memory leaks
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthUI;
            playerStats.OnXPChanged -= UpdateXPUI;
            playerStats.OnGoldChanged -= UpdateGoldUI;
            playerStats.OnLevelUp -= OpenLevelUpSelection;
        }
    }

    private void Update()
    {
        // Smoothly slide the health and experience bars (using unscaledDeltaTime so animations complete during pause)
        if (smoothTransition)
        {
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Lerp(healthSlider.value, _targetHealthPct, Time.unscaledDeltaTime * smoothSpeed);
            }
            if (xpSlider != null)
            {
                xpSlider.value = Mathf.Lerp(xpSlider.value, _targetXPPct, Time.unscaledDeltaTime * smoothSpeed);
            }
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        float pct = maxHealth > 0f ? (currentHealth / maxHealth) : 0f;

        if (smoothTransition)
        {
            _targetHealthPct = pct;
        }
        else if (healthSlider != null)
        {
            healthSlider.value = pct;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Round(currentHealth)}/{Mathf.Round(maxHealth)}";
        }
    }

    private void UpdateXPUI(int level, float currentXP, float xpToNextLevel)
    {
        float pct = xpToNextLevel > 0f ? (currentXP / xpToNextLevel) : 0f;

        if (smoothTransition)
        {
            _targetXPPct = pct;
        }
        else if (xpSlider != null)
        {
            xpSlider.value = pct;
        }

        if (levelText != null)
        {
            levelText.text = $"Lvl {level}";
        }
    }

    private void UpdateGoldUI(int gold)
    {
        if (hudGoldText != null)
        {
            hudGoldText.text = $"Gold: {gold}";
        }
    }

    private void SetLevelUpUIActive(bool active)
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(active);
        }
        if (_statsPanelObj != null)
        {
            _statsPanelObj.SetActive(active);
        }
    }

    public void SetStatsPanelActive(bool active)
    {
        if (_statsPanelObj != null)
        {
            _statsPanelObj.SetActive(active);
            if (active)
            {
                UpdateStatsPanelText();
            }
        }
    }

    private void CreateStatsPanel()
    {
        if (levelUpPanel == null) return;

        // 1. Create panel GameObject
        GameObject statsPanel = new GameObject("StatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        statsPanel.transform.SetParent(levelUpPanel.transform.parent, false); // Sibling of levelUpPanel under Canvas
        _statsPanelObj = statsPanel;

        RectTransform panelRt = statsPanel.GetComponent<RectTransform>();
        // Position on the left side of the screen (slightly wider and taller to fit larger font: 24% width, 64% height)
        panelRt.anchorMin = new Vector2(0.02f, 0.18f);
        panelRt.anchorMax = new Vector2(0.26f, 0.82f);
        panelRt.pivot = new Vector2(0f, 0.5f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Styling panel image (dark charcoal semi-transparent overlay with a premium look)
        Image panelImage = statsPanel.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.14f, 0.92f); // Charcoal dark background

        // 2. Create Title Text
        GameObject titleObj = new GameObject("StatsTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(statsPanel.transform, false);

        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.88f);
        titleRt.anchorMax = new Vector2(1f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "CHARACTER STATS";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.78f, 0f, 1f); // Gold color title
        titleText.alignment = TextAlignmentOptions.Center;

        // 3. Create Content Text
        GameObject contentObj = new GameObject("StatsContentText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        contentObj.transform.SetParent(statsPanel.transform, false);

        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0.08f, 0.05f);
        contentRt.anchorMax = new Vector2(0.92f, 0.84f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        _statsContentText = contentObj.GetComponent<TextMeshProUGUI>();
        _statsContentText.fontSize = 20;
        _statsContentText.color = new Color(0.92f, 0.92f, 0.95f, 1f); // Off-white text
        _statsContentText.lineSpacing = 26f; // More spacing for larger font
        _statsContentText.alignment = TextAlignmentOptions.TopLeft;

        // Initial update
        UpdateStatsPanelText();
    }

    private void UpdateStatsPanelText()
    {
        if (playerStats == null || _statsContentText == null) return;

        float maxHp = playerStats.maxHealth;
        float armor = playerStats.armor;

        float speed = 0f;
        PlayerController pc = playerStats.GetComponent<PlayerController>();
        if (pc != null) speed = pc.moveSpeed;

        float damage = 0f;
        float cooldown = 0f;
        int count = 0;
        int pierce = 0;
        float range = 0f;
        float projSpeed = 0f;

        SwordThrower st = playerStats.GetComponent<SwordThrower>();
        if (st != null)
        {
            damage = st.swordDamage;
            cooldown = st.throwCooldown;
            count = st.swordCount;
            pierce = st.pierceCount;
            range = st.searchRange;
            projSpeed = st.projectileSpeed;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Max Health <pos=70%>{maxHp}");
        sb.AppendLine($"Armor <pos=70%>+{armor}");
        sb.AppendLine($"Move Speed <pos=70%>{speed:F1} m/s");
        sb.AppendLine($"Sword Damage <pos=70%>{damage}");
        sb.AppendLine($"Cooldown <pos=70%>{cooldown:F2}s");
        sb.AppendLine($"Sword Count <pos=70%>{count}");
        sb.AppendLine($"Pierce <pos=70%>{pierce}");
        sb.AppendLine($"Search Range <pos=70%>{range:F1}");
        sb.AppendLine($"Projectile Speed <pos=70%>{projSpeed:F1} m/s");

        _statsContentText.text = sb.ToString();
    }

    private void OpenLevelUpSelection()
    {
        if (levelUpPanel == null) return;

        // Refresh stats panel details
        UpdateStatsPanelText();

        // Ensure cursor is visible so player can select an upgrade
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pick 3 unique options from the pool using weighted random selection
        List<UpgradeOption> chosenOptions = PickRandomUpgrades(3);

        // Bind Option 1
        if (chosenOptions.Count >= 1 && optionButton1 != null)
        {
            optionTitle1.text = GetRarityColorTag(chosenOptions[0].rarity) + chosenOptions[0].title;
            optionDesc1.text = chosenOptions[0].description;
            UpgradeOption opt = chosenOptions[0];
            optionButton1.onClick.RemoveAllListeners();
            optionButton1.onClick.AddListener(() => SelectUpgrade(opt));
        }

        // Bind Option 2
        if (chosenOptions.Count >= 2 && optionButton2 != null)
        {
            optionTitle2.text = GetRarityColorTag(chosenOptions[1].rarity) + chosenOptions[1].title;
            optionDesc2.text = chosenOptions[1].description;
            UpgradeOption opt = chosenOptions[1];
            optionButton2.onClick.RemoveAllListeners();
            optionButton2.onClick.AddListener(() => SelectUpgrade(opt));
        }

        // Bind Option 3
        if (chosenOptions.Count >= 3 && optionButton3 != null)
        {
            optionTitle3.text = GetRarityColorTag(chosenOptions[2].rarity) + chosenOptions[2].title;
            optionDesc3.text = chosenOptions[2].description;
            UpgradeOption opt = chosenOptions[2];
            optionButton3.onClick.RemoveAllListeners();
            optionButton3.onClick.AddListener(() => SelectUpgrade(opt));
        }

        // Display the panels
        SetLevelUpUIActive(true);
    }

    // Weighted random selection algorithm to pick 'count' unique options
    private List<UpgradeOption> PickRandomUpgrades(int count)
    {
        List<UpgradeOption> chosen = new List<UpgradeOption>();
        List<UpgradeOption> pool = new List<UpgradeOption>(_availableUpgrades);

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            int totalWeight = 0;
            foreach (var opt in pool)
            {
                totalWeight += opt.weight;
            }

            if (totalWeight <= 0) break;

            // Pick a weighted random value
            int randVal = Random.Range(0, totalWeight);
            int currentSum = 0;
            UpgradeOption selectedOption = pool[0];

            for (int j = 0; j < pool.Count; j++)
            {
                currentSum += pool[j].weight;
                if (randVal <= currentSum)
                {
                    selectedOption = pool[j];
                    break;
                }
            }

            chosen.Add(selectedOption);
            pool.Remove(selectedOption); // Remove from pool to prevent duplicates
        }

        return chosen;
    }

    // Returns a colored Rich Text prefix based on upgrade rarity
    private string GetRarityColorTag(string rarity)
    {
        switch (rarity)
        {
            case "Rare":
                return "<color=#00bcd4>[Rare]</color> "; // Cyan/Blue
            case "Epic":
                return "<color=#e91e63>[Epic]</color> "; // Magenta/Pink/Gold
            default:
                return "<color=#8bc34a>[Common]</color> "; // Light Green
        }
    }

    private void SelectUpgrade(UpgradeOption option)
    {
        if (playerStats == null) return;

        // Apply the chosen upgrade to the player
        switch (option.type)
        {
            case UpgradeType.MoveSpeed:
                PlayerController controller = playerStats.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.moveSpeed += 1.0f; // Increase speed by 1 unit
                }
                break;

            case UpgradeType.MaxHealth:
                playerStats.maxHealth += 20f;
                playerStats.Heal(20f); // Increase max health and heal the amount
                break;

            case UpgradeType.AttackSpeed:
                SwordThrower thrower = playerStats.GetComponent<SwordThrower>();
                if (thrower != null)
                {
                    thrower.throwCooldown = Mathf.Max(0.2f, thrower.throwCooldown - 0.2f); // faster throwing cooldown
                }
                break;

            case UpgradeType.SwordDamage:
                SwordThrower damageThrower = playerStats.GetComponent<SwordThrower>();
                if (damageThrower != null)
                {
                    damageThrower.swordDamage += 8f; // Increase sword damage by 8 units
                }
                break;

            case UpgradeType.FirstAid:
                playerStats.Heal(playerStats.maxHealth); // Fully heal
                break;

            case UpgradeType.SwordCount:
                SwordThrower countThrower = playerStats.GetComponent<SwordThrower>();
                if (countThrower != null)
                {
                    countThrower.swordCount += 1; // Throw an extra sword in fan shape
                }
                break;

            case UpgradeType.PierceCount:
                SwordThrower pierceThrower = playerStats.GetComponent<SwordThrower>();
                if (pierceThrower != null)
                {
                    pierceThrower.pierceCount += 1; // Pierce one more enemy
                }
                break;

            case UpgradeType.Defense:
                playerStats.armor += 2f; // Reduce damage taken by 2 flat points
                break;

            case UpgradeType.SearchRange:
                SwordThrower rangeThrower = playerStats.GetComponent<SwordThrower>();
                if (rangeThrower != null)
                {
                    rangeThrower.searchRange += 4f; // Increase weapon range by 4 units
                }
                break;

            case UpgradeType.ProjectileSpeed:
                SwordThrower speedThrower = playerStats.GetComponent<SwordThrower>();
                if (speedThrower != null)
                {
                    speedThrower.projectileSpeed += 5f; // Projectile speed +5
                }
                break;
        }

        // Hide panels
        SetLevelUpUIActive(false);

        // Resume game time
        Time.timeScale = 1f;

        Debug.Log($"Applied Upgrade: {option.title}");
    }
}
