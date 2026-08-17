using UnityEngine;

namespace VRunner.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "GameData/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player Settings")]
        public float playerSpeed = 10f;
        public float jumpHeight = 3f;
        public float laneDistance = 2f;

        [Header("Spawn Settings")]
        public float spawnDistance = 50f;
        public float despawnDistance = 30f;
        public int initialSegments = 5;

        [Header("Difficulty")]
        public DifficultyConfig difficultyConfig;

        [Header("Segments")]
        public SegmentData[] segments;

        [Header("Powerups")]
        public PowerupData[] powerups;
    }
}
