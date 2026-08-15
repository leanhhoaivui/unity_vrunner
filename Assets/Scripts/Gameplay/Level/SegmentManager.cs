using UnityEngine;
using System.Collections.Generic;
using VRunner.Core;
using VRunner.Data;
using VRunner.Gameplay;

namespace VRunner.Gameplay.Level
{
    public class SegmentManager : MonoBehaviour
    {
        [Header("Game Config")]
        [SerializeField] private GameConfig gameConfig;

        [Header("Settings")]
        [SerializeField] private int initialSegmentCount = 5;
        [SerializeField] private float spawnDistance = 50f;
        [SerializeField] private float despawnDistance = 30f;


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
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

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

        private void SpawnSegment()
        {
            // Weighted random selection
            SegmentData data = SelectSegmentByWeight();
            if (data?.prefab == null) return;

            Segment segment = PoolManager.Instance.GetSegmentByPrefab(data.prefab);
            if (segment == null) return;

            // Add to active list
            segment.transform.SetPositionAndRotation(nextSpawnPosition, Quaternion.identity);
            activeSegments.Add(segment);

            // Update distance và spawn position
            float segmentLength = segment.GetLength();
            totalDistance += segmentLength;
            nextSpawnPosition += Vector3.forward * segmentLength;

            Debug.Log($"Spawned segment at {nextSpawnPosition.z}");
        }

        private SegmentData SelectSegmentByWeight()
        {
            float totalWeight = 0f;
            foreach (SegmentData data in gameConfig.segments)
            {
                if (ScoreManager.Instance.DistanceTraveled >= data.minDistanceRequired)
                {
                    totalWeight += data.spawnWeight;
                }
            }

            float random = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (SegmentData data in gameConfig.segments)
            {
                if (ScoreManager.Instance.DistanceTraveled >= data.minDistanceRequired)
                {
                    currentWeight += data.spawnWeight;
                    if (random <= currentWeight)
                    {
                        return data;
                    }
                }
            }

            return gameConfig.segments[0]; // Fallback
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

                    // Debug.Log($"Despawned segment at {segment.transform.position.z}");
                }
            }
        }
        #endregion
    }
}
