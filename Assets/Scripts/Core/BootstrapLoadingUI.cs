using System;
using UnityEngine;
using UnityEngine.UI;

namespace VRunner.Core
{
    /// <summary>
    /// UI hien thi tien trinh tai remote AssetBundle (CDN) + nut "Thu lai" khi loi mang, dieu khien boi
    /// BootstrapLoader trong luc InitializeAsync dang chay. Cac field UI de trong (null) van chay duoc -
    /// BootstrapLoader khong phu thuoc UI de hoat dong, chi mat phan hien thi.
    ///
    /// Setup thu cong trong Bootstrap.unity (Unity Editor): tao 1 Canvas voi Slider (progressSlider),
    /// Text/TMP hien % + thong bao loi (statusText), Button "Thu lai" (retryButton, an mac dinh) roi keo
    /// vao cac field tuong ung tren component nay, sau do keo GameObject nay vao field
    /// BootstrapLoader.loadingUI.
    /// </summary>
    public class BootstrapLoadingUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject retryButtonRoot;
        [SerializeField] private Button retryButton;

        private Action onRetryClicked;
        private string statusPrefix = "Dang tai du lieu...";

        private void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetryClicked);
            SetRetryVisible(false);
        }

        // Goi truoc moi bundle remote (AssetProvider.InitializeAsync onBundleStatus) de phan biet 2
        // truong hop: da co san trong Unity Caching (doc lai, nhanh) vs chua co (phai tai ve tu mang).
        public void ShowBundleStatus(string bundleName, bool isCached)
        {
            SetActive(true);
            SetRetryVisible(false);
            statusPrefix = isCached
                ? $"Da co san du lieu '{bundleName}' (doc tu cache)"
                : $"Chua co du lieu '{bundleName}', dang tai ve...";
            if (statusText != null) statusText.text = statusPrefix;
        }

        public void ShowProgress(float progress)
        {
            SetActive(true);
            SetRetryVisible(false);
            if (progressSlider != null) progressSlider.value = Mathf.Clamp01(progress);
            if (statusText != null) statusText.text = $"{statusPrefix} {Mathf.RoundToInt(progress * 100)}%";
        }

        public void ShowError(string message, Action onRetry)
        {
            onRetryClicked = onRetry;
            SetActive(true);
            SetRetryVisible(true);
            if (statusText != null) statusText.text = $"Khong tai duoc du lieu.\n{message}";
        }

        public void Hide()
        {
            SetActive(false);
        }

        private void HandleRetryClicked()
        {
            SetRetryVisible(false);
            onRetryClicked?.Invoke();
        }

        private void SetActive(bool active)
        {
            if (root != null) root.SetActive(active);
        }

        private void SetRetryVisible(bool visible)
        {
            if (retryButtonRoot != null) retryButtonRoot.SetActive(visible);
        }
    }
}
