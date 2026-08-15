using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using VRunner.Core;
using VRunner.Gameplay;
using VRunner.Gameplay.Collectible;
using VRunner.Gameplay.Level;
using VRunner.Gameplay.Obstacles;

namespace VRunner.Gameplay.Player
{
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
            // Ignore nếu đã chết
            if (isDead) return;

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
        /// Bắt trường hợp nhảy vào volume Hole rồi đáp xuống (Enter đã bỏ qua khi airborne).
        /// </summary>
        private void OnTriggerStay(Collider other)
        {
            if (isDead || isInvincible) return;
            if (other.gameObject.layer != LayerMask.NameToLayer("ObstacleLayer"))
                return;

            Obstacle obstacle = other.GetComponentInParent<Obstacle>();
            if (obstacle == null || obstacle.Type != ObstacleType.Hole)
                return;

            HandleObstacleCollision(other);
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
                return;
            }

            Obstacle obstacle = obstacleCollider.GetComponentInParent<Obstacle>();
            if (obstacle != null && obstacle.Type == ObstacleType.Hole)
            {
                if (playerController == null || !playerController.IsGrounded)
                    return;
                if (playerController.IsFallingIntoHole)
                    return;

                StartCoroutine(FallAndDie(obstacleCollider));
                return;
            }

            Debug.Log($"Player hit obstacle: {obstacleCollider.name}");
            TakeDamage(1, obstacleCollider);
        }

        /// <summary>
        /// Rơi xuống hố rồi Game Over (không qua TakeDamage).
        /// </summary>
        private IEnumerator FallAndDie(Collider holeCollider)
        {
            Vector3 holeCenter = holeCollider.bounds.center;
            yield return playerController.FallIntoHole(holeCenter);
            Die(holeCollider);
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
                if (PowerupManager.Instance != null)
                    PowerupManager.Instance.ActivatePowerup(powerupScript.Data);
                powerupScript.Collect();
            }
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
}
