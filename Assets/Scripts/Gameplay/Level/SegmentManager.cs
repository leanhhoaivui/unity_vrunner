using UnityEngine;
using System.Collections.Generic;

public class SegmentManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int initialSegmentCount = 5;
    [SerializeField] private float spawnDistance = 50f;
    [SerializeField] private float despawnDistance = 30f;
    

    [Header("Difficulty")]  [Header("Difficulty")]
    [SerializeField] private DifficultyConfig difficultyConfig;
    
    [Header("Segment Types")]
    [SerializeField] private Segment emptySegmentPrefab;
    [SerializeField] private Segment obstacleSegmentPrefab;
    [SerializeField] private Segment coinSegmentPrefab;
    
    [Header("Validation")]
    [SerializeField] private int maxConsecutiveObstacles = 3;
    [SerializeField] private int minEmptyBetweenObstacles = 1;

    [Header("Performance")]
    [SerializeField] private int maxSpawnsPerFrame = 2;

    private List<Segment> activeSegments = new List<Segment>();
    private Transform playerTransform;
    private Vector3 nextSpawnPosition = Vector3.zero;
    private float totalDistance = 0f;
    private List<string> recentSegmentTypes = new List<string>();
    
    #region MonoBehaviour
    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (playerTransform == null)
        {
            Debug.LogError("Player not found!");
            return;
        }
        
        SpawnInitialSegments();
    }
    
    private void Update()
    {
        if (playerTransform == null) return;
        
        // Spawn (với limit)
        int spawnsThisFrame = 0;
        float distanceToNext = nextSpawnPosition.z - playerTransform.position.z;
        
        while (distanceToNext < spawnDistance && spawnsThisFrame < maxSpawnsPerFrame)
        {
            SpawnSegment();
            spawnsThisFrame++;
            distanceToNext = nextSpawnPosition.z - playerTransform.position.z;
        }
        
        // Check despawn condition
        DespawnOldSegments();
    }
    #endregion

    #region Private Methods
    private void SpawnInitialSegments()
    {
        Debug.Log("SpawnInitialSegments");
        for (int i = 0; i < initialSegmentCount; i++)
        {
            SpawnSegment();
        }
    }

    /// <summary>
    /// Chọn segment dựa trên weighted random và difficulty
    /// </summary>
    private Segment SelectSegmentPrefab()
    {
        Segment selectedPrefab;
        int attempts = 0;
        
        do
        {
            selectedPrefab = SelectSegmentPrefab_Internal();
            attempts++;
            
            if (attempts > 10)
            {
                // Fallback: force empty segment
                return emptySegmentPrefab;
            }
        }
        while (!IsValidSegment(selectedPrefab));
        
        // Track segment type
        recentSegmentTypes.Add(selectedPrefab.name);
        if (recentSegmentTypes.Count > 10)
        {
            recentSegmentTypes.RemoveAt(0);
        }
        
        return selectedPrefab;
    }
    
    private Segment SelectSegmentPrefab_Internal()
    {
        // Logic từ bước 2
        var tier = difficultyConfig.GetTierForDistance(totalDistance);
        float totalWeight = tier.emptyWeight + tier.obstacleWeight + tier.coinWeight;
        float randomValue = Random.Range(0f, totalWeight);
        
        if (randomValue < tier.emptyWeight)
            return emptySegmentPrefab;
        else if (randomValue < tier.emptyWeight + tier.obstacleWeight)
            return obstacleSegmentPrefab;
        else
            return coinSegmentPrefab;
    }
    
    private bool IsValidSegment(Segment prefab)
    {
        // Rule 1: Không quá nhiều obstacle liên tiếp
        if (prefab == obstacleSegmentPrefab)
        {
            int consecutiveObstacles = 0;
            for (int i = recentSegmentTypes.Count - 1; i >= 0; i--)
            {
                if (recentSegmentTypes[i].Contains("Obstacle"))
                    consecutiveObstacles++;
                else
                    break;
            }
            
            if (consecutiveObstacles >= maxConsecutiveObstacles)
                return false;
        }
        
        // Rule 2: Cần empty segment giữa obstacles (optional)
        // ... implement thêm rules nếu cần
        
        return true;
    }
    
    private void SpawnSegment()
    {
        // Select prefab dựa trên difficulty
        Segment prefab = SelectSegmentPrefab();
        
        // // Get segment từ pool (thay vì Instantiate)
        // Segment segment = PoolManager.Instance.GetRandomSegment();

        // Get từ pool (cần update PoolManager để support specific prefab)
        Segment segment = PoolManager.Instance.GetSegmentByPrefab(prefab);
        
        if (segment == null)
        {
            Debug.LogError("Failed to get segment from pool!");
            return;
        }
        
        // Setup position
        segment.transform.position = nextSpawnPosition;
        segment.transform.rotation = Quaternion.identity;
        
        // Add to active list
        activeSegments.Add(segment);
        
        // Update next spawn position
        // nextSpawnPosition += Vector3.forward * segment.GetLength();

        // Update distance và spawn position
        float segmentLength = segment.GetLength();
        totalDistance += segmentLength;
        nextSpawnPosition += Vector3.forward * segmentLength;
        
        Debug.Log($"Spawned segment at {nextSpawnPosition.z}");
    }
    
    private void DespawnOldSegments()
    {
        // Check segments phía sau player
        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            Segment segment = activeSegments[i];
            float distanceBehindPlayer = playerTransform.position.z - segment.transform.position.z;
            
            if (distanceBehindPlayer > despawnDistance)
            {
                // Return về pool (thay vì Destroy)
                segment.ReturnToPool();
                activeSegments.RemoveAt(i);
                
                Debug.Log($"Despawned segment at {segment.transform.position.z}");
            }
        }
    }
    #endregion
}