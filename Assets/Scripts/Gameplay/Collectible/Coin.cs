using UnityEngine;

/// <summary>
/// Coin collectible với rotation animation
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Header("Coin Settings")]
    [SerializeField] private int value = 1;
    [SerializeField] private float rotationSpeed = 90f;
    
    [Header("Magnet Settings")]
    [SerializeField] private float magnetSpeed = 10f;
    [SerializeField] private float magnetRange = 5f;
    private bool isMagnetized = false;
    private Transform playerTransform;
    
    [Header("Effects")]
    [SerializeField] private PooledVFX collectVFX;
    [SerializeField] private TrailRenderer trailRenderer;
    
    private Collider coinCollider;    
    public int Value => value;

    [Header("Float Settings")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatFrequency = 2f;
    private Vector3 startPosition;
    
    private void Awake()
    {
        coinCollider = GetComponent<Collider>();
        
        // Ensure trigger
        if (!coinCollider.isTrigger)
        {
            coinCollider.isTrigger = true;
        }

        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }

        SetupTrailDefaults();
        SetTrailEnabled(false);
    }
    
    private void OnEnable()
    {
        // Reset state khi spawn từ pool
        isMagnetized = false;
        playerTransform = null;
        // Lưu local position để bob đúng khi coin là child của Segment
        startPosition = transform.localPosition;
        SetTrailEnabled(false);
    }

    private void SetupTrailDefaults()
    {
        if (trailRenderer == null) return;

        // Chỉ set nếu chưa cấu hình trong prefab
        if (trailRenderer.time <= 0f)
        {
            trailRenderer.time = 0.35f;
        }

        if (trailRenderer.widthMultiplier <= 0f)
        {
            trailRenderer.widthMultiplier = 0.15f;
        }

        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.emitting = false;
    }

    private void SetTrailEnabled(bool enabled)
    {
        if (trailRenderer == null) return;

        trailRenderer.emitting = enabled;
        if (!enabled)
        {
            trailRenderer.Clear();
        }
    }
    
    private void Update()
    {
        // Float animation (tắt khi magnet kéo để tránh conflict)
        if (!isMagnetized)
        {
            float offset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.localPosition = startPosition + Vector3.up * offset;
        }

        // Rotate animation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        
        // Magnet pull (nếu magnet power-up active)
        if (isMagnetized && playerTransform != null)
        {
            MoveTowardsPlayer();
        }
    }
    
    /// <summary>
    /// Di chuyển về phía player (magnet effect)
    /// </summary>
    private void MoveTowardsPlayer()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > magnetRange)
        {
            // Ngoài range → dừng hút, cho float lại
            isMagnetized = false;
            playerTransform = null;
            startPosition = transform.localPosition;
            SetTrailEnabled(false);
            return;
        }

        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * magnetSpeed * Time.deltaTime;
    }
    
    /// <summary>
    /// Activate magnet pull (chỉ trong magnetRange)
    /// </summary>
    public void EnableMagnet(Transform player)
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > magnetRange) return;

        bool wasMagnetized = isMagnetized;
        isMagnetized = true;
        playerTransform = player;

        if (!wasMagnetized)
        {
            SetTrailEnabled(true);
        }
    }
    
    /// <summary>
    /// Collect coin
    /// </summary>
    public void Collect()
    {
        SetTrailEnabled(false);

        // Spawn VFX
        if (collectVFX != null && PoolManager.Instance != null)
            PoolManager.Instance.GetVFX(collectVFX, transform.position);
        
        // Return to pool
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
}