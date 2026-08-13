using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Powerup Icons")]
    [SerializeField] private GameObject magnetIcon;
    [SerializeField] private GameObject shieldIcon;
    [SerializeField] private GameObject speedIcon;
    [SerializeField] private TextMeshProUGUI magnetTimerText;
    [SerializeField] private TextMeshProUGUI shieldTimerText;
    [SerializeField] private TextMeshProUGUI speedTimerText;

    private bool eventSubscribed = false;
    
    private void Start()
    {
        // // Subscribe to events
        // if (ScoreManager.Instance != null)
        // {
        //     ScoreManager.Instance.OnScoreChanged.AddListener(UpdateScore);
        //     ScoreManager.Instance.OnCoinsChanged.AddListener(UpdateCoins);
        //     ScoreManager.Instance.OnDistanceChanged.AddListener(UpdateDistance);
        // }
        
        // if (PowerupManager.Instance != null)
        // {
        //     PowerupManager.Instance.OnPowerupActivated.AddListener(ShowPowerupIcon);
        //     PowerupManager.Instance.OnPowerupExpired.AddListener(HidePowerupIcon);
        // }


        SubscribeEvents();
        // Hide powerup icons initially
        HideAllPowerupIcons();
    }

    private void SubscribeEvents()
    {
        if (eventSubscribed || EventManager.Instance == null)
            return;
        
        EventManager.Instance.OnScoreChanged += UpdateScore;
        EventManager.Instance.OnCoinsChanged += UpdateCoins;
        EventManager.Instance.OnDistanceChanged += UpdateDistance;
        EventManager.Instance.OnHealthChanged += UpdateHealth;
        eventSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!eventSubscribed || EventManager.Instance == null)
            return;
            
        EventManager.Instance.OnScoreChanged -= UpdateScore;
        EventManager.Instance.OnCoinsChanged -= UpdateCoins;
        EventManager.Instance.OnDistanceChanged -= UpdateDistance;
        EventManager.Instance.OnHealthChanged -= UpdateHealth;
        eventSubscribed = false;
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }
    
    private void Update()
    {
        UpdatePowerupTimers();
    }
    
    private void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score:N0}";
    }
    
    private void UpdateCoins(int coins)
    {
        coinsText.text = coins.ToString();
    }
    
    private void UpdateDistance(float distance)
    {
        distanceText.text = $"{Mathf.FloorToInt(distance)}m";
    }

    private void UpdateHealth(int health)
    {
        healthText.text = health.ToString();
    }
    
    private void ShowPowerupIcon(PowerupType type, float duration)
    {
        GameObject icon = GetPowerupIcon(type);
        if (icon != null)
        {
            icon.SetActive(true);
        }
    }
    
    private void HidePowerupIcon(PowerupType type)
    {
        GameObject icon = GetPowerupIcon(type);
        if (icon != null)
        {
            icon.SetActive(false);
        }
    }
    
    private void UpdatePowerupTimers()
    {
        if (PowerupManager.Instance == null) return;
        
        UpdatePowerupTimer(PowerupType.Magnet, magnetTimerText);
        UpdatePowerupTimer(PowerupType.Shield, shieldTimerText);
        UpdatePowerupTimer(PowerupType.SpeedBoost, speedTimerText);
    }
    
    private void UpdatePowerupTimer(PowerupType type, TextMeshProUGUI timerText)
    {
        if (timerText == null) return;
        
        float remaining = PowerupManager.Instance.GetPowerupRemainingTime(type);
        if (remaining > 0)
        {
            timerText.text = $"{Mathf.CeilToInt(remaining)}s";
        }
    }
    
    private GameObject GetPowerupIcon(PowerupType type)
    {
        switch (type)
        {
            case PowerupType.Magnet: return magnetIcon;
            case PowerupType.Shield: return shieldIcon;
            case PowerupType.SpeedBoost: return speedIcon;
            default: return null;
        }
    }
    
    private void HideAllPowerupIcons()
    {
        if (magnetIcon) magnetIcon.SetActive(false);
        if (shieldIcon) shieldIcon.SetActive(false);
        if (speedIcon) speedIcon.SetActive(false);
    }
}