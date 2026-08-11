using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleData", menuName = "GameData/Obstacle Data")]
public class ObstacleData : ScriptableObject
{
    public string obstacleName;
    public ObstacleType type;
    public GameObject prefab;
    
    [Header("Properties")]
    public bool isLethal = true; // Nếu false, chỉ slow player
    public int damageAmount = 1;
    
    [Header("Spawn")]
    public float spawnWeight = 1f;
    public float minDistanceRequired = 0f;
}