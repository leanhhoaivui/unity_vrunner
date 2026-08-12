using UnityEngine;
using UnityEngine.Events;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;
    
    [SerializeField] private DifficultyConfig config;
    
    // [Header("Events")]
    // public UnityEvent<DifficultyTier> OnDifficultyChanged;
    
    private DifficultyTier currentTier;
    private float currentDistance;
    
    public DifficultyTier CurrentTier => currentTier;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    private void Start()
    {
        if (config != null && config.tiers.Length > 0)
        {
            currentTier = config.tiers[0];
        }
    }
    
    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        
        UpdateDifficulty();
    }
    
    private void UpdateDifficulty()
    {
        if (ScoreManager.Instance == null || config == null) return;
        
        currentDistance = ScoreManager.Instance.DistanceTraveled;
        
        // Find matching tier
        foreach (DifficultyTier tier in config.tiers)
        {
            if (currentDistance >= tier.minDistance && 
                (tier.maxDistance <= 0 || currentDistance < tier.maxDistance))
            {
                if (currentTier != tier)
                {
                    currentTier = tier;
                    // OnDifficultyChanged?.Invoke(tier);
                    Debug.Log($"Difficulty changed to: {tier.name}");
                }
                break;
            }
        }
    }
    
    // public float GetObstacleSpawnChance()
    // {
    //     return currentTier != null ? currentTier.obstacleSpawnChance : 0.5f;
    // }
    
    public float GetSpeedMultiplier()
    {
        return currentTier != null ? currentTier.speedMultiplier : 1f;
    }
}