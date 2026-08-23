using UnityEngine;
using VRunner.Core;

namespace VRunner.Gameplay.Player
{
    // Chay truoc GameManager.Awake (dang FindObjectOfType<PlayerController> luc Awake)
    [DefaultExecutionOrder(-100)]
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;

        private void Awake()
        {
            if (spawnPoint == null)
                spawnPoint = transform;

            GameObject prefab = AssetProvider.Load<GameObject>("Prefabs/Player/player_001");
            if (prefab == null)
                return;

            Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
        }
    }
}
