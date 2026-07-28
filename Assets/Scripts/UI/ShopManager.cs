using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Between-run upgrade shop, laid out as a Vampire-Survivors-style "market grid":
/// nine upgrade cards in a 3x3 grid, each showing name / current->next value /
/// level / cost. Clicking an affordable card buys it on the spot (no separate
/// detail panel).
///
/// The whole layout is built in code at runtime (buttons are reparented into the
/// grid), so the scene file is left as-is - re-skinning happens here + in
/// <see cref="UITheme"/> / <see cref="UIStyle"/>, not in the scene.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Shop UI Panel")]
    public GameObject shopPanel;
    public TextMeshProUGUI shopGoldText;

    [Header("Upgrade Buttons (become the 3x3 grid, in this order)")]
    public Button btnMaxHealth;
    public Button btnMoveSpeed;
    public Button btnAttackSpeed;
    public Button btnSwordDamage;
    public Button btnSwordCount;
    public Button btnPierceCount;
    public Button btnDefense;
    public Button btnSearchRange;
    public Button btnProjSpeed;

    [Header("Old detail panel (hidden in the grid layout, kept for scene refs)")]
    public TextMeshProUGUI detailTitleText;
    public TextMeshProUGUI detailStatsText;
    public TextMeshProUGUI detailCostText;
    public Button buyButton;

    private const int MaxLevel = 5;

    // (PlayerPrefs key, its card button) in grid order. Built once in Start.
    private readonly List<KeyValuePair<string, Button>> _cards = new List<KeyValuePair<string, Button>>();

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);

        BuildCardList();
        StyleShop();        // reparents the buttons into a grid, styles them, wires click-to-buy
        RefreshShopCards(); // fills in values / levels / costs
    }

    // Pairs each PlayerPrefs key with its button, in the order they appear in the grid.
    private void BuildCardList()
    {
        _cards.Clear();
        AddCard("Shop_MaxHealth", btnMaxHealth);
        AddCard("Shop_MoveSpeed", btnMoveSpeed);
        AddCard("Shop_AttackSpeed", btnAttackSpeed);
        AddCard("Shop_SwordDamage", btnSwordDamage);
        AddCard("Shop_SwordCount", btnSwordCount);
        AddCard("Shop_PierceCount", btnPierceCount);
        AddCard("Shop_Defense", btnDefense);
        AddCard("Shop_SearchRange", btnSearchRange);
        AddCard("Shop_ProjectileSpeed", btnProjSpeed);
    }

    private void AddCard(string key, Button btn)
    {
        if (btn != null) _cards.Add(new KeyValuePair<string, Button>(key, btn));
    }

    public void OpenShop()
    {
        if (shopPanel != null) shopPanel.SetActive(true);

        // Hide the character stats panel while shopping so the two don't overlap.
        if (UIManager.Instance != null) UIManager.Instance.SetStatsPanelActive(false);

        RefreshShopCards();
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);

        // Make the player's components reload their freshly-purchased stats from PlayerPrefs.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        if (UIManager.Instance != null) UIManager.Instance.SetStatsPanelActive(true);
    }

    private void UpdateGoldText()
    {
        if (shopGoldText != null)
        {
            int currentGold = PlayerPrefs.GetInt("PlayerGold", 0);
            shopGoldText.text = $"Gold: {currentGold}";
        }
    }

    // Buys one upgrade if the player can afford it and it isn't maxed, then refreshes.
    private void TryBuy(string key)
    {
        int level = PlayerPrefs.GetInt(key, 0);
        if (level >= MaxLevel) return;

        int cost = CalculateCost(key, level);
        int gold = PlayerPrefs.GetInt("PlayerGold", 0);
        if (gold < cost) return; // can't afford - card is already dimmed/disabled

        PlayerPrefs.SetInt("PlayerGold", gold - cost);
        PlayerPrefs.SetInt(key, level + 1);
        PlayerPrefs.Save();

        Debug.Log($"Purchased {key}. New level: {level + 1}");
        RefreshShopCards();
    }

    // ---------------------------------------------------------------- layout / style

    // One-time: reparents the buttons into a grid, styles the window, header, close
    // button and each card, and wires each card's click to buy its upgrade.
    private void StyleShop()
    {
        if (shopPanel == null) return;

        // Rounded window background.
        Image rootImg = shopPanel.GetComponent<Image>();
        if (rootImg != null) UIStyle.ApplyPanel(rootImg, UITheme.PanelDark);

        // Hide the old master/detail sub-panels - the grid replaces them.
        HideChild("LeftPanel");
        HideChild("RightPanel");
        if (buyButton != null) buyButton.gameObject.SetActive(false);

        // Gold header: reparent onto the window (so hiding the sub-panels can't hide it),
        // pin it top-left.
        if (shopGoldText != null)
        {
            shopGoldText.rectTransform.SetParent(shopPanel.transform, false);
            Anchor(shopGoldText.rectTransform, 0.05f, 0.88f, 0.70f, 0.98f);
            shopGoldText.alignment = TextAlignmentOptions.Left;
            shopGoldText.color = UITheme.Gold;
            shopGoldText.fontStyle = FontStyles.Bold;
            shopGoldText.enableAutoSizing = true;
            shopGoldText.fontSizeMin = 18f;
            shopGoldText.fontSizeMax = 34f;
        }

        // Close button: top-right corner.
        Transform closeT = shopPanel.transform.Find("CloseButton");
        if (closeT != null)
        {
            Button close = closeT.GetComponent<Button>();
            RectTransform crt = close.GetComponent<RectTransform>();
            crt.SetParent(shopPanel.transform, false);
            Anchor(crt, 0.80f, 0.88f, 0.97f, 0.98f);
            StyleSolidButton(close, UITheme.Danger, "CLOSE");
        }

        // Build the grid: reparent each button onto the window and place + style it.
        for (int i = 0; i < _cards.Count; i++)
        {
            string key = _cards[i].Key;
            Button btn = _cards[i].Value;

            btn.transform.SetParent(shopPanel.transform, false);
            LayoutShopCard(btn, i);
            StyleCardVisual(btn, key);

            string capturedKey = key; // capture for the closure
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => TryBuy(capturedKey));
        }
    }

    // Places one card in a 3x3 grid and lays out its four text lines.
    private void LayoutShopCard(Button btn, int index)
    {
        // Grid area inside the window (below the header, above the bottom edge).
        const float gx = 0.015f, gy = 0.02f;
        const float x0 = 0.04f, x1 = 0.96f, y0 = 0.06f, y1 = 0.83f;
        float cw = (x1 - x0 - 2f * gx) / 3f;
        float ch = (y1 - y0 - 2f * gy) / 3f;

        int col = index % 3;
        int row = index / 3;
        float left = x0 + col * (cw + gx);
        float top = y1 - row * (ch + gy);
        Anchor(btn.GetComponent<RectTransform>(), left, top - ch, left + cw, top);

        RectTransform rt = btn.GetComponent<RectTransform>();

        // Name (top). Reuse the button's existing label (first text child).
        TextMeshProUGUI name = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (name != null)
        {
            Anchor(name.rectTransform, 0.05f, 0.72f, 0.95f, 0.96f);
            name.text = GetUpgradeTitle(GetKeyForButton(btn));
            name.color = UITheme.TextLight;
            name.fontStyle = FontStyles.Bold;
            name.alignment = TextAlignmentOptions.Center;
            name.enableWordWrapping = true;
            name.enableAutoSizing = true;
            name.fontSizeMin = 13f;
            name.fontSizeMax = 22f;
        }

        // The other three lines are created once and refreshed each time.
        Anchor(GetOrCreateText(rt, "__Value").rectTransform, 0.05f, 0.50f, 0.95f, 0.70f);
        Anchor(GetOrCreateText(rt, "__Level").rectTransform, 0.05f, 0.31f, 0.95f, 0.48f);
        Anchor(GetOrCreateText(rt, "__Cost").rectTransform, 0.05f, 0.06f, 0.95f, 0.28f);
    }

    // Rounded card body, rarity-colored frame, hover/dim feedback.
    private void StyleCardVisual(Button btn, string key)
    {
        Image img = btn.GetComponent<Image>();
        UIStyle.ApplyPanel(img, Color.white); // rounded; ColorBlock below tints it
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = img;

        UIStyle.SetBorder(btn.GetComponent<RectTransform>(), GetShopRarity(key));

        ColorBlock cb = btn.colors;
        cb.normalColor = UITheme.Card;
        cb.highlightedColor = UITheme.CardHover;
        cb.pressedColor = UITheme.Gold;
        cb.selectedColor = UITheme.Card;
        cb.disabledColor = new Color(0.15f, 0.15f, 0.17f, 1f); // can't-afford / maxed = dim
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
    }

    // Fills every card's value / level / cost and dims the ones you can't buy.
    private void RefreshShopCards()
    {
        UpdateGoldText();
        int gold = PlayerPrefs.GetInt("PlayerGold", 0);

        foreach (KeyValuePair<string, Button> card in _cards)
        {
            string key = card.Key;
            Button btn = card.Value;
            if (btn == null) continue;

            int level = PlayerPrefs.GetInt(key, 0);
            bool maxed = level >= MaxLevel;
            int cost = CalculateCost(key, level);
            bool canAfford = !maxed && gold >= cost;

            RectTransform rt = btn.GetComponent<RectTransform>();

            // Value: current -> next (or just current when maxed).
            TextMeshProUGUI value = GetOrCreateText(rt, "__Value");
            value.text = maxed
                ? GetStatValueString(key, level)
                : $"{GetStatValueString(key, level)} -> {GetStatValueString(key, level + 1)}";
            value.color = UITheme.TextLight;
            value.alignment = TextAlignmentOptions.Center;
            value.enableWordWrapping = true;
            value.enableAutoSizing = true;
            value.fontSizeMin = 11f;
            value.fontSizeMax = 18f;

            // Level line.
            TextMeshProUGUI lvl = GetOrCreateText(rt, "__Level");
            lvl.text = $"LV {level}/{MaxLevel}";
            lvl.color = level > 0 ? UITheme.Gold : UITheme.TextMuted;
            lvl.fontStyle = FontStyles.Bold;
            lvl.alignment = TextAlignmentOptions.Center;
            lvl.enableAutoSizing = true;
            lvl.fontSizeMin = 11f;
            lvl.fontSizeMax = 20f;

            // Cost line, colored by whether you can afford it.
            TextMeshProUGUI costTxt = GetOrCreateText(rt, "__Cost");
            costTxt.text = maxed ? "MAXED" : $"{cost} G";
            costTxt.color = maxed ? UITheme.Gold : (canAfford ? UITheme.Affordable : UITheme.TooExpensive);
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.alignment = TextAlignmentOptions.Center;
            costTxt.enableAutoSizing = true;
            costTxt.fontSizeMin = 12f;
            costTxt.fontSizeMax = 22f;

            // Dim + disable clicks on cards you can't act on.
            btn.interactable = canAfford;
        }
    }

    // ---------------------------------------------------------------- small helpers

    private void HideChild(string childName)
    {
        if (shopPanel == null) return;
        Transform t = shopPanel.transform.Find(childName);
        if (t != null) t.gameObject.SetActive(false);
    }

    // Sets a RectTransform's anchors to fill the given normalized rect of its parent.
    private static void Anchor(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Gets or creates a named TextMeshPro child under a card (used for value/level/cost).
    private TextMeshProUGUI GetOrCreateText(RectTransform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

        GameObject go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.raycastTarget = false; // clicks go to the card button, not the label
        return t;
    }

    // Solid-colored button (Close) with a white label.
    private void StyleSolidButton(Button btn, Color color, string label)
    {
        if (btn == null) return;

        Image img = btn.GetComponent<Image>();
        UIStyle.ApplyPanel(img, Color.white);
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        cb.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;

        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = label;
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
        }
    }

    // Looks up a button's PlayerPrefs key (small list, so a linear scan is fine).
    private string GetKeyForButton(Button btn)
    {
        foreach (KeyValuePair<string, Button> card in _cards)
            if (card.Value == btn) return card.Key;
        return "";
    }

    // Groups the shop upgrades into rarity tiers, matching the level-up rarity colors.
    private Color GetShopRarity(string key)
    {
        switch (key)
        {
            case "Shop_AttackSpeed":
            case "Shop_SwordDamage":
            case "Shop_Defense":
                return UITheme.Rare;
            case "Shop_SwordCount":
            case "Shop_PierceCount":
                return UITheme.Epic;
            default:
                return UITheme.Common;
        }
    }

    // ---------------------------------------------------------------- pricing / values (unchanged)

    private int CalculateCost(string key, int level)
    {
        if (level >= MaxLevel) return 0;

        int baseCost = 100;
        int multiplier = 100;

        switch (key)
        {
            case "Shop_MoveSpeed":
                baseCost = 100; multiplier = 100; break;
            case "Shop_MaxHealth":
                baseCost = 100; multiplier = 100; break;
            case "Shop_SearchRange":
                baseCost = 100; multiplier = 120; break;
            case "Shop_ProjectileSpeed":
                baseCost = 100; multiplier = 120; break;

            case "Shop_AttackSpeed":
                baseCost = 150; multiplier = 180; break;
            case "Shop_SwordDamage":
                baseCost = 200; multiplier = 250; break;
            case "Shop_Defense":
                baseCost = 250; multiplier = 300; break;

            case "Shop_SwordCount":
                baseCost = 1000; multiplier = 1200; break;
            case "Shop_PierceCount":
                baseCost = 500; multiplier = 600; break;
        }

        // BaseCost + (Level * Multiplier) * (1.0 + Level * 0.25)
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
            case "Shop_SwordCount": return "Multi-Throw";
            case "Shop_PierceCount": return "Piercing Edge";
            case "Shop_Defense": return "Armor Defense";
            case "Shop_SearchRange": return "Search Range";
            case "Shop_ProjectileSpeed": return "Projectile Speed";
            default: return "Unknown Stat";
        }
    }

    private string GetStatValueString(string key, int level)
    {
        switch (key)
        {
            case "Shop_MaxHealth": return $"{100f + (level * 10f)} HP";
            case "Shop_MoveSpeed": return $"{6f + (level * 0.5f)} m/s";
            case "Shop_AttackSpeed": return $"{1.5f - (level * 0.1f):F1}s";
            case "Shop_SwordDamage": return $"{20f + (level * 4f)} dmg";
            case "Shop_SwordCount": return $"{1 + level} swords";
            case "Shop_PierceCount": return $"{1 + level} pierce";
            case "Shop_Defense": return $"+{level} armor";
            case "Shop_SearchRange": return $"{15f + (level * 1.5f)} range";
            case "Shop_ProjectileSpeed": return $"{15f + (level * 2f)} m/s";
            default: return "0";
        }
    }
}
