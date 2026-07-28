using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Screen-clearing bomb ability: press SPACE to blast every enemy on screen (they
/// die through the normal path, so they still juice + drop XP/gold) plus a big
/// screen shake. On a cooldown so it can't be spammed.
///
/// Also draws a small on-screen indicator (bottom-center) so the player knows the
/// key exists and when it's ready: gold "SPACE / BOMB" when ready, dimmed with a
/// "Ns" countdown while on cooldown.
///
/// Self-bootstraps at runtime like <see cref="JuiceManager"/>, so nothing has to
/// be placed in the scene. Uses the new Input System (the project's input mode).
/// </summary>
public class BombAbility : MonoBehaviour
{
    [Tooltip("Seconds between bomb uses.")]
    public static float Cooldown = 15f;

    private static readonly Color ReadyColor = new Color(1f, 0.85f, 0.20f); // gold
    private static readonly Color CoolColor  = new Color(0.55f, 0.55f, 0.60f); // dim grey

    private static BombAbility _instance;
    private float _readyAt;

    private TextMeshProUGUI _label;
    private Image _border;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("BombAbility");
        _instance = go.AddComponent<BombAbility>();
        DontDestroyOnLoad(go);
        _instance.BuildUI();
    }

    private void Update()
    {
        bool ready = Time.time >= _readyAt;

        if (ready
            && Keyboard.current != null
            && Keyboard.current.spaceKey.wasPressedThisFrame
            && Time.timeScale > 0f)          // ignore while paused (level-up / shop)
        {
            Detonate();
            _readyAt = Time.time + Cooldown;
        }

        UpdateIndicator();
    }

    private void Detonate()
    {
        // Huge damage to every living enemy -> each dies through the normal path,
        // so it still gives death debris, damage numbers, XP and gold.
        EnemyHealth[] enemies = Object.FindObjectsOfType<EnemyHealth>();
        foreach (EnemyHealth e in enemies)
        {
            if (e != null) e.TakeDamage(99999f);
        }

        JuiceManager.Shake(0.6f);
        Debug.Log($"BOMB! cleared {enemies.Length} enemies.");
    }

    // ---- On-screen indicator -------------------------------------------

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("BombCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObj.transform.SetParent(transform, false);
        Canvas c = canvasObj.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 40;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Rounded box, bottom-center.
        GameObject boxObj = new GameObject("BombIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform brt = boxObj.GetComponent<RectTransform>();
        brt.SetParent(canvasObj.transform, false);
        brt.anchorMin = new Vector2(0.5f, 0f);
        brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(180f, 76f);
        brt.anchoredPosition = new Vector2(0f, 24f);
        UIStyle.ApplyPanel(boxObj.GetComponent<Image>(), new Color(0.10f, 0.10f, 0.12f, 0.92f));
        _border = UIStyle.SetBorder(brt, ReadyColor); // recolored in UpdateIndicator

        // Label.
        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.SetParent(brt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 6f);
        trt.offsetMax = new Vector2(-8f, -6f);
        _label = txtObj.GetComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontStyle = FontStyles.Bold;
        _label.enableAutoSizing = true;
        _label.fontSizeMin = 14f;
        _label.fontSizeMax = 24f;
        _label.raycastTarget = false;

        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (_label == null) return;

        float remaining = _readyAt - Time.time;
        if (remaining <= 0f)
        {
            _label.text = "SPACE\n<size=70%>BOMB</size>";
            _label.color = ReadyColor;
            if (_border != null) _border.color = ReadyColor;
        }
        else
        {
            _label.text = $"BOMB\n<size=80%>{Mathf.CeilToInt(remaining)}s</size>";
            _label.color = CoolColor;
            if (_border != null) _border.color = CoolColor;
        }
    }
}
