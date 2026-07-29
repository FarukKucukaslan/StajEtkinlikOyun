using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        ProjectileSpeed,
        XPMagnet,
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
    [Tooltip(
        "Reference to the PlayerStats script. If left empty, it will auto-detect from the GameObject tagged 'Player'."
    )]
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

    [Tooltip("Free rerolls the player gets each time the level-up screen opens.")]
    public int rerollsPerLevelUp = 2;

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
    private float _lastHealth = -1f; // previous health, to detect when the player takes damage

    // Death / results screen (built once on first death, then reused)
    private GameObject _deathScreenObj;
    private TextMeshProUGUI _deathStatsText;

    // Level-up extras (all built once in StyleLevelUpCards, then reused)
    private GameObject _levelUpDim; // full-screen dark overlay behind the panel
    private Button _rerollButton;
    private TextMeshProUGUI _rerollLabel;
    private int _rerollsLeft;

    // Numbers in perk text (e.g. "+15%", "-0.2s", "8") get highlighted gold.
    private static readonly Regex _numberRegex = new Regex(
        @"[+\-]?\d+(?:\.\d+)?(?:%|s)?",
        RegexOptions.Compiled
    );

    private const float XPPickupRangeMultiplier = 1.3f;

    // Available upgrades pool with weights (Total: 11 options)
    private readonly List<UpgradeOption> _availableUpgrades = new List<UpgradeOption>()
    {
        // COMMON (Weight: 80)
        new UpgradeOption
        {
            type = UpgradeType.MoveSpeed,
            title = "Speed Boost",
            description = "Increase movement speed by 15% (+1.0 speed).",
            weight = 80,
            rarity = "Common",
        },
        new UpgradeOption
        {
            type = UpgradeType.MaxHealth,
            title = "Vitality",
            description = "Increase maximum health by 20 and heal fully.",
            weight = 80,
            rarity = "Common",
        },
        new UpgradeOption
        {
            type = UpgradeType.AttackSpeed,
            title = "Fast Hands",
            description = "Throw swords 15% faster (-0.2s cooldown).",
            weight = 80,
            rarity = "Common",
        },
        new UpgradeOption
        {
            type = UpgradeType.SearchRange,
            title = "Eagle Eye",
            description = "Increase sword target search range by 25% (+4.0 range).",
            weight = 80,
            rarity = "Common",
        },
        new UpgradeOption
        {
            type = UpgradeType.ProjectileSpeed,
            title = "Swift Blades",
            description = "Swords travel 30% faster (+5.0 projectile speed).",
            weight = 80,
            rarity = "Common",
        },
        new UpgradeOption
        {
            type = UpgradeType.XPMagnet,
            title = "Crystal Magnet",
            description = "Increase XP crystal pickup range by 30%.",
            weight = 80,
            rarity = "Common",
        },
        // RARE (Weight: 30)
        new UpgradeOption
        {
            type = UpgradeType.SwordDamage,
            title = "Sharp Blade",
            description = "Increase sword damage by 8 points.",
            weight = 30,
            rarity = "Rare",
        },
        new UpgradeOption
        {
            type = UpgradeType.FirstAid,
            title = "First Aid",
            description = "Heal to full health instantly.",
            weight = 30,
            rarity = "Rare",
        },
        new UpgradeOption
        {
            type = UpgradeType.Defense,
            title = "Heavy Armor",
            description = "Reduce incoming damage from all enemies by 2 points.",
            weight = 30,
            rarity = "Rare",
        },
        // EPIC (Weight: 10)
        new UpgradeOption
        {
            type = UpgradeType.SwordCount,
            title = "Multi-Throw",
            description = "Throw one additional sword at nearest enemies.",
            weight = 10,
            rarity = "Epic",
        },
        new UpgradeOption
        {
            type = UpgradeType.PierceCount,
            title = "Piercing Edge",
            description = "Swords pass through one more enemy.",
            weight = 10,
            rarity = "Epic",
        },
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

        // Apply the visual theme to the level-up cards once.
        StyleLevelUpCards();

        // Round + recolor the health and XP bars.
        StyleHealthBar();
        StyleXPBar();

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
                healthSlider.value = Mathf.Lerp(
                    healthSlider.value,
                    _targetHealthPct,
                    Time.unscaledDeltaTime * smoothSpeed
                );
            }
            if (xpSlider != null)
            {
                xpSlider.value = Mathf.Lerp(
                    xpSlider.value,
                    _targetXPPct,
                    Time.unscaledDeltaTime * smoothSpeed
                );
            }
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        float pct = maxHealth > 0f ? (currentHealth / maxHealth) : 0f;

        // Health dropped => the player got hit: flash the screen red + a small shake.
        // (_lastHealth starts at -1 so the first call just sets the baseline.)
        if (_lastHealth >= 0f && currentHealth < _lastHealth)
        {
            JuiceManager.PlayerHitFlash(1f);
            JuiceManager.Shake(0.2f);
        }
        _lastHealth = currentHealth;

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

    // --- HUD bar sizing/position knobs (tweak these to move/resize the bars) ---
    private const int BarSegments = 10; // notch count on both bars

    // Health bar (Megabonk-style): compact red bar top-left, value text centered on it,
    // sitting just under the XP strip.
    private void StyleHealthBar()
    {
        if (healthSlider != null)
        {
            RectTransform rt = healthSlider.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(360f, 80f); // big & bulky
            rt.anchoredPosition = new Vector2(24f, -100f); // top-left, clears the 88px XP strip with a gap
        }

        StyleSliderChrome(healthSlider, new Color(0.85f, 0.16f, 0.14f, 1f), false); // red

        // "110/110" centered on the bar, sized to fill it.
        if (healthText != null && healthSlider != null)
        {
            RectTransform brt = healthSlider.GetComponent<RectTransform>();
            RectTransform trt = healthText.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(0f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.sizeDelta = brt.sizeDelta;
            trt.anchoredPosition = brt.anchoredPosition;
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.fontStyle = FontStyles.Bold;
            healthText.color = Color.white;
            healthText.enableAutoSizing = true;
            healthText.fontSizeMin = 18f;
            healthText.fontSizeMax = 34f;
            healthText.transform.SetAsLastSibling(); // draw over the bar
        }
    }

    // XP bar (Megabonk-style): thin cyan strip full-width along the very top edge.
    private void StyleXPBar()
    {
        if (xpSlider != null)
        {
            RectTransform rt = xpSlider.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 84f); // full width, tall, covers the top
            rt.anchoredPosition = new Vector2(0f, 20f); // bleed 20px above the top edge so no game world shows in the gap

            // Unity's default Slider insets both the track (Background) and the
            // coloured fill (Fill Area) to the middle 50% of the bar's height
            // (anchors y 0.25..0.75) plus side padding. Stretch every part to the
            // full bar so the track and fill fill it edge-to-edge and reach the
            // very top. Horizontal size of the fill is still driven by the slider
            // value, so we only override the vertical anchors/offsets on it.
            StretchFull(
                xpSlider.fillRect != null ? xpSlider.fillRect.parent as RectTransform : null
            );
            StretchFull(GetSliderBackground(xpSlider));
        }

        StyleSliderChrome(xpSlider, new Color(0.07f, 0.28f, 0.62f, 1f), false, rounded: false); // dark blue, square (flush to top edge)
    }

    // Anchors a RectTransform to fill its parent completely (no padding).
    private void StretchFull(RectTransform rt)
    {
        if (rt == null)
            return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // The slider's track image lives on the "Background" child (Unity's default
    // slider layout). Returns its RectTransform, or null if it isn't there.
    private RectTransform GetSliderBackground(Slider s)
    {
        if (s == null)
            return null;
        Transform bg = s.transform.Find("Background");
        return bg as RectTransform;
    }

    // Shared bar chrome: rounds the bar, colors the fill, and makes the empty
    // track a clean dark. No outline frame (it read as an ugly border). Returns
    // the fill Image so the caller can recolor it.
    private Image StyleSliderChrome(Slider s, Color fillColor, bool segmented, bool rounded = true)
    {
        if (s == null)
            return null;

        Color track = new Color(0.10f, 0.13f, 0.22f, 1f); // dark blue-grey: empty bar still reads as a bar, not a black slab

        Image fill = s.fillRect != null ? s.fillRect.GetComponent<Image>() : null;

        // Color every image on the bar: the fill gets its color, everything else
        // (background / track) gets the dark track color. This also wipes any stray
        // color the slider had in the scene. Rounded bars use the chunky corners;
        // flat bars (rounded == false) stay square so a bar flush against a screen
        // edge doesn't show the game world through its rounded corners.
        foreach (Image img in s.GetComponentsInChildren<Image>(true))
        {
            Color c = img == fill ? fillColor : track;
            if (rounded)
                UIStyle.ApplyPanel(img, c);
            else
                UIStyle.ApplyFlat(img, c);
        }

        if (segmented)
            AddSegments(s.GetComponent<RectTransform>(), BarSegments, new Color(0f, 0f, 0f, 0.45f));
        return fill;
    }

    // Adds (once) evenly-spaced vertical divider lines over a bar, giving it the
    // chunky segmented look. The lines are fixed; the fill slides underneath them.
    private void AddSegments(RectTransform bar, int count, Color color)
    {
        if (bar == null || count < 2)
            return;
        if (bar.Find("__Segments") != null)
            return; // built once

        GameObject container = new GameObject("__Segments", typeof(RectTransform));
        RectTransform crt = container.GetComponent<RectTransform>();
        crt.SetParent(bar, false);
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        for (int i = 1; i < count; i++)
        {
            float f = (float)i / count;
            GameObject line = new GameObject(
                "seg" + i,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            RectTransform lrt = line.GetComponent<RectTransform>();
            lrt.SetParent(crt, false);
            lrt.anchorMin = new Vector2(f, 0.12f);
            lrt.anchorMax = new Vector2(f, 0.88f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(3f, 0f);
            lrt.anchoredPosition = Vector2.zero;

            Image img = line.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
        container.transform.SetAsLastSibling();
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
        if (_levelUpDim != null)
            _levelUpDim.SetActive(active);
        if (levelUpPanel != null)
            levelUpPanel.SetActive(active);
        if (_statsPanelObj != null)
            _statsPanelObj.SetActive(active);

        // When showing, stack draw order so the dim sits over the HUD but the
        // panel and stats sit over the dim (last sibling = drawn on top).
        if (active)
        {
            if (_levelUpDim != null)
                _levelUpDim.transform.SetAsLastSibling();
            if (_statsPanelObj != null)
                _statsPanelObj.transform.SetAsLastSibling();
            if (levelUpPanel != null)
                levelUpPanel.transform.SetAsLastSibling();
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
        if (levelUpPanel == null)
            return;

        // 1. Create panel GameObject
        GameObject statsPanel = new GameObject(
            "StatsPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
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
        GameObject titleObj = new GameObject(
            "StatsTitleText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
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
        GameObject contentObj = new GameObject(
            "StatsContentText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
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
        if (playerStats == null || _statsContentText == null)
            return;

        float maxHp = playerStats.maxHealth;
        float armor = playerStats.armor;

        float speed = 0f;
        PlayerController pc = playerStats.GetComponent<PlayerController>();
        if (pc != null)
            speed = pc.moveSpeed;

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
        sb.AppendLine($"XP Pickup Range <pos=70%>{playerStats.XPPickupRange:F1}");
        sb.AppendLine($"Projectile Speed <pos=70%>{projSpeed:F1} m/s");

        _statsContentText.text = sb.ToString();
    }

    private void OpenLevelUpSelection()
    {
        if (levelUpPanel == null)
            return;

        // Refresh stats panel details
        UpdateStatsPanelText();

        // Ensure cursor is visible so player can select an upgrade
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Fresh set of rerolls for this level-up, then fill the three cards.
        _rerollsLeft = rerollsPerLevelUp;
        PopulateOptions();
        UpdateRerollLabel();

        // Display the panels
        SetLevelUpUIActive(true);
    }

    // Picks 3 unique weighted-random options and binds them to the three cards.
    // Called on open and on every reroll.
    private void PopulateOptions()
    {
        List<UpgradeOption> chosenOptions = PickRandomUpgrades(3);

        if (chosenOptions.Count >= 1 && optionButton1 != null)
            Bind(optionButton1, optionTitle1, optionDesc1, chosenOptions[0]);
        if (chosenOptions.Count >= 2 && optionButton2 != null)
            Bind(optionButton2, optionTitle2, optionDesc2, chosenOptions[1]);
        if (chosenOptions.Count >= 3 && optionButton3 != null)
            Bind(optionButton3, optionTitle3, optionDesc3, chosenOptions[2]);
    }

    // Reroll button: swaps the three options for a new random set, one use per press.
    private void DoReroll()
    {
        if (_rerollsLeft <= 0)
            return;
        _rerollsLeft--;
        PopulateOptions();
        UpdateRerollLabel();
    }

    // Updates the reroll button's label ("REROLL (n)") and disables it at zero.
    private void UpdateRerollLabel()
    {
        if (_rerollLabel != null)
            _rerollLabel.text = $"REROLL ({_rerollsLeft})";
        if (_rerollButton != null)
            _rerollButton.interactable = _rerollsLeft > 0;
    }

    // Fills one upgrade card with an option and color-codes it by rarity.
    // Fills one upgrade card with an option and color-codes it by rarity.
    // Fills one upgrade card with an option: color-codes by rarity and shows its icon.
    private void Bind(Button btn, TextMeshProUGUI title, TextMeshProUGUI desc, UpgradeOption opt)
    {
        Color rarity = UITheme.Rarity(opt.rarity);

        title.text = opt.title;
        title.color = rarity;

        // Description with numbers highlighted, plus a "current -> next" impact line
        // (skipped in the main menu where there is no player to read stats from).
        string body = HighlightNumbers(opt.description);
        string preview = GetUpgradePreview(opt.type);
        if (!string.IsNullOrEmpty(preview))
            body += "\n\n" + HighlightNumbers(preview);
        desc.text = body;

        RectTransform rt = btn.GetComponent<RectTransform>();
        UIStyle.SetBorder(rt, rarity);

        // Rarity badge at the top of the card (COMMON / RARE / EPIC in its color).
        Transform badgeT = rt.Find("__Rarity");
        if (badgeT != null)
        {
            TextMeshProUGUI badge = badgeT.GetComponent<TextMeshProUGUI>();
            badge.text = opt.rarity;
            badge.color = rarity;
        }

        // Optional icon: none in the project yet. If art is later added to
        // Resources/Icons it shows in the card's icon slot; nothing breaks without it.
        Sprite iconSprite = IconLibrary.Get(opt.type.ToString());
        Image icon = UIStyle.GetOrCreateChildImage(rt, "Icon");
        bool hasIcon = iconSprite != null;
        icon.gameObject.SetActive(hasIcon);
        if (hasIcon)
        {
            icon.sprite = iconSprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        // Card glows toward the rarity color on hover.
        ColorBlock cb = btn.colors;
        cb.normalColor = UITheme.Card;
        cb.highlightedColor = Color.Lerp(UITheme.Card, rarity, 0.45f);
        cb.pressedColor = Color.Lerp(UITheme.Card, rarity, 0.65f);
        cb.selectedColor = UITheme.Card;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        // Rebind the click to apply this specific upgrade.
        btn.onClick.RemoveAllListeners();
        UpgradeOption captured = opt;
        btn.onClick.AddListener(() => SelectUpgrade(captured));
    }

    // Weighted random selection algorithm to pick 'count' unique options
    private List<UpgradeOption> PickRandomUpgrades(int count)
    {
        List<UpgradeOption> chosen = new List<UpgradeOption>();
        List<UpgradeOption> pool = new List<UpgradeOption>(_availableUpgrades);

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0)
                break;

            int totalWeight = 0;
            foreach (var opt in pool)
            {
                totalWeight += opt.weight;
            }

            if (totalWeight <= 0)
                break;

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

    // Wraps every number in the text (e.g. "+15%", "-0.2s", "8") in bold gold so
    // the actual effect stands out from the surrounding words.
    private string HighlightNumbers(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        string hex = ColorUtility.ToHtmlStringRGB(UITheme.Gold);
        return _numberRegex.Replace(text, m => $"<b><color=#{hex}>{m.Value}</color></b>");
    }

    // Builds a short "current -> next" impact string for one upgrade, read from the
    // player's live stats. Returns "" when there is no player (e.g. main menu).
    private string GetUpgradePreview(UpgradeType type)
    {
        if (playerStats == null)
            return "";
        PlayerController pc = playerStats.GetComponent<PlayerController>();
        SwordThrower st = playerStats.GetComponent<SwordThrower>();

        switch (type)
        {
            case UpgradeType.MoveSpeed:
                if (pc == null)
                    return "";
                return $"{pc.moveSpeed:F1} -> {pc.moveSpeed + 1f:F1} spd";
            case UpgradeType.MaxHealth:
                return $"{playerStats.maxHealth:F0} -> {playerStats.maxHealth + 20f:F0} HP";
            case UpgradeType.AttackSpeed:
                if (st == null)
                    return "";
                return $"{st.throwCooldown:F2}s -> {Mathf.Max(0.2f, st.throwCooldown - 0.2f):F2}s";
            case UpgradeType.SwordDamage:
                if (st == null)
                    return "";
                return $"{st.swordDamage:F0} -> {st.swordDamage + 8f:F0} dmg";
            case UpgradeType.FirstAid:
                return "Full heal";
            case UpgradeType.SwordCount:
                if (st == null)
                    return "";
                return $"{st.swordCount} -> {st.swordCount + 1} swords";
            case UpgradeType.PierceCount:
                if (st == null)
                    return "";
                return $"{st.pierceCount} -> {st.pierceCount + 1} pierce";
            case UpgradeType.Defense:
                return $"{playerStats.armor:F0} -> {playerStats.armor + 2f:F0} armor";
            case UpgradeType.SearchRange:
                if (st == null)
                    return "";
                return $"{st.searchRange:F0} -> {st.searchRange + 4f:F0} range";
            case UpgradeType.ProjectileSpeed:
                if (st == null)
                {
                    return "";
                }

                return $"{st.projectileSpeed:F0} -> " + $"{st.projectileSpeed + 5f:F0} spd";

            case UpgradeType.XPMagnet:
                float nextPickupRange = playerStats.GetNextXPPickupRange(XPPickupRangeMultiplier);

                return $"{playerStats.XPPickupRange:F1} -> " + $"{nextPickupRange:F1} range";
        }
        return "";
    }

    private void SelectUpgrade(UpgradeOption option)
    {
        if (playerStats == null)
            return;

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
                    speedThrower.projectileSpeed += 5f;
                }
                break;

            case UpgradeType.XPMagnet:
                playerStats.IncreaseXPPickupRange(XPPickupRangeMultiplier);
                break;
        }

        // Hide panels
        SetLevelUpUIActive(false);

        // Resume game time
        Time.timeScale = 1f;

        Debug.Log($"Applied Upgrade: {option.title}");
    }

    // Shows the end-of-run results screen. Builds it the first time, then reuses it.
    public void ShowDeathScreen(int goldThisRun, int totalGold, float survivalSeconds)
    {
        if (_deathScreenObj == null)
            CreateDeathScreen();
        if (_deathScreenObj == null)
            return;

        int minutes = (int)(survivalSeconds / 60f);
        int seconds = (int)(survivalSeconds % 60f);

        if (_deathStatsText != null)
        {
            _deathStatsText.text =
                $"Time Survived: {minutes:00}:{seconds:00}\n"
                + $"Gold This Run: {goldThisRun}\n"
                + $"Total Gold: {totalGold}";
        }

        _deathScreenObj.SetActive(true);
        _deathScreenObj.transform.SetAsLastSibling(); // draw on top of the rest of the UI
    }

    // Hides the results screen (called by GameManager when returning to the main menu).
    public void HideDeathScreen()
    {
        if (_deathScreenObj != null)
        {
            _deathScreenObj.SetActive(false);
        }
    }

    // Builds the results screen UI in code so no manual Inspector wiring is needed.
    private void CreateDeathScreen()
    {
        // Full-screen dim overlay (also blocks clicks on the game behind it)
        GameObject overlay = new GameObject(
            "DeathScreen",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        overlay.transform.SetParent(transform, false);
        RectTransform overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        _deathScreenObj = overlay;

        // Centered results panel
        GameObject panel = new GameObject(
            "DeathPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        panel.transform.SetParent(overlay.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(640f, 520f);
        panelRt.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.98f);

        // Title: \"YOU DIED\"
        GameObject titleObj = new GameObject(
            "DeathTitle",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.72f);
        titleRt.anchorMax = new Vector2(1f, 0.95f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
        title.text = "YOU DIED";
        title.fontSize = 64;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.86f, 0.15f, 0.15f, 1f);
        title.alignment = TextAlignmentOptions.Center;

        // Run stats text
        GameObject statsObj = new GameObject(
            "DeathStats",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        statsObj.transform.SetParent(panel.transform, false);
        RectTransform statsRt = statsObj.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0.1f, 0.3f);
        statsRt.anchorMax = new Vector2(0.9f, 0.7f);
        statsRt.offsetMin = Vector2.zero;
        statsRt.offsetMax = Vector2.zero;
        _deathStatsText = statsObj.GetComponent<TextMeshProUGUI>();
        _deathStatsText.fontSize = 30;
        _deathStatsText.color = new Color(0.92f, 0.92f, 0.95f, 1f);
        _deathStatsText.alignment = TextAlignmentOptions.Center;
        _deathStatsText.lineSpacing = 24f;

        // Continue button -> back to the main menu
        GameObject btnObj = new GameObject(
            "ContinueButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        btnObj.transform.SetParent(panel.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.25f, 0.08f);
        btnRt.anchorMax = new Vector2(0.75f, 0.22f);
        btnRt.offsetMin = Vector2.zero;
        btnRt.offsetMax = Vector2.zero;
        btnObj.GetComponent<Image>().color = new Color(0.86f, 0.15f, 0.15f, 1f);
        btnObj
            .GetComponent<Button>()
            .onClick.AddListener(() =>
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMainMenu();
                }
            });

        // Continue button label
        GameObject btnTextObj = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
        btnTextRt.anchorMin = Vector2.zero;
        btnTextRt.anchorMax = Vector2.one;
        btnTextRt.offsetMin = Vector2.zero;
        btnTextRt.offsetMax = Vector2.zero;
        TextMeshProUGUI btnLabel = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnLabel.text = "CONTINUE";
        btnLabel.fontSize = 30;
        btnLabel.fontStyle = FontStyles.Bold;
        btnLabel.color = Color.white;
        btnLabel.alignment = TextAlignmentOptions.Center;

        overlay.SetActive(false);
    }

    // One-time styling of the level-up card container and text.
    // One-time styling + layout of the level-up screen.
    private void StyleLevelUpCards()
    {
        if (levelUpPanel == null)
            return;

        // Compact, centered panel (was full-height and cramped against the timer).
        RectTransform panelRt = levelUpPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(820f, 660f);
            panelRt.anchoredPosition = new Vector2(0f, -30f); // nudge below the timer
        }
        Image panelBg = levelUpPanel.GetComponent<Image>();
        if (panelBg != null)
            UIStyle.ApplyPanel(panelBg, UITheme.PanelDark);

        CreateBackgroundDim();
        CreateLevelUpHeader();

        // Three cards with gaps, leaving room for the header on top.
        LayoutCard(optionButton1, optionTitle1, optionDesc1, 0);
        LayoutCard(optionButton2, optionTitle2, optionDesc2, 1);
        LayoutCard(optionButton3, optionTitle3, optionDesc3, 2);

        CreateRerollButton();
    }

    // Full-screen dark overlay drawn behind the level-up panel, so the bright
    // gameplay behind it stops competing for attention. Toggled in SetLevelUpUIActive.
    private void CreateBackgroundDim()
    {
        if (levelUpPanel == null)
            return;
        Transform parent = levelUpPanel.transform.parent;
        if (parent == null)
            return;

        Transform existing = parent.Find("__LevelUpDim");
        if (existing != null)
        {
            _levelUpDim = existing.gameObject;
            return;
        }

        GameObject dim = new GameObject(
            "__LevelUpDim",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        RectTransform rt = dim.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f); // raycast on = blocks clicks behind
        _levelUpDim = dim;
        dim.SetActive(false);
    }

    // "REROLL (n)" button at the bottom of the panel. Built once; DoReroll swaps the options.
    private void CreateRerollButton()
    {
        if (levelUpPanel == null)
            return;
        if (levelUpPanel.transform.Find("__Reroll") != null)
        {
            _rerollButton = levelUpPanel.transform.Find("__Reroll").GetComponent<Button>();
            _rerollLabel = _rerollButton.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        GameObject go = new GameObject(
            "__Reroll",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(levelUpPanel.transform, false);
        rt.anchorMin = new Vector2(0.35f, 0.03f);
        rt.anchorMax = new Vector2(0.65f, 0.12f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        UIStyle.ApplyPanel(img, Color.white); // rounded; the ColorBlock below tints it
        _rerollButton = go.GetComponent<Button>();
        _rerollButton.transition = Selectable.Transition.ColorTint;
        _rerollButton.targetGraphic = img;

        ColorBlock cb = _rerollButton.colors;
        cb.normalColor = UITheme.Card;
        cb.highlightedColor = UITheme.CardHover;
        cb.pressedColor = UITheme.Gold;
        cb.disabledColor = new Color(0.18f, 0.18f, 0.20f, 1f);
        cb.fadeDuration = 0.1f;
        _rerollButton.colors = cb;
        _rerollButton.onClick.AddListener(DoReroll);

        GameObject textObj = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.SetParent(rt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        _rerollLabel = textObj.GetComponent<TextMeshProUGUI>();
        _rerollLabel.text = "REROLL";
        _rerollLabel.color = UITheme.TextLight;
        _rerollLabel.fontStyle = FontStyles.Bold;
        _rerollLabel.alignment = TextAlignmentOptions.Center;
        _rerollLabel.enableAutoSizing = true;
        _rerollLabel.fontSizeMin = 14f;
        _rerollLabel.fontSizeMax = 24f;
    }

    // Configures a card text field: wrapping, auto-size range, padding, alignment.

    // Positions and styles one upgrade card. Three tall cards side by side
    // (index 0 = left, 2 = right): rarity badge on top, title, then description.
    // Text-first layout (no icons in the project yet) so the perks stay readable.
    private void LayoutCard(Button btn, TextMeshProUGUI title, TextMeshProUGUI desc, int index)
    {
        if (btn == null)
            return;

        // Three equal columns spanning 0.04..0.96 with a gap between them.
        const float gap = 0.03f;
        float width = (0.92f - gap * 2f) / 3f;
        float left = 0.04f + index * (width + gap);

        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(left, 0.15f); // header above (~0.82), reroll button below
        rt.anchorMax = new Vector2(left + width, 0.82f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Rounded card body (the ColorBlock in Bind tints it; SetBorder recolors per rarity).
        Image img = btn.GetComponent<Image>();
        UIStyle.ApplyPanel(img, Color.white);
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = img;
        UIStyle.SetBorder(rt, UITheme.Card);

        // Rarity badge at the very top (text + color set per-option in Bind).
        TextMeshProUGUI badge = GetOrCreateCardText(rt, "__Rarity");
        RectTransform brt = badge.rectTransform;
        brt.anchorMin = new Vector2(0.08f, 0.85f);
        brt.anchorMax = new Vector2(0.92f, 0.96f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        badge.alignment = TextAlignmentOptions.Center;
        badge.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        badge.characterSpacing = 6f;
        badge.enableAutoSizing = true;
        badge.fontSizeMin = 12f;
        badge.fontSizeMax = 20f;

        // Title under the badge: big, bold, rarity-colored (color set in Bind).
        if (title != null)
        {
            RectTransform trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.06f, 0.60f);
            trt.anchorMax = new Vector2(0.94f, 0.83f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            title.enableWordWrapping = true;
            title.enableAutoSizing = true;
            title.fontSizeMin = 20f;
            title.fontSizeMax = 34f;
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 0f;
        }

        // Description fills the card body: bright enough to read, wrapped, centered.
        if (desc != null)
        {
            RectTransform drt = desc.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.10f, 0.08f);
            drt.anchorMax = new Vector2(0.90f, 0.56f);
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;
            desc.enableWordWrapping = true;
            desc.enableAutoSizing = true;
            desc.fontSizeMin = 16f;
            desc.fontSizeMax = 26f;
            desc.alignment = TextAlignmentOptions.Center;
            desc.color = UITheme.TextLight;
        }
    }

    // Gets or creates a named TextMeshPro child under a card (used for the rarity badge).
    private TextMeshProUGUI GetOrCreateCardText(RectTransform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing.GetComponent<TextMeshProUGUI>();

        GameObject go = new GameObject(
            childName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        go.GetComponent<RectTransform>().SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        return t;
    }

    // Creates the "LEVEL UP" header at the top of the panel (once).
    private void CreateLevelUpHeader()
    {
        if (levelUpPanel == null)
            return;
        if (levelUpPanel.transform.Find("__Header") != null)
            return;

        GameObject headerObj = new GameObject(
            "__Header",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        RectTransform rt = headerObj.GetComponent<RectTransform>();
        rt.SetParent(levelUpPanel.transform, false);
        rt.anchorMin = new Vector2(0.05f, 0.86f);
        rt.anchorMax = new Vector2(0.95f, 0.99f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI t = headerObj.GetComponent<TextMeshProUGUI>();
        t.text = "LEVEL UP";
        t.color = UITheme.Gold;
        t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        t.characterSpacing = 10f;
        t.enableAutoSizing = true;
        t.fontSizeMin = 24f;
        t.fontSizeMax = 44f;
        t.alignment = TextAlignmentOptions.Center;
    }
}
