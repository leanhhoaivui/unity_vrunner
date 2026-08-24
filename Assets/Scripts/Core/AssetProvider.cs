using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRunner.Core
{
    /// <summary>
    /// Diem load asset duy nhat cho code. Ho tro 2 nguon: Editor (mac dinh, chi dung trong Editor
    /// qua AssetDatabase de dev nhanh - khong can build bundle truoc) va AssetBundle legacy (build that,
    /// hoac AssetBundle mode trong Editor).
    /// AssetBundle lai chia lam 2 nhom: local (doc tu StreamingAssets/AssetBundles/&lt;platform&gt;, ship
    /// trong APK) va remote (tai tu CDN qua remoteBaseUrl, cache lai bang Unity Caching theo Hash128).
    /// Call site chi dung Load/LoadAll, khong can biet bundle dang o nhom nao hay nguon nao dang active.
    /// </summary>
    public static class AssetProvider
    {
        public enum SourceMode
        {
            Editor,
            AssetBundle
        }

        // Asset root cho ca 2 mode: Editor (AssetDatabase) va AssetBundle (sau khi build).
        public const string AssetRoot = "Assets/GameAssets/MainAsset/GameData";

        // Bundle local: nho, can ngay luc boot/gameplay bat dau, ship san trong StreamingAssets (APK).
        // Public vi Editor/AssetBundleBuilder.cs (assembly rieng) can danh sach nay de biet copy bundle
        // nao vao StreamingAssets vs giu lai cho CDN.
        public static readonly string[] LocalBundleNames =
        {
            "data", "prefabs_managers", "prefabs_player", "prefabs_segments", "prefabs_obstacles",
            "prefabs_collectibles", "vfx", "texture"
        };

        // Bundle remote: nang, khong can ngay khung hinh dau, tai tu CDN (remoteBaseUrl) va cache lai.
        public static readonly string[] RemoteBundleNames = { "sound", "texture_skybox" };

        private static readonly Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static AssetBundleManifest mainManifest;
        private static bool localInitialized;
        private static bool remoteInitialized;

#if UNITY_EDITOR
        private static Dictionary<string, string> editorAssetIndex;
#endif

        public static SourceMode Mode { get; private set; } = SourceMode.Editor;

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
        //
        // SourceMode.Editor chi chay trong Unity Editor; chay PlayMode khong can init gi tru building
        // asset index bang AssetDatabase.FindAssets (lazy).
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

                // Load manifest truoc de co dependency info cho cac bundle khac.
                AssetBundle manifestBundle = null;
                yield return LoadLocalBundle(bundleRoot, platform, useWebRequest, b => manifestBundle = b);
                if (manifestBundle != null)
                    mainManifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                // Load local bundles voi dependency resolution: neu bundle A co dependency tro toi bundle B,
                // phai load B truoc.
                foreach (string bundleName in LocalBundleNames)
                {
                    yield return EnsureBundleLoaded(bundleRoot, bundleName, useWebRequest, true);
                }

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

                    // Ensure dependencies (khi trai dependency khong co trong remote, co the nam o local da load).
                    if (mainManifest != null)
                    {
                        foreach (string dep in mainManifest.GetAllDependencies(bundleName))
                        {
                            // Neu dependency la remote bundle chua load, se goi LoadRemoteBundle de tai.
                            // Neu la local bundle, se da co trong loadedBundles tu tren.
                            if (Array.IndexOf(RemoteBundleNames, dep) >= 0 && !loadedBundles.ContainsKey(dep))
                            {
                                yield return LoadRemoteBundle(remoteBaseUrl, platform, dep, onBundleStatus, debugStepDelay);
                            }
                        }
                    }

                    yield return LoadRemoteBundle(remoteBaseUrl, platform, bundleName, onBundleStatus, debugStepDelay);

                    int completed = i + 1;
                    onRemoteProgress?.Invoke((float)completed / total);
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
#if UNITY_EDITOR
            if (Mode == SourceMode.Editor)
                return EditorLoad<T>(path);
#endif
            if (Mode == SourceMode.AssetBundle)
                return LoadFromBundle<T>(path);

            Debug.LogError($"AssetProvider: SourceMode.Editor khong chay duoc ngoai Unity Editor");
            return null;
        }

        public static T[] LoadAll<T>(string folder) where T : Object
        {
#if UNITY_EDITOR
            if (Mode == SourceMode.Editor)
                return EditorLoadAll<T>(folder);
#endif
            if (Mode == SourceMode.AssetBundle)
                return LoadAllFromBundle<T>(folder);

            Debug.LogError($"AssetProvider: SourceMode.Editor khong chay duoc ngoai Unity Editor");
            return new T[0];
        }

        // Đảm bảo bundle được load (để trong dictionary loadedBundles). Nếu bundle chưa load,
        // load nó từ bundleRoot (local); nếu chưa load dependency của nó, load dependency trước.
        // Để local init: sau khi load manifest, gọi đối với mỗi local bundle.
        private static IEnumerator EnsureBundleLoaded(string bundleRoot, string bundleName, bool useWebRequest, bool isLocal)
        {
            if (loadedBundles.ContainsKey(bundleName)) yield break;

            if (mainManifest != null)
            {
                foreach (string dep in mainManifest.GetAllDependencies(bundleName))
                {
                    if (!loadedBundles.ContainsKey(dep))
                        yield return EnsureBundleLoaded(bundleRoot, dep, useWebRequest, isLocal);
                }
            }

            yield return LoadLocalBundle(bundleRoot, bundleName, useWebRequest);
        }

        // Load 1 remote bundle: download qua UnityWebRequestAssetBundle, cache qua Caching.
        private static IEnumerator LoadRemoteBundle(string remoteBaseUrl, string platform, string bundleName,
            Action<string, bool> onBundleStatus = null, float debugStepDelay = 0f)
        {
            if (loadedBundles.ContainsKey(bundleName)) yield break;

            string url = $"{remoteBaseUrl}/{platform}/{bundleName}";
            Hash128 hash = mainManifest != null ? mainManifest.GetAssetBundleHash(bundleName) : default;

            bool isCached = Caching.IsVersionCached(url, hash);
            onBundleStatus?.Invoke(bundleName, isCached);
            if (debugStepDelay > 0f) yield return new WaitForSeconds(debugStepDelay);

            using UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(url, hash, 0);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"AssetProvider: khong tai duoc remote bundle '{bundleName}' tu {url} ({req.error})");
                yield break;
            }

            loadedBundles[bundleName] = DownloadHandlerAssetBundle.GetContent(req);
            if (debugStepDelay > 0f) yield return new WaitForSeconds(debugStepDelay);
        }

#if UNITY_EDITOR
        // Xây dựng asset index lần đầu (lazy): map logical path → full asset path, dùng cho EditorLoad.
        private static void BuildEditorAssetIndex()
        {
            if (editorAssetIndex != null) return;
            editorAssetIndex = new Dictionary<string, string>();

            string[] guids = UnityEditor.AssetDatabase.FindAssets(string.Empty, new[] { AssetRoot });
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (UnityEditor.AssetDatabase.IsValidFolder(assetPath)) continue;

                string logicalPath = GetLogicalPath(assetPath);
                if (logicalPath != null)
                    editorAssetIndex[logicalPath] = assetPath;
            }
        }

        // Trích logical path từ asset path đầy đủ: bỏ prefix "AssetRoot/" và extension.
        // Public vì Editor/AssetBundleBuilder.cs (assembly riêng) cần dùng.
        public static string GetLogicalPath(string assetPath)
        {
            string prefix = AssetRoot + "/";
            if (!assetPath.StartsWith(prefix)) return null;

            string relative = assetPath.Substring(prefix.Length);
            return Path.ChangeExtension(relative, null).Replace('\\', '/');
        }

        // Load 1 asset qua AssetDatabase trong Editor mode.
        private static T EditorLoad<T>(string path) where T : Object
        {
            BuildEditorAssetIndex();

            if (!editorAssetIndex.TryGetValue(path, out string assetPath))
            {
                Debug.LogError($"AssetProvider: khong tim thay asset tai {AssetRoot}/{path}");
                return null;
            }

            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
                Debug.LogError($"AssetProvider: khong load duoc asset {assetPath} (type mismatch?)");
            return asset;
        }

        // Load tất cả asset trong 1 folder (logic similar Resources.LoadAll).
        private static T[] EditorLoadAll<T>(string folder) where T : Object
        {
            BuildEditorAssetIndex();

            string prefix = folder + "/";
            var results = new List<T>();

            foreach (var kvp in editorAssetIndex)
            {
                if (kvp.Key.StartsWith(prefix))
                {
                    T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(kvp.Value);
                    if (asset != null)
                        results.Add(asset);
                }
            }

            return results.ToArray();
        }
#endif

        // Map path Resources ("Sound/Music/bg_001") sang ten bundle chua no ("sound"). Dung chung cho
        // ca runtime load va Editor/AssetBundleBuilder.cs khi gan bundle name, de 2 ben khong bao gio
        // lech nhau. Public vi Editor script nam o assembly rieng (Assembly-CSharp-Editor), internal se
        // khong thay duoc tu day. Path logic ("Texture/Skybox/...") giu nguyen bat ke file vat ly nam o
        // root nao.
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
