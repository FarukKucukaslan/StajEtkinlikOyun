using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gives the UI its "chunky roguelite" look: rounded panels and rarity-colored
/// frames. The rounded sprites are drawn in code (once, then cached), so the
/// project needs no extra image files. Pair with <see cref="UITheme"/> for colors.
/// </summary>
public static class UIStyle
{
    // --- Shape settings (tweak here to change the whole UI's roundness) ---
    private const int TextureSize = 64;      // sprite resolution (small = cheap, 9-slice scales it up)
    private const int CornerRadius = 20;     // corner roundness in texture pixels (higher = chunkier, Megabonk-style)
    private const int BorderPixels = 7;      // thickness of the outline sprite's ring

    private static Sprite _fill;
    private static Sprite _outline;

    /// <summary>Solid rounded rectangle, 9-sliced. Use for panel/card/button backgrounds.</summary>
    public static Sprite RoundedFill => _fill != null ? _fill : (_fill = Build(false));

    /// <summary>Hollow rounded rectangle (a frame), 9-sliced. Use for colored borders.</summary>
    public static Sprite RoundedOutline => _outline != null ? _outline : (_outline = Build(true));

    /// <summary>Applies a rounded background of the given color to an Image.</summary>
    public static void ApplyPanel(Image img, Color color)
    {
        if (img == null) return;
        img.sprite = RoundedFill;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 2f; // keeps corners crisp as the sprite stretches
        img.color = color;
    }

    /// <summary>
    /// Applies a plain, square-cornered background of the given color. Use for bars
    /// pressed flush against a screen edge: rounded corners there would leave the
    /// corner pixels transparent and show the game world bleeding through.
    /// </summary>
    public static void ApplyFlat(Image img, Color color)
    {
        if (img == null) return;
        img.sprite = null;              // null sprite = solid, square-cornered rectangle
        img.type = Image.Type.Simple;
        img.color = color;
    }

    /// <summary>
    /// Adds (or recolors) a rounded colored frame as a child of <paramref name="card"/>.
    /// Rendered above the card's background but behind its text. Safe to call repeatedly.
    /// </summary>
    public static Image SetBorder(RectTransform card, Color color)
    {
        if (card == null) return null;

        Transform existing = card.Find("__Border");
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject("__Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(card, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(0); // first child = drawn behind the text, above the fill
        }

        Image img = go.GetComponent<Image>();
        img.sprite = RoundedOutline;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 2f;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Draws the rounded sprite into a texture. outlineOnly = hollow frame instead of solid fill.
    private static Sprite Build(bool outlineOnly)
    {
        var tex = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                bool inside = InRoundedRect(x, y, TextureSize, TextureSize, CornerRadius);
                bool visible = inside;

                if (outlineOnly)
                {
                    // Hollow out the middle to leave only a border ring.
                    bool innerInside = InRoundedRect(x - BorderPixels, y - BorderPixels,
                        TextureSize - BorderPixels * 2, TextureSize - BorderPixels * 2, CornerRadius - BorderPixels);
                    visible = inside && !innerInside;
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, visible ? 1f : 0f));
            }
        }
        tex.Apply();

        var borders = new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius);
        return Sprite.Create(tex, new Rect(0, 0, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borders);
    }

    // Standard rounded-rectangle hit test: straight edges plus quarter-circle corners.
    private static bool InRoundedRect(float px, float py, float w, float h, float r)
    {
        if (r < 0f) r = 0f;
        // Middle vertical band (full height between the rounded corners)
        if (px >= r && px <= w - r) return py >= 0f && py <= h;
        // Middle horizontal band (full width between the rounded corners)
        if (py >= r && py <= h - r) return px >= 0f && px <= w;
        // Corner: inside only if within the corner's circle
        float cx = Mathf.Clamp(px, r, w - r);
        float cy = Mathf.Clamp(py, r, h - r);
        float dx = px - cx, dy = py - cy;
        return (dx * dx + dy * dy) <= r * r;
    }


/// <summary>
    /// Returns a child Image with the given name (creating it if missing). The caller
    /// sets anchors and sprite. Used for icon slots; never blocks clicks (raycast off).
    /// </summary>
    public static Image GetOrCreateChildImage(RectTransform parent, string childName)
    {
        if (parent == null) return null;

        Transform existing = parent.Find(childName);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.GetComponent<RectTransform>().SetParent(parent, false);
        }

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }
}
