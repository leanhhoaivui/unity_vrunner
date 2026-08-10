using UnityEngine;

/// <summary>
/// Obstacle types trong game
/// </summary>
public enum ObstacleType
{
    Ground,      // Obstacle trên mặt đất (phải nhảy hoặc đổi lane)
    Air,         // Obstacle trên không (phải slide hoặc đổi lane)
    Hole,        // Hố trống (phải nhảy)
    Moving       // Obstacle di chuyển trái/phải
}

/// <summary>
/// Base class cho tất cả obstacles
/// </summary>
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] private ObstacleType obstacleType;
    [SerializeField] private int damageAmount = 1; // Để mở rộng: có thể có obstacles gây damage thay vì instant death
    
    [Header("Moving Obstacle (nếu type = Moving)")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveRange = 2f; // Di chuyển trái/phải bao nhiêu units
    
    private Collider obstacleCollider;
    private Vector3 startPosition;
    private float moveTimer = 0f;
    
    public ObstacleType Type => obstacleType;
    
    private void Awake()
    {
        obstacleCollider = GetComponent<Collider>();
        
        // Đảm bảo collider là trigger
        if (!obstacleCollider.isTrigger)
        {
            Debug.LogWarning($"Obstacle {name}: Collider chưa set IsTrigger! Đang tự động set.");
            obstacleCollider.isTrigger = true;
        }
    }
    
    private void OnEnable()
    {
        // Reset khi được spawn từ pool
        startPosition = transform.localPosition;
        moveTimer = 0f;
    }
    
    private void Update()
    {
        if (isMoving)
        {
            HandleMovingObstacle();
        }
    }
    
    /// <summary>
    /// Xử lý obstacle di chuyển trái/phải
    /// </summary>
    private void HandleMovingObstacle()
    {
        moveTimer += Time.deltaTime * moveSpeed;
        
        // Dùng sin wave để di chuyển mượt
        float offset = Mathf.Sin(moveTimer) * moveRange;
        Vector3 newPos = startPosition;
        newPos.x += offset;
        transform.localPosition = newPos;
    }
    
    /// <summary>
    /// Khi player va chạm với obstacle
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check tag player
        if (other.CompareTag("Player"))
        {
            OnPlayerHit(other.gameObject);
        }
    }
    
    /// <summary>
    /// Xử lý khi player hit obstacle
    /// </summary>
    private void OnPlayerHit(GameObject player)
    {
        Debug.Log($"Player hit obstacle: {obstacleType}");
        
        // TODO: Sẽ gọi GameManager.Instance.GameOver() trong Tutorial 16
        // Tạm thời chỉ log để test
        
        // Optional: VFX, sound effect
        // PlayHitEffect();
    }
    
    /// <summary>
    /// Public method để disable obstacle (cho pooling)
    /// </summary>
    public void Disable()
    {
        gameObject.SetActive(false);
    }
}