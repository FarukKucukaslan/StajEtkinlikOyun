using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Central "game juice" helper: floating damage numbers, hit-stop (a tiny time
/// freeze on impact) and enemy death debris. Everything is generated in code, so
/// the project needs no extra prefabs or art assets.
///
/// It creates itself automatically the first time a scene loads, so nothing has
/// to be placed in the scene by hand. Call the static methods from anywhere:
///     JuiceManager.DamageNumber(worldPos, amount);
///     JuiceManager.HitStop(0.03f);
///     JuiceManager.DeathPop(worldPos, color);
///     JuiceManager.Shake(0.2f);   // forwards to the camera
/// Screen shake itself lives on the camera (see TopDownCameraFollow.AddShake).
/// </summary>
public class JuiceManager : MonoBehaviour
{
    private static JuiceManager _instance;
    private Canvas _canvas;        // screen-space overlay that damage numbers live on
    private bool _hitStopActive;

    private Image _hitFlash;              // full-screen red vignette shown when the player is hit
    private Coroutine _flashRoutine;
    private static Sprite _vignetteSprite;

    // Runs once after the first scene loads; spawns the manager so no scene setup is needed.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("JuiceManager");
        _instance = go.AddComponent<JuiceManager>();
        DontDestroyOnLoad(go);
        _instance.BuildCanvas();
        _instance.BuildFlashOverlay();
    }

    private void BuildCanvas()
    {
        GameObject canvasObj = new GameObject("JuiceCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObj.transform.SetParent(transform, false);
        _canvas = canvasObj.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -1; // just under the game UI (which is 0) so numbers never cover panels

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
    }

    // ----- Player hit flash (red screen vignette) -----------------------

    // Full-screen red vignette on its own canvas above the HUD, hidden until a hit.
    private void BuildFlashOverlay()
    {
        GameObject canvasObj = new GameObject("JuiceFlashCanvas", typeof(Canvas));
        canvasObj.transform.SetParent(transform, false);
        Canvas c = canvasObj.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 50; // above the game HUD (0), so the flash reads clearly

        GameObject img = new GameObject("HitFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.SetParent(canvasObj.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _hitFlash = img.GetComponent<Image>();
        _hitFlash.sprite = GetVignetteSprite();
        _hitFlash.raycastTarget = false;
        _hitFlash.color = new Color(0.85f, 0.10f, 0.10f, 0f); // red, invisible until a hit
    }

    // Builds (once) a soft radial sprite: clear in the middle, opaque toward the edges.
    private static Sprite GetVignetteSprite()
    {
        if (_vignetteSprite != null) return _vignetteSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist; // 0 center -> ~1 edge
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d)); // clear middle, strong edges
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _vignetteSprite;
    }

    /// <summary>Pulses the red edge-flash. Call when the player takes damage.</summary>
    public static void PlayerHitFlash(float strength = 1f)
    {
        if (_instance != null) _instance.DoHitFlash(strength);
    }

    private void DoHitFlash(float strength)
    {
        if (_hitFlash == null) return;
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(HitFlashRoutine(Mathf.Clamp01(strength) * 0.55f));
    }

    // Snaps in, fades out. Unscaled so it looks right even if time is scaled.
    private IEnumerator HitFlashRoutine(float peak)
    {
        const float up = 0.05f, down = 0.35f;
        float t = 0f;
        while (t < up) { t += Time.unscaledDeltaTime; SetFlashAlpha(Mathf.Lerp(0f, peak, t / up)); yield return null; }
        t = 0f;
        while (t < down) { t += Time.unscaledDeltaTime; SetFlashAlpha(Mathf.Lerp(peak, 0f, t / down)); yield return null; }
        SetFlashAlpha(0f);
        _flashRoutine = null;
    }

    private void SetFlashAlpha(float a)
    {
        if (_hitFlash == null) return;
        Color col = _hitFlash.color;
        col.a = a;
        _hitFlash.color = col;
    }

    // ----- Screen shake (forwarded to the camera) -----------------------

    public static void Shake(float intensity)
    {
        if (TopDownCameraFollow.Instance != null)
            TopDownCameraFollow.Instance.AddShake(intensity);
    }

    // ----- Floating damage numbers --------------------------------------

    public static void DamageNumber(Vector3 worldPos, float amount)
    {
        if (_instance != null) _instance.SpawnDamageNumber(worldPos, amount);
    }

    private void SpawnDamageNumber(Vector3 worldPos, float amount)
    {
        if (_canvas == null || Camera.main == null) return;

        GameObject go = new GameObject("DamageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(_canvas.transform, false);

        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = Mathf.RoundToInt(amount).ToString();
        txt.fontSize = 36f;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(1f, 0.85f, 0.2f, 1f); // gold
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140f, 60f);
        // Start a little above the enemy, nudged sideways so stacked hits don't overlap.
        Vector3 screen = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 1.2f);
        screen.x += Random.Range(-25f, 25f);
        rt.position = screen;

        StartCoroutine(AnimateDamageNumber(rt, txt, screen));
    }

    // Rises, grows slightly, then fades out.
    private IEnumerator AnimateDamageNumber(RectTransform rt, TextMeshProUGUI txt, Vector3 startScreen)
    {
        const float dur = 0.6f;
        float t = 0f;
        Color baseColor = txt.color;
        while (t < dur)
        {
            // Unscaled so numbers keep animating (and clear away) even when the game
            // is paused for a level-up, instead of freezing on top of the panel.
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            rt.position = startScreen + new Vector3(0f, 70f * k, 0f);
            rt.localScale = Vector3.one * (1f + 0.3f * (1f - k));
            txt.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - k);
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    // ----- Hit-stop (micro time freeze on impact) -----------------------

    public static void HitStop(float duration)
    {
        if (_instance != null) _instance.DoHitStop(duration);
    }

    private void DoHitStop(float duration)
    {
        if (_hitStopActive) return;                 // one at a time; don't stack freezes
        if (Time.timeScale <= 0.01f) return;        // already paused (e.g. level-up) - leave it alone
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        _hitStopActive = true;
        float prev = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        // Only lift the freeze if nothing else paused the game in the meantime.
        if (Time.timeScale == 0f) Time.timeScale = prev;
        _hitStopActive = false;
    }

    // ----- Enemy death debris -------------------------------------------

    public static void DeathPop(Vector3 worldPos, Color color)
    {
        if (_instance != null) _instance.SpawnDeathPop(worldPos, color);
    }

    // ponytail: spawns a few primitive cubes per death. Fine at the game's enemy
    // counts; pool them if death-heavy scenes ever show GC spikes.
    private void SpawnDeathPop(Vector3 pos, Color color)
    {
        const int count = 6;
        var bits = new List<Transform>(count);
        var vels = new List<Vector3>(count);

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color); // URP Lit
        mpb.SetColor("_Color", color);     // Standard / legacy

        for (int i = 0; i < count; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(cube.GetComponent<Collider>()); // debris shouldn't collide with anything
            cube.transform.position = pos + Vector3.up * 0.5f;
            cube.transform.localScale = Vector3.one * Random.Range(0.15f, 0.30f);
            cube.GetComponent<Renderer>().SetPropertyBlock(mpb);

            bits.Add(cube.transform);
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), Random.Range(-1f, 1f)).normalized;
            vels.Add(dir * Random.Range(3f, 6f));
        }

        StartCoroutine(AnimateDeathPop(bits, vels));
    }

    // Flings the cubes out, pulls them down with fake gravity, shrinks them to nothing.
    private IEnumerator AnimateDeathPop(List<Transform> bits, List<Vector3> vels)
    {
        const float dur = 0.4f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i] == null) continue;
                vels[i] += Vector3.down * 12f * Time.deltaTime; // gravity
                bits[i].position += vels[i] * Time.deltaTime;
                bits[i].localScale = Vector3.Lerp(bits[i].localScale, Vector3.zero, t / dur);
            }
            yield return null;
        }
        foreach (Transform b in bits)
            if (b != null) Destroy(b.gameObject);
    }
}
