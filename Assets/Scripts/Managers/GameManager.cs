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
    [Tooltip("Drag all gameplay HUD elements (Sliders, Texts) here to turn them off during main menu.")]
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
        // Auto-find references if not assigned
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

        // Initialize state: Pause game and open Main Menu
        ReturnToMainMenu();
    }

    public void StartGame()
    {
        // 1. Reset player position
        if (playerObj != null)
        {
            // Temporarily disable CharacterController during teleport to avoid conflicts
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            playerObj.transform.position = _playerStartPos;
            playerObj.transform.rotation = Quaternion.identity;
            
            if (cc != null) cc.enabled = true;
        }

        // 2. Load shop upgrades and reset player stats
        if (playerObj != null)
        {
            // Call Start() manually or re-trigger loading of player/weapon stats
            playerObj.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // 3. Reset UI and timer
        if (gameTimer != null)
        {
            gameTimer.ResetTimer();
        }

        // 4. Clear old enemies and enable spawner
        if (enemySpawner != null)
        {
            enemySpawner.ClearAllEnemies();
            enemySpawner.isSpawning = true;
            
            // Reset the spawner interval back to its baseline
            // (The baseline is loaded in EnemySpawner's start, we can reset it)
            enemySpawner.spawnInterval = 1.5f; 
        }

        // 5. Hide main menu buttons & stats panel, show gameplay HUD elements
        if (playButton != null) playButton.SetActive(false);
        if (marketButton != null) marketButton.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetStatsPanelActive(false);
        }
        
        if (gameplayHUDObjects != null)
        {
            foreach (GameObject hudObj in gameplayHUDObjects)
            {
                if (hudObj != null) hudObj.SetActive(true);
            }
        }

        // 6. Set TimeScale to 1 (unpause game)
        Time.timeScale = 1f;

        Debug.Log("Game Started!");
    }

    public void OnPlayerDeath(int goldCollectedThisRun)
    {
        // 1. Save gold collected permanently
        int totalGold = PlayerPrefs.GetInt("PlayerGold", 0);
        PlayerPrefs.SetInt("PlayerGold", totalGold + goldCollectedThisRun);
        PlayerPrefs.Save();

        Debug.Log($"Player Died! Collected Gold: {goldCollectedThisRun}. Total Gold: {totalGold + goldCollectedThisRun}");

        // 2. Return to main menu and pause the game
        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        // Pause time Scale
        Time.timeScale = 0f;

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable enemy spawning and clear remaining enemies
        if (enemySpawner != null)
        {
            enemySpawner.isSpawning = false;
            enemySpawner.ClearAllEnemies();
        }

        // Force player components to reload baseline stats from PlayerPrefs (resets level-up bonuses)
        if (playerObj != null)
        {
            playerObj.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // Toggle UI Panels, Buttons & Stats Panel
        if (playButton != null) playButton.SetActive(true);
        if (marketButton != null) marketButton.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetStatsPanelActive(true);
        }
        
        if (gameplayHUDObjects != null)
        {
            foreach (GameObject hudObj in gameplayHUDObjects)
            {
                if (hudObj != null) hudObj.SetActive(false);
            }
        }

        // Reset shop UI if opened (ShopManager handles this via close button)
    }
}
