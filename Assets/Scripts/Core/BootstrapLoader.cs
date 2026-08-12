using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MenuScene";

    [Header("Core managers")]
    [SerializeField] private GameObject eventManagerPrefab;
    [SerializeField] private GameObject saveManagerPrefab;
    [SerializeField] private GameObject audioManagerPrefab;
    [SerializeField] private GameObject inputManagerPrefab;

    private void Awake()
    {
        SpawnIfNeeded(eventManagerPrefab, EventManager.Instance == null);
        SpawnIfNeeded(saveManagerPrefab, SaveManager.Instance == null);
        SpawnIfNeeded(audioManagerPrefab, AudioManager.Instance == null);
        SpawnIfNeeded(inputManagerPrefab, InputManager.Instance == null);
    }

    private void Start()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    private static void SpawnIfNeeded(GameObject prefab, bool needed)
    {
        if (!needed || prefab == null) return;
        Instantiate(prefab);
        // Awake trên prefab sẽ set Instance + DontDestroyOnLoad
    }
}