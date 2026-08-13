using UnityEngine;
using System;

/// <summary>
/// Centralized event manager sử dụng C# Actions
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // ===== GAME EVENTS =====
    
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameOver;
    
    public void TriggerGameStart() => OnGameStart?.Invoke();
    public void TriggerGamePause() => OnGamePause?.Invoke();
    public void TriggerGameResume() => OnGameResume?.Invoke();
    public void TriggerGameOver() => OnGameOver?.Invoke();
    
    // ===== PLAYER EVENTS =====
    
    public event Action OnPlayerDeath;
    public event Action<float> OnPlayerJump; // float = jump height
    public event Action<int> OnPlayerLaneChanged; // int = new lane index
    
    public void TriggerPlayerDeath() => OnPlayerDeath?.Invoke();
    public void TriggerPlayerJump(float height) => OnPlayerJump?.Invoke(height);
    public void TriggerPlayerLaneChanged(int lane) => OnPlayerLaneChanged?.Invoke(lane);
    
    // ===== COLLECTIBLE EVENTS =====
    
    public event Action<int> OnCoinCollected; // int = coin value
    public event Action<PowerupType, float> OnPowerupActivated; // type, duration
    public event Action<PowerupType> OnPowerupExpired;
    
    public void TriggerCoinCollected(int value) => OnCoinCollected?.Invoke(value);
    public void TriggerPowerupActivated(PowerupType type, float duration) => OnPowerupActivated?.Invoke(type, duration);
    public void TriggerPowerupExpired(PowerupType type) => OnPowerupExpired?.Invoke(type);
    
    // ===== SCORE EVENTS =====
    
    public event Action<int> OnScoreChanged; // int = new score
    public event Action<int> OnCoinsChanged; // int = new coins
    public event Action<float> OnDistanceChanged; // float = distance
    public event Action<float> OnDistanceMilestone; // float = distance milestone
    public event Action<int> OnHealthChanged; // int = health
    
    public void TriggerScoreChanged(int score) => OnScoreChanged?.Invoke(score);
    public void TriggerCoinsChanged(int coins) => OnCoinsChanged?.Invoke(coins);
    public void TriggerDistanceChanged(float distance) => OnDistanceChanged?.Invoke(distance);
    public void TriggerDistanceMilestone(float distance) => OnDistanceMilestone?.Invoke(distance);
    public void TriggerHealthChanged(int health) => OnHealthChanged?.Invoke(health);
    
    // ===== OBSTACLE EVENTS =====
    
    public event Action<ObstacleType> OnObstacleHit; // type of obstacle
    public event Action<GameObject> OnObstacleDestroyed; // obstacle GameObject
    
    public void TriggerObstacleHit(ObstacleType type) => OnObstacleHit?.Invoke(type);
    public void TriggerObstacleDestroyed(GameObject obstacle) => OnObstacleDestroyed?.Invoke(obstacle);
    
    // ===== DIFFICULTY EVENTS =====
    
    public event Action<int> OnDifficultyTierChanged; // int = tier level
    public event Action<float> OnSpeedIncreased; // float = new speed
    
    public void TriggerDifficultyTierChanged(int tier) => OnDifficultyTierChanged?.Invoke(tier);
    public void TriggerSpeedIncreased(float speed) => OnSpeedIncreased?.Invoke(speed);
}