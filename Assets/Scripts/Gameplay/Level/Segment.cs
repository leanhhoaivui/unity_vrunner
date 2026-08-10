using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Đại diện cho một segment của map
/// Quản lý spawn points và segment lifecycle
/// </summary>
public class Segment : MonoBehaviour
{
    [Header("Segment Info")]
    [SerializeField] private string segmentName = "Segment";
    [SerializeField] private float segmentLength = 20f;
    
    [Header("Spawn Points")]
    [SerializeField] private Transform obstacleSpawns;
    [SerializeField] private Transform coinSpawns;
    [SerializeField] private Transform powerupSpawns;
    
    // Cached spawn point lists
    private List<Transform> obstaclePoints = new List<Transform>();
    private List<Transform> coinPoints = new List<Transform>();
    private List<Transform> powerupPoints = new List<Transform>();
    
    [Header("Pooling")]
    private int poolIndex = -1; // Track pool nào segment này thuộc về

    [Header("Obstacles")]
    [SerializeField] private Transform obstacleContainer; // Parent cho obstacles
    public Transform ObstacleContainer => obstacleContainer;

    [Header("Coins")]
    [SerializeField] private Transform coinContainer;
    [SerializeField] private Transform[] coinSpawnPoints; // Positions để spawn coins

    public Transform CoinContainer => coinContainer;
    public Transform[] CoinSpawnPoints => coinSpawnPoints;

    #region MonoBehaviour
    private void Awake()
    {
        CacheSpawnPoints();
    }

    /// <summary>
    /// Được gọi khi segment được lấy từ pool (OnEnable)
    /// </summary>
    private void OnEnable()
    {
        // Reset segment state
        ResetSegment();
    }

    /// <summary>
    /// Được gọi khi segment được trả về pool (OnDisable)
    /// </summary>
    private void OnDisable()
    {
        // Cleanup nếu cần
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Cache tất cả spawn points vào lists
    /// </summary>
    private void CacheSpawnPoints()
    {
        // Cache obstacle points
        if (obstacleSpawns != null)
        {
            foreach (Transform child in obstacleSpawns)
            {
                obstaclePoints.Add(child);
            }
        }
        
        // Cache coin points
        if (coinSpawns != null)
        {
            foreach (Transform child in coinSpawns)
            {
                coinPoints.Add(child);
            }
        }
        
        // Cache powerup points
        if (powerupSpawns != null)
        {
            foreach (Transform child in powerupSpawns)
            {
                powerupPoints.Add(child);
            }
        }
    }

    /// <summary>
    /// Reset segment về trạng thái ban đầu
    /// </summary>
    private void ResetSegment()
    {
        // Clear spawned objects (sẽ implement sau khi có obstacle/coin pooling)
        // Reset bất kỳ state nào khác
    }
    #endregion

    #region Public Getters
    public float GetLength() => segmentLength;
    
    public List<Transform> GetObstacleSpawnPoints() => obstaclePoints;
    public List<Transform> GetCoinSpawnPoints() => coinPoints;
    public List<Transform> GetPowerupSpawnPoints() => powerupPoints;
    
    public Vector3 GetEndPosition()
    {
        return transform.position + Vector3.forward * segmentLength;
    }

    /// <summary>
    /// Set pool index (gọi bởi PoolManager)
    /// </summary>
    public void SetPoolIndex(int index)
    {
        poolIndex = index;
    }

    /// <summary>
    /// Return segment về pool
    /// </summary>
    public void ReturnToPool()
    {
        PoolManager.Instance.ReturnSegment(this);
    }
    #endregion
    
    #region Debug
    private void OnDrawGizmos()
    {
        // Vẽ segment bounds
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + Vector3.forward * (segmentLength / 2f);
        Vector3 size = new Vector3(9f, 0.1f, segmentLength);
        Gizmos.DrawWireCube(center, size);
        
        // Vẽ spawn points
        DrawSpawnPointGizmos(obstacleSpawns, Color.red);
        DrawSpawnPointGizmos(coinSpawns, Color.yellow);
        DrawSpawnPointGizmos(powerupSpawns, Color.cyan);
    }
    
    private void DrawSpawnPointGizmos(Transform parent, Color color)
    {
        if (parent == null) return;
        
        Gizmos.color = color;
        foreach (Transform child in parent)
        {
            Gizmos.DrawWireSphere(child.position, 0.3f);
        }
    }
    #endregion
}