using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject pausePanel;

    [Header("Debug")]
    [Tooltip("Spawns one elite enemy immediately whenever the game is paused.")]
    public bool spawnEliteOnPause;

    public EnemySpawner enemySpawner;

    public bool IsPaused => _isPaused;

    private bool _isPaused;

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
    }

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // Hooked to the pause button's OnClick.
    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        // Time.timeScale is already 0 during the main menu, level-up screen,
        // elite reward selection and death screen, so this naturally refuses
        // to open the pause panel on top of any of those.
        if (_isPaused || Time.timeScale <= 0f)
        {
            return;
        }

        _isPaused = true;
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (spawnEliteOnPause && enemySpawner != null)
        {
            enemySpawner.ForceSpawnElite();
        }
    }

    // Hooked to the Resume button's OnClick.
    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    // Hooked to the Exit button's OnClick.
    public void ExitToMainMenu()
    {
        _isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }
}
