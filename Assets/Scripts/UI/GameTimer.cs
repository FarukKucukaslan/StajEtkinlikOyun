using System.Collections;
using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timerText;

    [Header("Timer Visual Settings")]
    public Color defaultColor = Color.white;
    public Color minutePassedColor = Color.red;
    [Tooltip("Duration for the timer text to stay red when a minute passes.")]
    public float flashDuration = 1.5f;

    [Header("Spawner Reference")]
    [Tooltip("Reference to the spawner. Will auto-detect if left empty.")]
    public EnemySpawner enemySpawner;

    [Header("Difficulty Settings")]
    [Tooltip("Amount of seconds subtracted from the spawn interval every minute (making enemies spawn faster).")]
    public float intervalDecreaseAmount = 0.2f;

    [Tooltip("The lowest possible limit for the spawn interval to keep the game playable.")]
    public float minSpawnInterval = 0.2f;

    private float _elapsedTime;
    private int _lastIntervalTick = 0;
    private Coroutine _flashCoroutine;

    private void Start()
    {
        // Try to automatically find the EnemySpawner if not assigned in the Inspector
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (timerText != null)
        {
            timerText.color = defaultColor;
            UpdateTimerText();
        }
    }

    private void Update()
    {
        // Do not advance the clock if the game is paused (e.g., during level up)
        if (Time.timeScale <= 0f) return;

        _elapsedTime += Time.deltaTime;
        UpdateTimerText();

        // Detect when 30 seconds have passed
        int currentIntervalTick = (int)(_elapsedTime / 30f);
        if (currentIntervalTick > _lastIntervalTick)
        {
            _lastIntervalTick = currentIntervalTick;
            OnIntervalPassed();
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int minutes = (int)(_elapsedTime / 60f);
        int seconds = (int)(_elapsedTime % 60f);

        // Format: "01:05" (Minutes:Seconds)
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnIntervalPassed()
    {
        // Start the red flash effect (cancels previous flash routine if active)
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }
        _flashCoroutine = StartCoroutine(FlashTimerRedRoutine());

        // Make spawning faster by reducing the spawn interval
        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = Mathf.Max(minSpawnInterval, enemySpawner.spawnInterval - intervalDecreaseAmount);
            Debug.Log($"30 seconds passed! Difficulty scaled. New spawn interval: {enemySpawner.spawnInterval} seconds.");
        }
    }

    private IEnumerator FlashTimerRedRoutine()
    {
        if (timerText == null) yield break;

        // Turn text red
        timerText.color = minutePassedColor;

        // Wait in real-time seconds (unaffected by game time pauses)
        yield return new WaitForSecondsRealtime(flashDuration);

        // Turn text back to default color
        timerText.color = defaultColor;
        _flashCoroutine = null;
    }

    public void ResetTimer()
    {
        _elapsedTime = 0f;
        _lastIntervalTick = 0;
        UpdateTimerText();
        if (timerText != null)
        {
            timerText.color = defaultColor;
        }
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
    }
}
