using UnityEngine;
using TMPro;
using VRunner.Core;
using VRunner.Data;
using VRunner.Gameplay;

namespace VRunner.UI
{
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

        private bool eventSubscribed;

        private void Start()
        {
            SubscribeEvents();
            HideAllPowerupIcons();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (eventSubscribed || EventManager.Instance == null)
                return;

            EventManager.Instance.OnScoreChanged += UpdateScore;
            EventManager.Instance.OnCoinsChanged += UpdateCoins;
            EventManager.Instance.OnDistanceChanged += UpdateDistance;
            EventManager.Instance.OnHealthChanged += UpdateHealth;
            EventManager.Instance.OnPowerupActivated += ShowPowerupIcon;
            EventManager.Instance.OnPowerupExpired += HidePowerupIcon;
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
            EventManager.Instance.OnPowerupActivated -= ShowPowerupIcon;
            EventManager.Instance.OnPowerupExpired -= HidePowerupIcon;
            eventSubscribed = false;
        }

        private void Update()
        {
            // EventManager có thể Awake sau HUD
            if (!eventSubscribed)
                SubscribeEvents();

            UpdatePowerupTimers();
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score:N0}";
        }

        private void UpdateCoins(int coins)
        {
            if (coinsText != null)
                coinsText.text = coins.ToString();
        }

        private void UpdateDistance(float distance)
        {
            if (distanceText != null)
                distanceText.text = $"{Mathf.FloorToInt(distance)}m";
        }

        private void UpdateHealth(int health)
        {
            if (healthText != null)
                healthText.text = health.ToString();
        }

        private void ShowPowerupIcon(PowerupType type, float duration)
        {
            GameObject icon = GetPowerupIcon(type);
            if (icon != null)
                icon.SetActive(true);

            TextMeshProUGUI timerText = GetPowerupTimerText(type);
            if (timerText != null)
                timerText.text = $"{Mathf.CeilToInt(duration)}s";
        }

        private void HidePowerupIcon(PowerupType type)
        {
            GameObject icon = GetPowerupIcon(type);
            if (icon != null)
                icon.SetActive(false);

            TextMeshProUGUI timerText = GetPowerupTimerText(type);
            if (timerText != null)
                timerText.text = string.Empty;
        }

        private void UpdatePowerupTimers()
        {
            if (PowerupManager.Instance == null) return;

            UpdatePowerupTimer(PowerupType.Magnet, magnetIcon, magnetTimerText);
            UpdatePowerupTimer(PowerupType.Shield, shieldIcon, shieldTimerText);
            UpdatePowerupTimer(PowerupType.SpeedBoost, speedIcon, speedTimerText);
        }

        private void UpdatePowerupTimer(PowerupType type, GameObject icon, TextMeshProUGUI timerText)
        {
            float remaining = PowerupManager.Instance.GetPowerupRemainingTime(type);
            bool active = remaining > 0f;

            if (icon != null && icon.activeSelf != active)
                icon.SetActive(active);

            if (timerText == null) return;

            if (active)
                timerText.text = $"{Mathf.CeilToInt(remaining)}s";
            else if (!string.IsNullOrEmpty(timerText.text))
                timerText.text = string.Empty;
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

        private TextMeshProUGUI GetPowerupTimerText(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.Magnet: return magnetTimerText;
                case PowerupType.Shield: return shieldTimerText;
                case PowerupType.SpeedBoost: return speedTimerText;
                default: return null;
            }
        }

        private void HideAllPowerupIcons()
        {
            HidePowerupIcon(PowerupType.Magnet);
            HidePowerupIcon(PowerupType.Shield);
            HidePowerupIcon(PowerupType.SpeedBoost);
        }
    }
}
