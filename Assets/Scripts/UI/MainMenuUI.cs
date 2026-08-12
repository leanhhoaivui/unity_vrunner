using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsCloseButton;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI totalCoinsText;
    [SerializeField] private TextMeshProUGUI gamesPlayedText;
    [SerializeField] private TextMeshProUGUI versionText;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    private bool isTransitioning;

    private void OnEnable()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButton);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButton);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.AddListener(CloseSettings);
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayButton);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitButton);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);
        if (settingsCloseButton != null)
            settingsCloseButton.onClick.RemoveListener(CloseSettings);
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        isTransitioning = false;

        if (quitButton != null)
            quitButton.gameObject.SetActive(!Application.isMobilePlatform);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
            fadePanel.interactable = false;
        }

        if (versionText != null)
            versionText.text = $"v{Application.version}";

        RefreshStats();
        LoadSettingsSliders();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMenuMusic();
    }

    private void RefreshStats()
    {
        int highScore = 0;
        int totalCoins = 0;
        int gamesPlayed = 0;

        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null)
        {
            highScore = SaveManager.Instance.GetHighScore();
            totalCoins = SaveManager.Instance.CurrentSave.totalCoins;
            gamesPlayed = SaveManager.Instance.CurrentSave.gamesPlayed;
        }

        if (highScoreText != null)
            highScoreText.text = $"Best: {highScore:N0}";

        if (totalCoinsText != null)
            totalCoinsText.text = $"Coins: {totalCoins:N0}";

        if (gamesPlayedText != null)
            gamesPlayedText.text = $"Games: {gamesPlayed:N0}";
    }

    private void LoadSettingsSliders()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null)
            return;

        var save = SaveManager.Instance.CurrentSave;

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(save.musicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(save.sfxVolume);
    }

    public void OnPlayButton()
    {
        if (isTransitioning)
            return;

        StartCoroutine(FadeAndLoadScene(gameSceneName));
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SetMusicVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SetSFXVolume(value);
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        isTransitioning = true;

        if (playButton != null)
            playButton.interactable = false;

        if (fadePanel != null)
        {
            fadePanel.blocksRaycasts = true;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            fadePanel.alpha = 1f;
        }

        SceneManager.LoadScene(sceneName);
    }
}
