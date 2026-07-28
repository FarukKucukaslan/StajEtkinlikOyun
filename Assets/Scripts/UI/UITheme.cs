using UnityEngine;

/// <summary>
/// Shared color palette for the game's UI (shop + level-up screens).
/// Everything visual is defined here in one place, so re-skinning the whole
/// UI is just a matter of changing these values.
/// </summary>
public static class UITheme
{
    // --- Surfaces (dark, cohesive charcoal theme) ---
    public static readonly Color PanelDark = new Color(0.10f, 0.10f, 0.12f, 0.98f); // main window background
    public static readonly Color PanelMid  = new Color(0.16f, 0.16f, 0.19f, 1f);    // sub-panels inside a window
    public static readonly Color Card      = new Color(0.22f, 0.22f, 0.26f, 1f);    // buttons / upgrade cards
    public static readonly Color CardHover = new Color(0.30f, 0.30f, 0.35f, 1f);    // button hover state

    // --- Text ---
    public static readonly Color TextLight = new Color(0.92f, 0.92f, 0.95f, 1f);
    public static readonly Color TextMuted = new Color(0.65f, 0.65f, 0.70f, 1f);
    public static readonly Color Gold      = new Color(1f, 0.78f, 0f, 1f);          // headings / currency

    // --- Feedback ---
    public static readonly Color Affordable   = new Color(0.55f, 0.85f, 0.35f, 1f); // "you can buy this" green
    public static readonly Color TooExpensive = new Color(0.90f, 0.35f, 0.35f, 1f); // "not enough gold" red
    public static readonly Color Danger       = new Color(0.80f, 0.25f, 0.25f, 1f); // close / cancel

    // --- Rarity (matches the level-up rarity labels) ---
    public static readonly Color Common = new Color(0.55f, 0.76f, 0.29f, 1f); // green
    public static readonly Color Rare   = new Color(0.00f, 0.74f, 0.83f, 1f); // cyan
    public static readonly Color Epic   = new Color(0.91f, 0.12f, 0.39f, 1f); // magenta

    /// <summary>Returns the color for a rarity string ("Common" / "Rare" / "Epic").</summary>
    public static Color Rarity(string rarity)
    {
        switch (rarity)
        {
            case "Rare": return Rare;
            case "Epic": return Epic;
            default:     return Common;
        }
    }
}
