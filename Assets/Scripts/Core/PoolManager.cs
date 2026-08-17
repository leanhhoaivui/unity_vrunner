using UnityEngine;
using System.Collections.Generic;
using VRunner.Data;
using VRunner.Gameplay.Level;

namespace VRunner.Core
{
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


        [Header("Coin Pooling")][Header("VFX Pooling")]
        [SerializeField] private PooledVFX[] vfxPrefabs;
        [SerializeField] private int vfxPoolSize = 10;

        private ObjectPool<PooledVFX>[] vfxPools;
        private Dictionary<int, int> vfxPrefabIdToPoolIndex = new Dictionary<int, int>();
        private Transform vfxPoolParent;

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
            InitializeSegmentPools();
            InitializeVFXPools();
        }

        private void InitializeSegmentPools(){
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

        private void InitializeVFXPools()
        {
            vfxPoolParent = new GameObject("VFXPool").transform;
            vfxPoolParent.SetParent(transform);
            vfxPools = new ObjectPool<PooledVFX>[vfxPrefabs.Length];
            vfxPrefabIdToPoolIndex.Clear();
            for (int i = 0; i < vfxPrefabs.Length; i++)
            {
                PooledVFX prefab = vfxPrefabs[i];
                if (prefab == null) continue;
                vfxPools[i] = new ObjectPool<PooledVFX>(prefab, vfxPoolSize, vfxPoolParent);
                vfxPrefabIdToPoolIndex[prefab.GetInstanceID()] = i;
            }
        }


        #region Segment Pooling Methods
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

        #region VFX Pooling Methods
        public PooledVFX GetVFX(PooledVFX prefab, Vector3 position)
        {
            if (prefab == null) return null;
            int id = prefab.GetInstanceID();
            if (!vfxPrefabIdToPoolIndex.TryGetValue(id, out int poolIndex))
            {
                Debug.LogError($"No VFX pool for {prefab.name}. Gán prefab vào PoolManager.vfxPrefabs.");
                return null;
            }

            ObjectPool<PooledVFX> pool = vfxPools[poolIndex];
            if (pool.AvailableCount < 2)
                pool.PreWarm(5);

            PooledVFX vfx = pool.Get();
            vfx.PoolIndex = poolIndex;
            vfx.transform.SetPositionAndRotation(position, Quaternion.identity);
            return vfx;
        }

        public void ReturnVFX(PooledVFX vfx)
        {
            if (vfx == null) return;
            int i = vfx.PoolIndex;
            if (i >= 0 && i < vfxPools.Length)
                vfxPools[i].Return(vfx);
            else
                vfx.gameObject.SetActive(false);
        }
        #endregion
    }
}
