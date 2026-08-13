using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "GameData/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [Header("Speed Settings")]
    public float baseSpeed = 10f;
    public float maxSpeed = 25f;
    public float speedIncreaseRate = 0.5f;
    public float speedIncreaseInterval = 100f;
    
    [Header("Difficulty Tiers")]
    public DifficultyTier[] tiers;

}

[System.Serializable]
public class DifficultyTier
{
    public string name;
    public float minDistance;
    public float maxDistance;
    public float speedMultiplier = 1f;
    // public float obstacleSpawnChance = 0.5f;
    
    [Header("Segment Weights (Not used)")]
    [Range(0f, 1f)] public float emptyWeight = 0.5f;
    [Range(0f, 1f)] public float obstacleWeight = 0.3f;
    [Range(0f, 1f)] public float coinWeight = 0.2f;
    
    [Header("Spawn Settings (Not used)")]
    public float obstacleSpawnChance = 0.7f;
    public float coinSpawnChance = 0.8f;
    
}