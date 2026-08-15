using UnityEngine;
using VRunner.Gameplay.Level;

namespace VRunner.Data
{
    [CreateAssetMenu(fileName = "SegmentData", menuName = "GameData/Segment Data")]
    public class SegmentData : ScriptableObject
    {
        [Header("Segment Info")]
        public string segmentName;
        public Segment prefab;

        [Header("Spawn Settings")]
        [Range(0f, 1f)]
        public float spawnWeight = 1f; // Xác suất spawn (nếu cả 3 weight đều bằng 1 thì tỉ lệ là 1:1:1 ~ 33.33%)
        public float minDistanceRequired = 0f; // Distance tối thiểu để spawn

        [Header("Difficulty")]
        public int difficultyLevel = 1; // 1=Easy, 2=Medium, 3=Hard

        [Header("Contents")]
        public int obstacleCount;
        public int coinCount;
        public bool hasPowerup;
    }
}
