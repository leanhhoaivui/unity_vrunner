using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance;
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCollision playerCollision;
    
    // [Header("Events")]
    // public UnityEvent<PowerupType, float> OnPowerupActivated;
    // public UnityEvent<PowerupType> OnPowerupExpired;
    
    private Dictionary<PowerupType, Coroutine> activePowerups = new Dictionary<PowerupType, Coroutine>();
    private Dictionary<PowerupType, float> powerupTimers = new Dictionary<PowerupType, float>();
    
    // Magnet
    [SerializeField] private float magnetRadius = 5f;
    [SerializeField] private LayerMask coinLayer;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void ActivatePowerup(PowerupData data)
    {
        Debug.Log($"Activating powerup: {data.type}");
        
        // Stop existing coroutine nếu có
        if (activePowerups.ContainsKey(data.type))
        {
            StopCoroutine(activePowerups[data.type]);
        }
        
        // Start new coroutine
        Coroutine routine = StartCoroutine(PowerupDuration(data));
        activePowerups[data.type] = routine;
        powerupTimers[data.type] = data.duration;
        
        // Trigger event
        // OnPowerupActivated?.Invoke(data.type, data.duration);
        EventManager.Instance.TriggerPowerupActivated(data.type, data.duration);
    }
    
    private IEnumerator PowerupDuration(PowerupData data)
    {
        // Apply effect
        ApplyPowerupEffect(data.type, true);
        
        // Wait duration
        float elapsed = 0f;
        while (elapsed < data.duration)
        {
            elapsed += Time.deltaTime;
            powerupTimers[data.type] = data.duration - elapsed;
            yield return null;
        }
        
        // Remove effect
        ApplyPowerupEffect(data.type, false);
        activePowerups.Remove(data.type);
        powerupTimers.Remove(data.type);
        
        // OnPowerupExpired?.Invoke(data.type);
    }
    
    private void ApplyPowerupEffect(PowerupType type, bool activate)
    {
        switch (type)
        {
            case PowerupType.Magnet:
                // Magnet được xử lý trong Update()
                break;
                
            case PowerupType.Shield:
                if (playerCollision != null)
                {
                    playerCollision.IsInvincible = activate;
                }
                break;
                
            case PowerupType.SpeedBoost:
                if (playerController != null)
                {
                    // Assume PlayerController có SetSpeedMultiplier()
                    // playerController.SetSpeedMultiplier(activate ? 2f : 1f);
                }
                break;
        }
    }
    
    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;
        
        // Handle magnet
        if (activePowerups.ContainsKey(PowerupType.Magnet))
        {
            PullCoins();
        }
    }
    
    private void PullCoins()
    {
        Collider[] coins = Physics.OverlapSphere(playerController.transform.position, magnetRadius, coinLayer);
        
        foreach (Collider coinCollider in coins)
        {
            Coin coin = coinCollider.GetComponent<Coin>();
            if (coin != null)
            {
                coin.EnableMagnet(playerController.transform);
            }
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