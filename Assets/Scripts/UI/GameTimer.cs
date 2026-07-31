using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public static event Action OnFiveMinutesPassed;

    [Serializable]
    public struct TimedEnemySpawn
    {
        [Tooltip("Enemy prefab to spawn (must have EnemyHealth and an AI component, e.g. ItemThrowerEnemy).")]
        public GameObject enemyPrefab;

        [Tooltip("Elapsed run time, in minutes, at which this enemy first spawns.")]
        public float spawnAtMinute;

        [Tooltip("If greater than 0, the enemy spawns again every this many minutes after the first spawn.")]
        public float repeatEveryMinutes;

        [Tooltip("Distance from the player at which the enemy spawns.")]
        public float spawnRadius;

        [HideInInspector]
        public float nextSpawnTime;
    }

    [Header("Timed Enemy Spawns")]
    [Tooltip("Special enemies that spawn once the run reaches a specific elapsed time.")]
    public List<TimedEnemySpawn> timedEnemySpawns = new List<TimedEnemySpawn>();

    [Header("Timed Enemy Warning")]
    [Tooltip("Text flashed every 30 seconds while a timed enemy spawn is still pending.")]
    public TextMeshProUGUI timedEnemyWarningText;

    [Tooltip("Message shown in the warning text.")]
    public string timedEnemyWarningMessage = "HE IS COMING";

    [Tooltip("How long the warning stays visible each time it flashes.")]
    public float timedEnemyWarningDuration = 2f;

    private Coroutine _timedEnemyWarningCoroutine;

    private Transform _player;

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
    private int _lastFiveMinuteTick = 0;
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

        if (timedEnemyWarningText != null)
        {
            timedEnemyWarningText.gameObject.SetActive(false);
        }

        InitializeTimedEnemySpawns();
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

        // Detect every 5 minutes
        int currentFiveMinuteTick = (int)(_elapsedTime / 300f);
        if (currentFiveMinuteTick > _lastFiveMinuteTick)
        {
            _lastFiveMinuteTick = currentFiveMinuteTick;
            OnFiveMinutesPassed?.Invoke();

            Debug.Log($"5 minutes passed! ({currentFiveMinuteTick * 5} minutes)");
        }

        UpdateTimedEnemySpawns();
    }

    private void InitializeTimedEnemySpawns()
    {
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                _player = playerObject.transform;
            }
        }

        for (int i = 0; i < timedEnemySpawns.Count; i++)
        {
            TimedEnemySpawn spawn = timedEnemySpawns[i];
            spawn.nextSpawnTime = spawn.spawnAtMinute * 60f;
            timedEnemySpawns[i] = spawn;
        }
    }

    private void UpdateTimedEnemySpawns()
    {
        if (_player == null)
        {
            return;
        }

        for (int i = 0; i < timedEnemySpawns.Count; i++)
        {
            TimedEnemySpawn spawn = timedEnemySpawns[i];

            if (spawn.enemyPrefab == null || _elapsedTime < spawn.nextSpawnTime)
            {
                continue;
            }

            SpawnTimedEnemy(spawn);

            if (spawn.repeatEveryMinutes > 0f)
            {
                spawn.nextSpawnTime += spawn.repeatEveryMinutes * 60f;
            }
            else
            {
                spawn.nextSpawnTime = float.MaxValue;
            }

            timedEnemySpawns[i] = spawn;
        }
    }

    private void SpawnTimedEnemy(TimedEnemySpawn spawn)
    {
        Vector2 randomOffset2D = UnityEngine.Random.insideUnitCircle.normalized * spawn.spawnRadius;

        Vector3 spawnPosition = _player.position + new Vector3(randomOffset2D.x, 0f, randomOffset2D.y);

        Ray groundRay = new Ray(spawnPosition + Vector3.up * 25f, Vector3.down);

        LayerMask groundMask = enemySpawner != null ? enemySpawner.groundMask : (LayerMask)0;

        if (Physics.Raycast(groundRay, out RaycastHit hit, 50f, groundMask, QueryTriggerInteraction.Ignore))
        {
            spawnPosition.y = hit.point.y;
        }
        else
        {
            spawnPosition.y = _player.position.y;
        }

        Instantiate(spawn.enemyPrefab, spawnPosition, Quaternion.identity);

        JuiceManager.ChaosPulse();
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int minutes = (int)(_elapsedTime / 60f);
        int seconds = (int)(_elapsedTime % 60f);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnIntervalPassed()
    {
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
        }

        _flashCoroutine = StartCoroutine(FlashTimerRedRoutine());

        if (enemySpawner != null)
        {
            enemySpawner.spawnInterval = Mathf.Max(
                minSpawnInterval,
                enemySpawner.spawnInterval - intervalDecreaseAmount);

            Debug.Log($"30 seconds passed! Difficulty scaled. New spawn interval: {enemySpawner.spawnInterval} seconds.");
        }

        ShowTimedEnemyWarningIfPending();
    }

    private IEnumerator FlashTimerRedRoutine()
    {
        if (timerText == null) yield break;

        timerText.color = minutePassedColor;

        yield return new WaitForSecondsRealtime(flashDuration);

        timerText.color = defaultColor;
        _flashCoroutine = null;
    }

    // Flashes the warning text if any timed enemy spawn hasn't happened yet.
    private void ShowTimedEnemyWarningIfPending()
    {
        if (timedEnemyWarningText == null)
        {
            return;
        }

        bool hasPendingSpawn = false;

        foreach (TimedEnemySpawn spawn in timedEnemySpawns)
        {
            if (spawn.enemyPrefab != null
                && spawn.nextSpawnTime > _elapsedTime
                && spawn.nextSpawnTime < float.MaxValue)
            {
                hasPendingSpawn = true;
                break;
            }
        }

        if (!hasPendingSpawn)
        {
            return;
        }

        if (_timedEnemyWarningCoroutine != null)
        {
            StopCoroutine(_timedEnemyWarningCoroutine);
        }

        _timedEnemyWarningCoroutine = StartCoroutine(TimedEnemyWarningRoutine());
    }

    private IEnumerator TimedEnemyWarningRoutine()
    {
        timedEnemyWarningText.text = timedEnemyWarningMessage;
        timedEnemyWarningText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(timedEnemyWarningDuration);

        timedEnemyWarningText.gameObject.SetActive(false);
        _timedEnemyWarningCoroutine = null;
    }

    public void ResetTimer()
    {
        _elapsedTime = 0f;
        _lastIntervalTick = 0;
        _lastFiveMinuteTick = 0;

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

        if (_timedEnemyWarningCoroutine != null)
        {
            StopCoroutine(_timedEnemyWarningCoroutine);
            _timedEnemyWarningCoroutine = null;
        }

        if (timedEnemyWarningText != null)
        {
            timedEnemyWarningText.gameObject.SetActive(false);
        }

        InitializeTimedEnemySpawns();
    }

    // Seconds survived this run. Used by the death screen to show run length.
    public float GetElapsedTime()
    {
        return _elapsedTime;
    }
}