using UnityEngine;
using System.Collections.Generic;

public class SegmentManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int initialSegmentCount = 5;
    [SerializeField] private float spawnDistance = 50f;
    [SerializeField] private float despawnDistance = 30f;
    
    private List<Segment> activeSegments = new List<Segment>();
    private Transform playerTransform;
    private Vector3 nextSpawnPosition = Vector3.zero;
    
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
        
        // Check spawn condition
        float distanceToSpawnPoint = (nextSpawnPosition - playerTransform.position).z;
        if (distanceToSpawnPoint < spawnDistance)
        {
            SpawnSegment();
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
    
    private void SpawnSegment()
    {
        // Get segment từ pool (thay vì Instantiate)
        Segment segment = PoolManager.Instance.GetRandomSegment();
        
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
        nextSpawnPosition += Vector3.forward * segment.GetLength();
        
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