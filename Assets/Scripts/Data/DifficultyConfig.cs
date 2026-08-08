using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Game/Difficulty Config")]
public class DifficultyConfig : ScriptableObject
{
    [System.Serializable]
    public class DifficultyTier
    {
        public string tierName;
        public float startDistance;
        
        [Header("Segment Weights")]
        [Range(0f, 1f)] public float emptyWeight = 0.5f;
        [Range(0f, 1f)] public float obstacleWeight = 0.3f;
        [Range(0f, 1f)] public float coinWeight = 0.2f;
        
        [Header("Spawn Settings")]
        public float obstacleSpawnChance = 0.7f;
        public float coinSpawnChance = 0.8f;
    }
    
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