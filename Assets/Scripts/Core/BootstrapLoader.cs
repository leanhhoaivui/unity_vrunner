using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRunner.Core
{
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "MenuScene";

        private void Awake()
        {
            SpawnIfNeeded("Prefabs/Managers/EventManager", EventManager.Instance == null);
            SpawnIfNeeded("Prefabs/Managers/SaveManager", SaveManager.Instance == null);
            SpawnIfNeeded("Prefabs/Managers/AudioManager", AudioManager.Instance == null);
            SpawnIfNeeded("Prefabs/Managers/InputManager", InputManager.Instance == null);
        }

        private void Start()
        {
            SceneManager.LoadScene(nextSceneName);
        }

        private static void SpawnIfNeeded(string resourcePath, bool needed)
        {
            if (!needed) return;
            GameObject prefab = AssetProvider.Load<GameObject>(resourcePath);
            if (prefab == null) return;
            Instantiate(prefab);
            // Awake trên prefab sẽ set Instance + DontDestroyOnLoad
        }
    }
}
