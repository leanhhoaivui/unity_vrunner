using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRunner.Core
{
    public class BootstrapLoader : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "MenuScene";
        [SerializeField] private AssetProvider.SourceMode assetSourceMode = AssetProvider.SourceMode.Resources;
        [SerializeField] private string remoteBundleBaseUrl = "https://cdn.hoaivui.com/assetbundles";
        [SerializeField] private BootstrapLoadingUI loadingUI;

        // Chi de debug: chen 1 khoang cho ro rang sau moi buoc tai remote bundle de con nguoi kip doc UI
        // (mac dinh 0.5s vi bundle local/file:// thuong xong trong 1 frame, khong kip nhin UI). Set 0 o
        // build that de khong lam cham thoi gian boot.
        [SerializeField] private float debugStepDelaySeconds = 2f;

        private bool retryRequested;

        // IEnumerator Start() de Unity chay nhu coroutine - can thiet cho Android (AssetProvider.InitializeAsync
        // phai yield qua UnityWebRequestAssetBundle). Awake() khong the la coroutine nen moi thu doi xuong Start().
        private IEnumerator Start()
        {
            string effectiveRemoteUrl = ResolveRemoteBaseUrl();

            bool success = false;
            while (!success)
            {
                bool errorOccurred = false;
                string errorMessage = null;

                yield return AssetProvider.InitializeAsync(
                    assetSourceMode,
                    effectiveRemoteUrl,
                    progress =>
                    {
                        if (loadingUI != null) loadingUI.ShowProgress(progress);
                    },
                    message =>
                    {
                        errorOccurred = true;
                        errorMessage = message;
                    },
                    (bundleName, isCached) =>
                    {
                        if (loadingUI != null) loadingUI.ShowBundleStatus(bundleName, isCached);
                    },
                    debugStepDelaySeconds);

                if (!errorOccurred)
                {
                    success = true;
                    continue;
                }

                // Khong co UI thi khong the retry theo yeu cau nguoi dung - dung lai, log da co san trong
                // AssetProvider.
                if (loadingUI == null) yield break;

                retryRequested = false;
                loadingUI.ShowError(errorMessage, () => retryRequested = true);
                yield return new WaitUntil(() => retryRequested);
            }

            if (loadingUI != null) loadingUI.Hide();

            SpawnIfNeeded("Prefabs/Managers/EventManager", EventManager.Instance == null);
            SpawnIfNeeded("Prefabs/Managers/SaveManager", SaveManager.Instance == null);
            SpawnIfNeeded("Prefabs/Managers/AudioManager", AudioManager.Instance == null);
            SpawnIfNeeded("Prefabs/Managers/InputManager", InputManager.Instance == null);

            SceneManager.LoadScene(nextSceneName);
        }

        // Editor: neu remoteBundleBaseUrl de trong, tu fallback sang file:// tro vao thu muc staging cuc
        // bo (Build/RemoteAssetBundles/<platform>, sinh ra boi VRunner/Build Asset Bundles menu) - de dev
        // Play duoc ngay khong can dung server gia lap CDN. Build that tren device luon
        // Application.isEditor == false nen khong bao gio fallback - bat buoc phai cau hinh
        // remoteBundleBaseUrl that (mac dinh da la https://cdn.hoaivui.com/assetbundles).
        private string ResolveRemoteBaseUrl()
        {
            if (!string.IsNullOrEmpty(remoteBundleBaseUrl) || !Application.isEditor)
                return remoteBundleBaseUrl;

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string localPath = Path.Combine(projectRoot, "Build", "RemoteAssetBundles", AssetProvider.GetPlatformFolderName());
            return "file://" + localPath.Replace('\\', '/');
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
