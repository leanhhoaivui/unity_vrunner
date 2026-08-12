using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton quản lý tất cả object pools
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }
    
    [Header("Segment Pooling")]
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private int segmentPoolSize = 10;
    
    private ObjectPool<Segment>[] segmentPools;
    private Segment[] segmentPrefabs;
    private Transform segmentPoolParent;
    private Dictionary<int, int> prefabIdToPoolIndex = new Dictionary<int, int>();
    
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
        if (gameConfig == null || gameConfig.segments == null)
        {
            Debug.LogError("PoolManager: thiếu GameConfig / segments!");
            return;
        }
        // Gom Segment unique từ SegmentData
        List<Segment> prefabs = new List<Segment>();
        foreach (SegmentData data in gameConfig.segments)
        {
            if (data == null || data.prefab == null) continue;
            Segment seg = data.prefab.GetComponent<Segment>();
            if (seg == null)
            {
                Debug.LogError($"SegmentData '{data.name}' prefab thiếu Segment!");
                continue;
            }
            if (!prefabs.Contains(seg))
                prefabs.Add(seg);
        }
        segmentPrefabs = prefabs.ToArray();
        segmentPools = new ObjectPool<Segment>[segmentPrefabs.Length];
        prefabIdToPoolIndex.Clear();
        segmentPoolParent = new GameObject("SegmentPool").transform;
        segmentPoolParent.SetParent(transform);
        for (int i = 0; i < segmentPrefabs.Length; i++)
        {
            Segment prefab = segmentPrefabs[i];
            segmentPools[i] = new ObjectPool<Segment>(prefab, segmentPoolSize, segmentPoolParent);
            prefabIdToPoolIndex[prefab.GetInstanceID()] = i;
            Debug.Log($"Initialized pool [{i}] for {prefab.name}");
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
        
        ObjectPool<Segment> pool = segmentPools[index];
        if (pool.AvailableCount < 3)
            pool.PreWarm(5);

        Segment segment = pool.Get();
        segment.SetPoolIndex(index); // quan trọng cho Return
        return segment;
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
        if (segment == null) return;
        // Ưu tiên poolIndex đã set lúc Get
        // (cần Segment expose getter — xem mục 3)
        int poolIndex = segment.PoolIndex;
        if (poolIndex >= 0 && poolIndex < segmentPools.Length)
        {
            segmentPools[poolIndex].Return(segment);
            return;
        }
        // Fallback theo tên (kém tin cậy hơn)
        for (int i = 0; i < segmentPrefabs.Length; i++)
        {
            if (segment.name.StartsWith(segmentPrefabs[i].name))
            {
                segmentPools[i].Return(segment);
                return;
            }
        }
        Debug.LogWarning($"Could not find pool for segment: {segment.name}");
        segment.gameObject.SetActive(false);
    }

    public Segment GetSegmentByPrefab(Segment prefab)
    {
        if (prefab == null) return null;
        int id = prefab.GetInstanceID();
        if (prefabIdToPoolIndex.TryGetValue(id, out int poolIndex))
            return GetSegment(poolIndex);
        
        Debug.LogError($"No pool found for prefab: {prefab.name}. " + "Kiểm tra prefab trong SegmentData có trùng GameConfig không.");
        return null;
    }

    #endregion
    
    // #region Debug
    // private void OnGUI()
    // {
    //     if (!Application.isPlaying) return;
        
    //     GUI.Label(new Rect(10, 150, 300, 20), "=== Segment Pools ===");
        
    //     for (int i = 0; i < segmentPools.Length; i++)
    //     {
    //         string name = segmentPrefabs[i].name;
    //         int available = segmentPools[i].AvailableCount;
    //         int total = segmentPools[i].TotalCount;
            
    //         GUI.Label(new Rect(10, 170 + i * 20, 300, 20), 
    //             $"{name}: {available}/{total} available");
    //     }
    // }
    // #endregion
}