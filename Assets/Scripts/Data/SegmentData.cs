using UnityEngine;

[CreateAssetMenu(fileName = "SegmentData", menuName = "GameData/Segment Data")]
public class SegmentData : ScriptableObject
{
    [Header("Segment Info")]
    public string segmentName;
    public GameObject prefab;
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float spawnWeight = 1f; // Xác suất spawn
    public float minDistanceRequired = 0f; // Distance tối thiểu để spawn
    
    [Header("Difficulty")]
    public int difficultyLevel = 1; // 1=Easy, 2=Medium, 3=Hard
    
    [Header("Contents")]
    public int obstacleCount;
    public int coinCount;
    public bool hasPowerup;
}