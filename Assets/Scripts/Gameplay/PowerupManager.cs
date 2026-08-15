using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VRunner.Core;
using VRunner.Data;
using VRunner.Gameplay.Collectible;
using VRunner.Gameplay.Player;

namespace VRunner.Gameplay
{
    public class PowerupManager : MonoBehaviour
    {
        public static PowerupManager Instance;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCollision playerCollision;
        [SerializeField] private CoinMagnet coinMagnet;

        private Dictionary<PowerupType, Coroutine> activePowerups = new Dictionary<PowerupType, Coroutine>();
        private Dictionary<PowerupType, float> powerupTimers = new Dictionary<PowerupType, float>();

        [Header("Magnet")]
        [SerializeField] private float magnetRadius = 5f;
        [SerializeField] private LayerMask coinLayer;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();

            if (playerCollision == null && playerController != null)
                playerCollision = playerController.GetComponent<PlayerCollision>();

            if (coinMagnet == null && playerController != null)
                coinMagnet = playerController.GetComponent<CoinMagnet>();

            // LayerMask = 0 → OverlapSphere không hit gì; fallback CollectibleLayer
            if (coinLayer.value == 0)
            {
                int layer = LayerMask.NameToLayer("CollectibleLayer");
                if (layer >= 0)
                    coinLayer = 1 << layer;
            }

            if (coinMagnet != null)
            {
                coinMagnet.MagnetActive = false;
                if (magnetRadius > 0f)
                    coinMagnet.SetRadius(magnetRadius);
                if (coinLayer.value != 0)
                    coinMagnet.SetCoinLayer(coinLayer);
            }
        }

        public void ActivatePowerup(PowerupData data)
        {
            if (data == null) return;

            Debug.Log($"Activating powerup: {data.type}");

            if (activePowerups.ContainsKey(data.type))
            {
                StopCoroutine(activePowerups[data.type]);
                ApplyPowerupEffect(data.type, false);
            }

            Coroutine routine = StartCoroutine(PowerupDuration(data));
            activePowerups[data.type] = routine;
            powerupTimers[data.type] = data.duration;

            EventManager.Instance?.TriggerPowerupActivated(data.type, data.duration);
        }

        private IEnumerator PowerupDuration(PowerupData data)
        {
            ApplyPowerupEffect(data.type, true);

            float elapsed = 0f;
            while (elapsed < data.duration)
            {
                elapsed += Time.deltaTime;
                powerupTimers[data.type] = data.duration - elapsed;
                yield return null;
            }

            ApplyPowerupEffect(data.type, false);
            activePowerups.Remove(data.type);
            powerupTimers.Remove(data.type);

            EventManager.Instance?.TriggerPowerupExpired(data.type);
        }

        private void ApplyPowerupEffect(PowerupType type, bool activate)
        {
            switch (type)
            {
                case PowerupType.Magnet:
                    if (coinMagnet != null)
                        coinMagnet.MagnetActive = activate;
                    break;

                case PowerupType.Shield:
                    if (playerCollision != null)
                        playerCollision.IsInvincible = activate;
                    break;

                case PowerupType.SpeedBoost:
                    if (playerController != null)
                        playerController.SetSpeedMultiplier(activate ? 1.5f : 1f);
                    break;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            // Fallback hút coin nếu không có CoinMagnet component
            if (coinMagnet == null && activePowerups.ContainsKey(PowerupType.Magnet))
                PullCoinsFallback();
        }

        private void PullCoinsFallback()
        {
            if (playerController == null || coinLayer.value == 0) return;

            Collider[] coins = Physics.OverlapSphere(
                playerController.transform.position,
                magnetRadius,
                coinLayer);

            foreach (Collider coinCollider in coins)
            {
                Coin coin = coinCollider.GetComponent<Coin>();
                if (coin != null)
                    coin.EnableMagnet(playerController.transform);
            }
        }

        public float GetPowerupRemainingTime(PowerupType type)
        {
            return powerupTimers.ContainsKey(type) ? powerupTimers[type] : 0f;
        }

        public bool IsPowerupActive(PowerupType type)
        {
            return activePowerups.ContainsKey(type);
        }
    }
}
