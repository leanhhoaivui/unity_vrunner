using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    
    [Header("Score Settings")]
    [SerializeField] private int coinsCollected = 0;
    [SerializeField] private float distanceTraveled = 0f;
    [SerializeField] private int currentScore = 0;
    
    [Header("Score Calculation")]
    [SerializeField] private int coinValue = 1;
    [SerializeField] private float distanceMultiplier = 1f; // 1 meter = 1 điểm
    [SerializeField] private int coinScoreMultiplier = 10; // 1 coin = 10 điểm
    
    [Header("Multipliers")]
    [SerializeField] private float scoreMultiplier = 1f;
    
    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;
    public UnityEvent<int> OnCoinsChanged;
    public UnityEvent<float> OnDistanceChanged;
    
    [Header("References")]
    [SerializeField] private Transform player;
    
    private Vector3 startPosition;
    
    // Properties
    public int CoinsCollected => coinsCollected;
    public float DistanceTraveled => distanceTraveled;
    public int CurrentScore => currentScore;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        if (player != null)
        {
            startPosition = player.position;
        }
        
        ResetScore();
    }
    
    private void Update()
    {
        UpdateDistance();
    }
    
    /// <summary>
    /// Cập nhật distance traveled
    /// </summary>
    private void UpdateDistance()
    {
        if (player == null) return;
        
        float distance = player.position.z - startPosition.z;
        
        if (distance > distanceTraveled)
        {
            distanceTraveled = distance;
            OnDistanceChanged?.Invoke(distanceTraveled);
            CalculateScore();
        }
    }
    
    /// <summary>
    /// Add coins
    /// </summary>
    public void AddCoins(int amount)
    {
        coinsCollected += amount;
        OnCoinsChanged?.Invoke(coinsCollected);
        CalculateScore();
    }
    
    /// <summary>
    /// Calculate total score
    /// </summary>
    private void CalculateScore()
    {
        int distanceScore = Mathf.FloorToInt(distanceTraveled * distanceMultiplier);
        int coinScore = coinsCollected * coinScoreMultiplier;
        
        currentScore = Mathf.FloorToInt((distanceScore + coinScore) * scoreMultiplier);
        
        OnScoreChanged?.Invoke(currentScore);
    }
    
    /// <summary>
    /// Set score multiplier (power-ups)
    /// </summary>
    public void SetScoreMultiplier(float multiplier)
    {
        scoreMultiplier = multiplier;
        CalculateScore();
    }
    
    /// <summary>
    /// Reset score
    /// </summary>
    public void ResetScore()
    {
        coinsCollected = 0;
        distanceTraveled = 0f;
        currentScore = 0;
        scoreMultiplier = 1f;
        
        if (player != null)
        {
            startPosition = player.position;
        }
        
        OnScoreChanged?.Invoke(0);
        OnCoinsChanged?.Invoke(0);
        OnDistanceChanged?.Invoke(0f);
    }
    
    /// <summary>
    /// Get formatted score string
    /// </summary>
    public string GetScoreText()
    {
        return currentScore.ToString("N0"); // Có comma separator
    }
    
    public string GetDistanceText()
    {
        return $"{Mathf.FloorToInt(distanceTraveled)}m";
    }
    
    public string GetCoinsText()
    {
        return coinsCollected.ToString();
    }
}