using UnityEngine;

namespace VRunner.Data
{
    public enum PowerupType
    {
        Magnet, // Kéo coin về phía player
        Shield, // Bảo vệ player khỏi sát thương
        SpeedBoost, // Tăng tốc độ chạy
        CoinMultiplier // Tăng số coin nhận được
    }

    [CreateAssetMenu(fileName = "PowerupData", menuName = "GameData/Powerup Data")]
    public class PowerupData : ScriptableObject
    {
        public PowerupType type;
        public string powerupName;
        public float duration;
        public Sprite icon;
        public Color iconColor;
        public GameObject prefabVisual;
    }
}
