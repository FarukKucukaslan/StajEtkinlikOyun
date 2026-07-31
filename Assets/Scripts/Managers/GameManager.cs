using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player References")]
    public GameObject playerObj;

    private PlayerStats _playerStats;
    private PlayerController _playerController;
    private SwordThrower _swordThrower;

    [Header("Core System References")]
    public EnemySpawner enemySpawner;
    public GameTimer gameTimer;

    [Header("UI Controls")]
    public GameObject playButton;
    public GameObject marketButton;

    [Tooltip(
        "Drag all gameplay HUD elements (Sliders, Texts) here to turn them off during main menu."
    )]
    public GameObject[] gameplayHUDObjects;

    private Vector3 _playerStartPos;

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
        // Auto-find references if not assigned.
        if (playerObj == null)
        {
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }

        if (playerObj != null)
        {
            _playerStats = playerObj.GetComponent<PlayerStats>();
            _playerController = playerObj.GetComponent<PlayerController>();
            _swordThrower = playerObj.GetComponent<SwordThrower>();

            _playerStartPos = playerObj.transform.position;
        }

        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (gameTimer == null)
        {
            gameTimer = FindFirstObjectByType<GameTimer>();
        }

        // Give the main-menu buttons the game's visual theme.
        StyleMenuButtons();

        // Initialize state: pause game and open main menu.
        ReturnToMainMenu();
    }

    public void StartGame()
    {
        // 1. Reset player position.
        if (playerObj != null)
        {
            CharacterController characterController = playerObj.GetComponent<CharacterController>();

            // CharacterController must be disabled while teleporting.
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            playerObj.transform.position = _playerStartPos;
            playerObj.transform.rotation = Quaternion.identity;

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        // 2. Load shop upgrades and reset player stats.
        if (playerObj != null)
        {
            playerObj.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // 3. Reset timer.
        if (gameTimer != null)
        {
            gameTimer.ResetTimer();
        }

        JuiceManager.ClearChaosDarken();

        // 4. Clear objects left from the previous run.
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();

            enemySpawner.spawnInterval = 1.5f;
            enemySpawner.isSpawning = true;
        }

        ClearExperiencePickups();
        ClearEliteRewardChests();

        // 5. Hide main menu and show gameplay HUD.
        if (playButton != null)
        {
            playButton.SetActive(false);
        }

        if (marketButton != null)
        {
            marketButton.SetActive(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetStatsPanelActive(false);
        }

        if (gameplayHUDObjects != null)
        {
            foreach (GameObject hudObject in gameplayHUDObjects)
            {
                if (hudObject != null)
                {
                    hudObject.SetActive(true);
                }
            }
        }

        // 6. Unpause game.
        Time.timeScale = 1f;

        Debug.Log("Game Started!");
    }

    public void OnPlayerDeath(int goldCollectedThisRun)
    {
        // 1. Save collected gold permanently.
        int totalGold = PlayerPrefs.GetInt("PlayerGold", 0);

        totalGold += goldCollectedThisRun;

        PlayerPrefs.SetInt("PlayerGold", totalGold);
        PlayerPrefs.Save();

        Debug.Log(
            $"Player Died! Collected Gold: {goldCollectedThisRun}. " + $"Total Gold: {totalGold}"
        );

        // 2. Pause the game and release the cursor.
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Stop spawning and clear remaining gameplay objects.
        if (enemySpawner != null)
        {
            enemySpawner.isSpawning = false;
            enemySpawner.ClearAllEnemies();
        }

        ClearExperiencePickups();
        ClearEliteRewardChests();

        // 4. Show results screen.
        float survivalTime = gameTimer != null ? gameTimer.GetElapsedTime() : 0f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDeathScreen(goldCollectedThisRun, totalGold, survivalTime);
        }
        else
        {
            // No UI available, return directly to main menu.
            ReturnToMainMenu();
        }
    }

    public void ReturnToMainMenu()
    {
        // 1. Pause game.
        Time.timeScale = 0f;

        // 2. Unlock and show cursor.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Stop spawning and clear gameplay objects.
        if (enemySpawner != null)
        {
            enemySpawner.isSpawning = false;
            enemySpawner.ClearAllEnemies();
        }

        ClearExperiencePickups();
        ClearEliteRewardChests();

        // 4. Reload baseline player stats from PlayerPrefs.
        if (playerObj != null)
        {
            playerObj.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // 5. Show main menu UI.
        if (playButton != null)
        {
            playButton.SetActive(true);
        }

        if (marketButton != null)
        {
            marketButton.SetActive(true);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetStatsPanelActive(true);
            UIManager.Instance.HideDeathScreen();
        }

        if (gameplayHUDObjects != null)
        {
            foreach (GameObject hudObject in gameplayHUDObjects)
            {
                if (hudObject != null)
                {
                    hudObject.SetActive(false);
                }
            }
        }
    }

    private void ClearExperiencePickups()
    {
        ExperiencePickup[] pickups = FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None);

        foreach (ExperiencePickup pickup in pickups)
        {
            if (pickup != null)
            {
                Destroy(pickup.gameObject);
            }
        }
    }

    // Styles the PLAY and SHOP buttons to match the rest of the UI.
    private void StyleMenuButtons()
    {
        StyleMenuButton(playButton, "PLAY", UITheme.Affordable);

        StyleMenuButton(marketButton, "SHOP", UITheme.Gold);
    }

    private void StyleMenuButton(GameObject buttonObject, string label, Color accent)
    {
        if (buttonObject == null)
        {
            return;
        }

        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();

        UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();

        if (image != null)
        {
            UIStyle.ApplyPanel(image, Color.white);
        }

        if (button != null)
        {
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

            button.targetGraphic = image;

            UnityEngine.UI.ColorBlock colors = button.colors;

            colors.normalColor = UITheme.Card;
            colors.highlightedColor = Color.Lerp(UITheme.Card, accent, 0.5f);

            colors.pressedColor = Color.Lerp(UITheme.Card, accent, 0.7f);

            colors.selectedColor = UITheme.Card;
            colors.fadeDuration = 0.1f;

            button.colors = colors;
        }

        // Prefer TextMeshPro, fall back to legacy UI Text.
        TMPro.TextMeshProUGUI tmp = buttonObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();

        if (tmp != null)
        {
            tmp.text = label;
            tmp.color = accent;
            tmp.fontStyle = TMPro.FontStyles.Bold | TMPro.FontStyles.UpperCase;

            tmp.characterSpacing = 6f;
            return;
        }

        UnityEngine.UI.Text legacyText = buttonObject.GetComponentInChildren<UnityEngine.UI.Text>();

        if (legacyText != null)
        {
            legacyText.text = label.ToUpper();
            legacyText.color = accent;
            legacyText.fontStyle = FontStyle.Bold;
        }
    }

    private void ClearEliteRewardChests()
    {
        EliteRewardChest[] rewardChests = FindObjectsByType<EliteRewardChest>(
            FindObjectsSortMode.None
        );

        foreach (EliteRewardChest rewardChest in rewardChests)
        {
            if (rewardChest != null)
            {
                Destroy(rewardChest.gameObject);
            }
        }
    }
}
