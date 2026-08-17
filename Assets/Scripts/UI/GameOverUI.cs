using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRunner.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI distanceText;

        // RetryButton
        [SerializeField] private Button retryButton;
        // MenuButton
        [SerializeField] private Button menuButton;

        private void Start()
        {
            Hide();
        }

        private void OnEnable()
        {
            retryButton.onClick.AddListener(OnRetryButton);
            menuButton.onClick.AddListener(OnMenuButton);
        }

        private void OnDisable()
        {
            retryButton.onClick.RemoveListener(OnRetryButton);
            menuButton.onClick.RemoveListener(OnMenuButton);
        }

        public void Show(int finalScore, int highScore, int coins, float distance)
        {
            gameOverPanel.SetActive(true);

            finalScoreText.text = $"Score: {finalScore:N0}";
            highScoreText.text = $"Best: {highScore:N0}";
            coinsText.text = $"Coins: {coins}";
            distanceText.text = $"Distance: {Mathf.FloorToInt(distance)}m";

            // Pause game
            // Time.timeScale = 0f;
        }

        public void Hide()
        {
            gameOverPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OnRetryButton()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnMenuButton()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
        }
    }
}
