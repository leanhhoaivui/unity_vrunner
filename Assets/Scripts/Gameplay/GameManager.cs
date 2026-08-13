using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Playing;
    [SerializeField] private float startDelay = 5f;
    [SerializeField] private float deathAnimDelay = 1.5f; // khớp length clip Death
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCollision playerCollision;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private PowerupManager powerupManager;
    
    [Header("UI References")]
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private GameObject pauseMenu;
    
    // [Header("Events")]
    // public UnityEvent OnGameStart;
    // public UnityEvent OnGamePause;
    // public UnityEvent OnGameResume;
    // public UnityEvent OnGameOver;
    
    public GameState CurrentState => currentState;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Find references if not assigned
        AutoFindReferences();
    }
    
    private void Start()
    {
        // StartGame();
        StartCoroutine(StartGameWithDelay());
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void AutoFindReferences()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        
        if (playerCollision == null)
        {
            playerCollision = FindObjectOfType<PlayerCollision>();
        }
        
        if (scoreManager == null)
        {
            scoreManager = ScoreManager.Instance;
        }
        
        if (powerupManager == null)
        {
            powerupManager = PowerupManager.Instance;
        }
        
        if (gameOverUI == null)
        {
            gameOverUI = FindObjectOfType<GameOverUI>();
        }
    }

    private IEnumerator StartGameWithDelay()
    {
        currentState = GameState.Menu; // hoặc thêm state Countdown
        if (playerController != null)
            playerController.enabled = false;
        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputEnabled(false);
        // Optional: hiện "3, 2, 1, GO!" UI
        yield return new WaitForSecondsRealtime(startDelay);
         
        StartGame(); // set Playing, enable input, OnGameStart...
        
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.PlayRunning();
        }
    }
    
    /// <summary>
    /// Start game
    /// </summary>
    public void StartGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        // Reset systems
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        
        if (playerCollision != null)
        {
            playerCollision.ResetState();
        }
        
        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputEnabled(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameMusic();

        // OnGameStart?.Invoke();
        Debug.Log("Game Started!");
    }
    
    /// <summary>
    /// Pause game
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.Paused;
        Time.timeScale = 0f;

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputEnabled(false);
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
        
        // OnGamePause?.Invoke();
        Debug.Log("Game Paused");
    }
    
    /// <summary>
    /// Resume game
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        
        currentState = GameState.Playing;
        Time.timeScale = 1f;

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputEnabled(true);
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        
        // OnGameResume?.Invoke();
        Debug.Log("Game Resumed");
    }
    
    /// <summary>
    /// Game over
    /// </summary>
    public void GameOver()
    {
        if (currentState == GameState.GameOver) return;
        
        currentState = GameState.GameOver;

        if (InputManager.Instance != null)
            InputManager.Instance.SetGameplayInputEnabled(false);
        
        Debug.Log("GAME OVER!");
        
        // Stop player
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        StartCoroutine(ShowGameOverAfterDeath());
    }

    private IEnumerator ShowGameOverAfterDeath()
    {
        // Dùng WaitForSeconds (không Realtime) nếu timeScale vẫn = 1
        // và Animator Update Mode = Normal
        // float len = playerController.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).length;
        // yield return new WaitForSeconds(len);
        yield return new WaitForSeconds(deathAnimDelay);

        // Get final stats
        int finalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        int coins = scoreManager != null ? scoreManager.CoinsCollected : 0;
        float distance = scoreManager != null ? scoreManager.DistanceTraveled : 0f;
        
        // Check high score
        bool isNewHighScore = SaveManager.Instance.IsNewHighScore(finalScore);
        if (isNewHighScore)
        {
            SaveManager.Instance.SetHighScore(finalScore);
        }
        
        // Save stats
        SaveManager.Instance.AddCoins(coins);
        SaveManager.Instance.AddDistance(distance);
        SaveManager.Instance.IncrementGamesPlayed();
        
        // Show game over UI
        if (gameOverUI != null)
        {
            gameOverUI.Show(finalScore, SaveManager.Instance.GetHighScore(), coins, distance);
        }
        
        // OnGameOver?.Invoke();
    }
    
    /// <summary>
    /// Restart game
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// Load menu scene
    /// </summary>
    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuScene");
    }
    
    /// <summary>
    /// Handle input
    /// </summary>
    private void HandleInput()
    {
        // Pause/Resume
        // if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        // {
        //     if (currentState == GameState.Playing)
        //     {
        //         PauseGame();
        //     }
        //     else if (currentState == GameState.Paused)
        //     {
        //         ResumeGame();
        //     }
        // }
    }
    
    /// <summary>
    /// Quit game
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // Debug commands (add to GameManager)
    [ContextMenu("Print Save Data")]
    public void PrintSaveData()
    {
        SaveData data = SaveManager.Instance.CurrentSave;
        Debug.Log($"High Score: {data.highScore}");
        Debug.Log($"Total Coins: {data.totalCoins}");
        Debug.Log($"Games Played: {data.gamesPlayed}");
    }

    [ContextMenu("Reset Save")]
    public void ResetSaveData()
    {
        SaveManager.Instance.ResetSave();
    }
}