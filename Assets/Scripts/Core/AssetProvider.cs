using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace VRunner.Core
{
    /// <summary>
    /// Diem load asset duy nhat cho code. Ho tro 2 nguon: Resources (mac dinh) va AssetBundle legacy.
    /// AssetBundle lai chia lam 2 nhom: local (doc tu StreamingAssets/AssetBundles/&lt;platform&gt;, ship
    /// trong APK) va remote (tai tu CDN qua remoteBaseUrl, cache lai bang Unity Caching theo Hash128).
    /// Call site chi dung Load/LoadAll, khong can biet bundle dang o nhom nao hay nguon nao dang active.
    /// </summary>
    public static class AssetProvider
    {
        public enum SourceMode
        {
            Resources,
            AssetBundle
        }

        // Bundle local: nho, can ngay luc boot/gameplay bat dau, ship san trong StreamingAssets (APK).
        // Public vi Editor/AssetBundleBuilder.cs (assembly rieng) can danh sach nay de biet copy bundle
        // nao vao StreamingAssets vs giu lai cho CDN.
        public static readonly string[] LocalBundleNames =
        {
            "data", "prefabs_managers", "prefabs_player", "prefabs_segments", "prefabs_obstacles",
            "prefabs_collectibles", "vfx", "texture"
        };

        // Bundle remote: nang, khong can ngay khung hinh dau, tai tu CDN (remoteBaseUrl) va cache lai.
        // Asset thuoc nhom nay phai nam ngoai bat ky thu muc ten "Resources/" nao (xem AssetBundleBuilder
        // RemoteSourceRoot) - neu khong Unity van tu nhung vao build chinh bat ke co gan bundle name hay
        // khong, lam mat tac dung tach CDN.
        public static readonly string[] RemoteBundleNames = { "sound", "texture_skybox" };

        private static readonly Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static AssetBundleManifest mainManifest;
        private static bool localInitialized;
        private static bool remoteInitialized;

        public static SourceMode Mode { get; private set; } = SourceMode.Resources;

        // Android: StreamingAssets nam trong APK nen, khong doc duoc bang File/AssetBundle.LoadFromFile,
        // phai dung UnityWebRequestAssetBundle (bat dong bo). Cac nen tang khac (Editor/Standalone/iOS)
        // doc truc tiep bang duong dan file nhu cu.
        //
        // Goi lai an toan: local chi init 1 lan (localInitialized), remote co the goi lai nhieu lan de
        // retry khi loi mang - cac bundle da tai thanh cong o lan truoc duoc Unity Caching giu lai nen
        // khong tai lai tu mang, chi bundle loi moi thuc su tai lai.
        //
        // onBundleStatus(bundleName, isCached): bao truoc moi bundle remote co san trong Unity Caching
        // (Caching.IsVersionCached) hay chua, de UI phan biet ro "da co san, doc cache" vs "chua co, dang
        // tai ve". debugStepDelay > 0 chen 1 khoang cho ro rang sau moi buoc (chi de debug/quan sat UI -
        // truyen 0 hoac bo qua o build that de khong lam cham thoi gian boot).
        public static IEnumerator InitializeAsync(SourceMode mode, string remoteBaseUrl = null,
            Action<float> onRemoteProgress = null, Action<string> onError = null,
            Action<string, bool> onBundleStatus = null, float debugStepDelay = 0f)
        {
            Mode = mode;
            if (Mode != SourceMode.AssetBundle) yield break;

            string platform = GetPlatformFolderName();
            bool useWebRequest = Application.platform == RuntimePlatform.Android;

            if (!localInitialized)
            {
                string bundleRoot = Path.Combine(Application.streamingAssetsPath, "AssetBundles", platform);

                foreach (string bundleName in LocalBundleNames)
                {
                    yield return LoadLocalBundle(bundleRoot, bundleName, useWebRequest);
                }

                // File manifest chinh (ten dung platform, vd "Android") ship local - nho, chi chua
                // hash/dependency, dung de lay Hash128 cho remote bundle.
                AssetBundle manifestBundle = null;
                yield return LoadLocalBundle(bundleRoot, platform, useWebRequest, b => manifestBundle = b);
                if (manifestBundle != null)
                    mainManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                localInitialized = true;
            }

            if (!remoteInitialized)
            {
                if (string.IsNullOrEmpty(remoteBaseUrl))
                {
                    Debug.LogError("AssetProvider: remoteBaseUrl trong, khong the tai remote bundle");
                    onError?.Invoke("remoteBaseUrl trong");
                    yield break;
                }

                int total = RemoteBundleNames.Length;
                for (int i = 0; i < total; i++)
                {
                    string bundleName = RemoteBundleNames[i];
                    string url = $"{remoteBaseUrl}/{platform}/{bundleName}";
                    Hash128 hash = mainManifest != null ? mainManifest.GetAssetBundleHash(bundleName) : default;

                    bool isCached = Caching.IsVersionCached(url, hash);
                    onBundleStatus?.Invoke(bundleName, isCached);
                    if (debugStepDelay > 0f) yield return new WaitForSeconds(debugStepDelay);

                    using UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(url, hash, 0);
                    UnityWebRequestAsyncOperation op = req.SendWebRequest();
                    int completed = i;
                    while (!op.isDone)
                    {
                        onRemoteProgress?.Invoke((completed + req.downloadProgress) / total);
                        yield return null;
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        string message = $"AssetProvider: khong tai duoc remote bundle '{bundleName}' tu {url} ({req.error})";
                        Debug.LogError(message);
                        onError?.Invoke(message);
                        yield break;
                    }

                    loadedBundles[bundleName] = DownloadHandlerAssetBundle.GetContent(req);
                    onRemoteProgress?.Invoke((float)(i + 1) / total);
                    if (debugStepDelay > 0f) yield return new WaitForSeconds(debugStepDelay);
                }

                remoteInitialized = true;
            }
        }

        private static IEnumerator LoadLocalBundle(string bundleRoot, string bundleName, bool useWebRequest, Action<AssetBundle> onLoaded = null)
        {
            string bundlePath = Path.Combine(bundleRoot, bundleName);
            AssetBundle bundle;
            Debug.Log($"[DIAG] AssetProvider.LoadLocalBundle() tải bundle '{bundleName}' từ {bundlePath}");
            if (useWebRequest)
            {
                using UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath);
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"AssetProvider: khong load duoc AssetBundle tai {bundlePath} ({req.error})");
                    yield break;
                }
                bundle = DownloadHandlerAssetBundle.GetContent(req);
            }
            else
            {
                bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            if (bundle == null)
            {
                Debug.LogError($"AssetProvider: khong load duoc AssetBundle tai {bundlePath}");
                yield break;
            }

            if (Array.IndexOf(LocalBundleNames, bundleName) >= 0)
                loadedBundles[bundleName] = bundle;
            onLoaded?.Invoke(bundle);
        }

        public static T Load<T>(string path) where T : Object
        {
            if (Mode == SourceMode.AssetBundle)
                return LoadFromBundle<T>(path);

            T asset = Resources.Load<T>(path);
            if (asset == null)
                Debug.LogError($"AssetProvider: khong tim thay asset tai Resources/{path}");
            return asset;
        }

        public static T[] LoadAll<T>(string folder) where T : Object
        {
            if (Mode == SourceMode.AssetBundle)
                return LoadAllFromBundle<T>(folder);

            return Resources.LoadAll<T>(folder);
        }

        // Map path Resources ("Sound/Music/bg_001") sang ten bundle chua no ("sound"). Dung chung cho
        // ca runtime load va Editor/AssetBundleBuilder.cs khi gan bundle name, de 2 ben khong bao gio
        // lech nhau. Public vi Editor script nam o assembly rieng (Assembly-CSharp-Editor), internal se
        // khong thay duoc tu day. Path logic ("Texture/Skybox/...") giu nguyen bat ke file vat ly nam o
        // Resources/ hay RemoteSource/ - xem AssetBundleBuilder.GetResourcesRelativePath.
        public static string GetBundleNameForPath(string resourcePath)
        {
            string[] segments = resourcePath.Split('/');
            switch (segments[0])
            {
                case "Data": return "data";
                case "VFX": return "vfx";
                case "Sound": return "sound";
                case "Texture":
                    if (segments.Length > 1 && segments[1] == "Skybox") return "texture_skybox";
                    return "texture";
                case "Prefabs":
                    if (segments.Length > 1 && segments[1] == "Managers") return "prefabs_managers";
                    if (segments.Length > 1 && segments[1] == "Player") return "prefabs_player";
                    if (segments.Length > 1 && segments[1] == "Segments") return "prefabs_segments";
                    if (segments.Length > 1 && segments[1] == "Obstacles") return "prefabs_obstacles";
                    if (segments.Length > 1 && segments[1] == "Collectibles") return "prefabs_collectibles";
                    break;
            }

            Debug.LogError($"AssetProvider: khong xac dinh duoc bundle cho path '{resourcePath}'");
            return null;
        }

        // Dung chung boi InitializeAsync() (runtime, ke ca trong Play Mode o Editor) va
        // Editor/AssetBundleBuilder.cs (build-time) de folder output va folder doc luon khop nhau.
        // Ho tro Standalone/Editor/Android/iOS. Trong Editor, Application.platform khong bao gio la
        // Android/iOS du activeBuildTarget dang chon nen tang do - nen Play Mode van doc bang
        // AssetBundle.LoadFromFile dong bo (xem InitializeAsync), chi thiet bi that moi dung
        // UnityWebRequestAssetBundle.
        public static string GetPlatformFolderName()
        {
#if UNITY_EDITOR
            switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
            {
                case UnityEditor.BuildTarget.StandaloneWindows:
                case UnityEditor.BuildTarget.StandaloneWindows64:
                    return "StandaloneWindows64";
                case UnityEditor.BuildTarget.StandaloneLinux64:
                    return "StandaloneLinux64";
                case UnityEditor.BuildTarget.Android:
                    return "Android";
                case UnityEditor.BuildTarget.iOS:
                    return "iOS";
                default:
                    return "StandaloneOSX";
            }
#else
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return "StandaloneWindows64";
                case RuntimePlatform.LinuxPlayer:
                    return "StandaloneLinux64";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                default:
                    return "StandaloneOSX";
            }
#endif
        }

        private static T LoadFromBundle<T>(string path) where T : Object
        {
            AssetBundle bundle = GetBundle(GetBundleNameForPath(path));
            if (bundle == null) return null;

            T asset = bundle.LoadAsset<T>(Path.GetFileName(path));
            if (asset == null)
                Debug.LogError($"AssetProvider: khong tim thay asset '{path}' trong AssetBundle");
            return asset;
        }

        private static T[] LoadAllFromBundle<T>(string folder) where T : Object
        {
            AssetBundle bundle = GetBundle(GetBundleNameForPath(folder));
            return bundle == null ? new T[0] : bundle.LoadAllAssets<T>();
        }

        private static AssetBundle GetBundle(string bundleName)
        {
            if (bundleName == null) return null;
            if (loadedBundles.TryGetValue(bundleName, out AssetBundle bundle)) return bundle;
            Debug.LogError($"AssetProvider: AssetBundle '{bundleName}' chua duoc load (InitializeAsync chua goi hoac load loi)");
            return null;
        }
    }
}
