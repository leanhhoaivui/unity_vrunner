using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Xử lý tất cả collision logic của Player
/// </summary>
public class PlayerCollision : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private bool isInvincible = false; // Shield power-up sẽ set true
    [SerializeField] private LayerMask obstacleLayer;   // Optional: dùng layer thay vì tag
    [SerializeField] private LayerMask coinLayer;       // Optional: dùng layer thay vì tag
    [SerializeField] private LayerMask powerupLayer;    // Optional: dùng layer thay vì tag
    
    [Header("Effects")]
    [SerializeField] private PooledVFX deathVFX;       // Particle effect khi chết

    // [Header("Events")]
    // public UnityEvent OnObstacleHit;     // Event khi hit obstacle
    // public UnityEvent<int> OnCoinCollect; // Event khi collect coin (int = coin value)
    // public UnityEvent<string> OnPowerupCollect; // Event khi collect powerup (string = type)
    
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;
    
    // Components
    private PlayerController playerController;
    
    // State
    private bool isDead = false;
    
    public bool IsInvincible
    {
        get => isInvincible;
        set => isInvincible = value;
    }
    
    private int totalObstacleHits = 0;
    private int totalCoinsCollected = 0;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }
    
    private void Start()
    {
        currentHealth = maxHealth;
        EventManager.Instance?.TriggerHealthChanged(currentHealth);
    }
    
    /// <summary>
    /// Detect trigger collisions
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log($"Trigger with: {other.name}, tag: {other.tag}");
        // Debug.Log($"Trigger with: {other.gameObject.layer}, layer: {other.gameObject.layer}");

        // Ignore nếu đã chết
        if (isDead) return;
        
        // Check collision type bằng tag
        // if (other.CompareTag("Obstacle"))
        // {
        //     HandleObstacleCollision(other);
        // }
        // else if (other.CompareTag("Coin"))
        // {
        //     HandleCoinCollection(other.gameObject);
        // }
        // else if (other.CompareTag("Powerup"))
        // {
        //     HandlePowerupCollection(other.gameObject);
        // }

        // Check collision type bằng layer
        int otherLayer = other.gameObject.layer;
        if (otherLayer == LayerMask.NameToLayer("ObstacleLayer"))
        {
            HandleObstacleCollision(other);
        }
        else if (otherLayer == LayerMask.NameToLayer("CollectibleLayer") && other.gameObject.CompareTag("Coin"))
        {
            HandleCoinCollection(other.gameObject);
        }
        else if (otherLayer == LayerMask.NameToLayer("CollectibleLayer") && other.gameObject.CompareTag("Powerup"))
        {
            HandlePowerupCollection(other.gameObject);
        }
        else
        {
            Debug.Log($"Player hit unknown object: {other.gameObject.name} on layer: {other.gameObject.layer} otherLayer={otherLayer}");
        }
    }
    
    /// <summary>
    /// Xử lý va chạm với obstacle
    /// </summary>
    private void HandleObstacleCollision(Collider obstacleCollider)
    {
        // Nếu có shield (invincible), bỏ qua
        if (isInvincible)
        {
            Debug.Log("Player hit obstacle but is invincible!");
            // TODO: Destroy obstacle hoặc effect khác
            return;
        }
        
        Debug.Log($"Player hit obstacle: {obstacleCollider.name}");
        
        // Trigger death
        // Die();
        TakeDamage(1, obstacleCollider);
    }
    
    /// <summary>
    /// Xử lý collect coin
    /// </summary>
    private void HandleCoinCollection(GameObject coin)
    {
        Debug.Log("Coin collected!");
        
        
        // Get coin value (default: 1)
        // int coinValue = 1;
        
        // Optional: Coin script có thể có custom value
        Coin coinScript = coin.GetComponent<Coin>();
        int coinValue = coinScript != null ? coinScript.Value : 1;
        
        // Trigger event
        // OnCoinCollect?.Invoke(coinValue);
        
        // Disable coin (return to pool)
        // coin.SetActive(false)
        
        if (coinScript != null)
            coinScript.Collect();
        else
            coin.SetActive(false);

        // Add to score
        ScoreManager.Instance.AddCoins(coinValue);
        // Play sound
        EventManager.Instance?.TriggerCoinCollected(coinValue);
    }
    
    /// <summary>
    /// Xử lý collect power-up
    /// </summary>
    private void HandlePowerupCollection(GameObject powerup)
    {
        Debug.Log("Power-up collected!");
        
        Powerup powerupScript = powerup.GetComponent<Powerup>();
        if (powerupScript != null && powerupScript.Data != null)
        {
            PowerupManager.Instance.ActivatePowerup(powerupScript.Data);
            powerupScript.Collect();
        }

        // Play sound
        // EventManager.Instance?.TriggerPowerupActivated(powerupScript.Data.Type, powerupScript.Data.Duration);
        
        // Get power-up type (sẽ implement trong Tutorial 11)
        // string powerupType = "unknown";
        
        // Optional: Powerup script
        // Powerup powerupScript = powerup.GetComponent<Powerup>();
        // if (powerupScript != null) powerupType = powerupScript.Type.ToString();
        
        // Trigger event
        // OnPowerupCollect?.Invoke(powerupType);
        
        // Disable power-up
        powerup.SetActive(false);
    }
    
    /// <summary>
    /// Xử lý player chết
    /// </summary>
    public void Die(Collider obstacleCollider)
    {
        if (isDead) return; // Prevent double death
        
        isDead = true;
        
        Debug.Log("Player died!");
        PrintCollisionStats();
        
        // Play death sound
        // Obstacle obstacle = obstacleCollider.GetComponent<Obstacle>();
        // Chỉ death sound — KHÔNG TriggerObstacleHit ở đây (tránh hit + death cùng lúc)
        EventManager.Instance?.TriggerPlayerDeath();
        
        // Spawn death VFX
        if (deathVFX != null && PoolManager.Instance != null)
            PoolManager.Instance.GetVFX(deathVFX, transform.position);
        
        // Stop player movement
        if (playerController != null)
        {
            playerController.Die(); // Dừng movement và animation.
            playerController.enabled = false; // Dừng điều khiển player.
        }
        
        // Trigger death event
        // OnObstacleHit?.Invoke();
        EventManager.Instance?.TriggerPlayerDeath();
        
        // TODO: Game Over logic sẽ implement trong Tutorial 16
        // GameManager.Instance.GameOver();
        
        // Call GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
    
    /// <summary>
    /// Reset player state (cho retry)
    /// </summary>
    public void ResetState()
    {
        isDead = false;
        isInvincible = false;
    }

    private void TakeDamage(int damage, Collider obstacleCollider)
    {
        if (isInvincible) return;
        
        currentHealth -= damage;
        EventManager.Instance?.TriggerHealthChanged(currentHealth);
        
        if (currentHealth <= 0)
        {
            Die(obstacleCollider);
        }
        else
        {
            // Flash effect, play hurt sound
            Obstacle obstacle = obstacleCollider.GetComponent<Obstacle>();
            if (obstacle != null)
                EventManager.Instance?.TriggerObstacleHit(obstacle.Type);

            StartCoroutine(InvincibilityFrames(0.5f));
        }
    }

    private IEnumerator InvincibilityFrames(float duration)
    {
        isInvincible = true;
        
        // Blink effect
        float elapsed = 0f;
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        
        while (elapsed < duration)
        {
            renderer.enabled = !renderer.enabled; // Toggle visibility
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        renderer.enabled = true;
        isInvincible = false;
    }

    public void PrintCollisionStats()
    {
        Debug.Log($"=== Collision Stats ===");
        Debug.Log($"Obstacle Hits: {totalObstacleHits}");
        Debug.Log($"Coins: {totalCoinsCollected}");
    }
}