using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton quản lý tất cả object pools
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    
    [Header("Segment Pooling")]
    [SerializeField] private Segment[] segmentPrefabs;
    [SerializeField] private int segmentPoolSize = 10;
    
    private ObjectPool<Segment>[] segmentPools;
    private Transform segmentPoolParent;
    private Dictionary<Segment, int> prefabToPoolIndex = new Dictionary<Segment, int>();
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        InitializePools();
    }
    
    private void InitializePools()
    {
        // Tạo parent object để organize hierarchy
        segmentPoolParent = new GameObject("SegmentPool").transform;
        segmentPoolParent.SetParent(transform);
        
        // Tạo pool cho mỗi segment prefab
        segmentPools = new ObjectPool<Segment>[segmentPrefabs.Length];
        
        for (int i = 0; i < segmentPrefabs.Length; i++)
        {
            if (segmentPrefabs[i] != null)
            {
                segmentPools[i] = new ObjectPool<Segment>(
                    segmentPrefabs[i],
                    segmentPoolSize,
                    segmentPoolParent
                );
                
                Debug.Log($"Initialized pool for {segmentPrefabs[i].name}");
            }
        }

        // Build lookup dictionary
        for (int i = 0; i < segmentPrefabs.Length; i++)
        {
            if (segmentPrefabs[i] != null)
            {
                prefabToPoolIndex[segmentPrefabs[i]] = i;
            }
        }
    }
    
    #region Public Methods
    /// <summary>
    /// Lấy segment từ pool (random)
    /// </summary>
    public Segment GetRandomSegment()
    {
        // int randomIndex = Random.Range(0, segmentPools.Length);
        // return segmentPools[randomIndex].Get();

        int randomIndex = Random.Range(0, segmentPools.Length);
        ObjectPool<Segment> segmentPool = segmentPools[randomIndex];
        
        if (segmentPool.AvailableCount < 3)
        {
            segmentPool.PreWarm(5);
        }
        return segmentPool.Get();
    }
    
    /// <summary>
    /// Lấy segment từ pool (specific index)
    /// </summary>
    public Segment GetSegment(int index)
    {
        if (index < 0 || index >= segmentPools.Length)
        {
            Debug.LogError($"Invalid segment index: {index}");
            return null;
        }
        
        return segmentPools[index].Get();
    }
    
    /// <summary>
    /// Trả segment về pool
    /// </summary>
    public void ReturnSegment(Segment segment, int poolIndex)
    {
        if (poolIndex < 0 || poolIndex >= segmentPools.Length)
        {
            Debug.LogError($"Invalid pool index: {poolIndex}");
            return;
        }
        
        segmentPools[poolIndex].Return(segment);
    }
    
    /// <summary>
    /// Trả segment về pool (auto-detect pool)
    /// </summary>
    public void ReturnSegment(Segment segment)
    {
        // Tìm pool phù hợp dựa trên prefab name
        for (int i = 0; i < segmentPrefabs.Length; i++)
        {
            if (segment.name.Contains(segmentPrefabs[i].name))
            {
                ReturnSegment(segment, i);
                return;
            }
        }
        
        Debug.LogWarning($"Could not find pool for segment: {segment.name}");
    }

    public Segment GetSegmentByPrefab(Segment prefab)
    {
        if (prefabToPoolIndex.TryGetValue(prefab, out int poolIndex))
        {
            return GetSegment(poolIndex);
        }
        
        Debug.LogError($"No pool found for prefab: {prefab.name}");
        return null;
    }
    #endregion
    
    #region Debug
    private void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUI.Label(new Rect(10, 150, 300, 20), "=== Segment Pools ===");
        
        for (int i = 0; i < segmentPools.Length; i++)
        {
            string name = segmentPrefabs[i].name;
            int available = segmentPools[i].AvailableCount;
            int total = segmentPools[i].TotalCount;
            
            GUI.Label(new Rect(10, 170 + i * 20, 300, 20), 
                $"{name}: {available}/{total} available");
        }
    }
    #endregion
}