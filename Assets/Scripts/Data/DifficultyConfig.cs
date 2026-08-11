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
    
    public DifficultyTier GetTierForDistance(float distance)
    {
        for (int i = tiers.Length - 1; i >= 0; i--)
        {
            if (distance >= tiers[i].startDistance)
                return tiers[i];
        }
        return tiers[0];
    }
}

[System.Serializable]
public class DifficultyTier
{
    public string tierName;
    public float startDistance;

    public string name;
    public float minDistance;
    public float maxDistance;
    public float speedMultiplier = 1f;
    // public float obstacleSpawnChance = 0.5f;
    
    [Header("Segment Weights")]
    [Range(0f, 1f)] public float emptyWeight = 0.5f;
    [Range(0f, 1f)] public float obstacleWeight = 0.3f;
    [Range(0f, 1f)] public float coinWeight = 0.2f;
    
    [Header("Spawn Settings")]
    public float obstacleSpawnChance = 0.7f;
    public float coinSpawnChance = 0.8f;
    
}