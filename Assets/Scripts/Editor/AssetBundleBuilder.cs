using System.IO;
using UnityEditor;
using UnityEngine;
using VRunner.Core;

namespace VRunner.Editor
{
    /// <summary>
    /// Gan bundle name cho asset duoi GameData/ (theo AssetProvider.GetBundleNameForPath)
    /// roi build AssetBundle legacy vao 1 thu muc staging. Sau do tach ket qua: bundle thuoc
    /// AssetProvider.LocalBundleNames (+ manifest chinh) copy vao StreamingAssets/AssetBundles/&lt;platform&gt;
    /// (ship trong APK); bundle thuoc AssetProvider.RemoteBundleNames copy vao
    /// Build/RemoteAssetBundles/&lt;platform&gt; (khong ship trong APK - tu upload len CDN, xem
    /// docs/asset-bundle.md). Chay lai menu nay moi khi them/xoa/di chuyen file duoi GameData/.
    /// </summary>
    public static class AssetBundleBuilder
    {
        private const string StagingRoot = "Build/AssetBundlesStaging";
        private const string RemoteOutputRoot = "Build/RemoteAssetBundles";

        [MenuItem("VRunner/Build Asset Bundles (Active Platform)")]
        public static void BuildBundles()
        {
            AssignBundleNames(AssetProvider.AssetRoot);

            string platform = AssetProvider.GetPlatformFolderName();
            string stagingPath = Path.Combine(StagingRoot, platform);
            Directory.CreateDirectory(stagingPath);

            // ChunkBasedCompression (LZ4) thay vi LZMA mac dinh: doc tu StreamingAssets tren mobile
            // (UnityWebRequestAssetBundle tren Android, file truc tiep tren iOS) khong can giai nen
            // toan bo bundle truoc khi dung duoc 1 asset ben trong.
            BuildPipeline.BuildAssetBundles(stagingPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

            string streamingPath = Path.Combine("Assets/StreamingAssets/AssetBundles", platform);
            Directory.CreateDirectory(streamingPath);
            string remoteOutputPath = Path.Combine(RemoteOutputRoot, platform);
            Directory.CreateDirectory(remoteOutputPath);

            // File manifest chinh (ten dung platform, vd "Android") sinh ra o goc staging - ship local
            // vi remote bundle can no de lay Hash128 phuc vu caching.
            CopyBundleFiles(stagingPath, streamingPath, platform);
            foreach (string bundleName in AssetProvider.LocalBundleNames)
                CopyBundleFiles(stagingPath, streamingPath, bundleName);
            foreach (string bundleName in AssetProvider.RemoteBundleNames)
                CopyBundleFiles(stagingPath, remoteOutputPath, bundleName);

            AssetDatabase.Refresh();

            Debug.Log($"AssetBundleBuilder: build xong. Local bundle da copy vao {streamingPath} (ship trong APK). " +
                      $"Remote bundle ({string.Join(", ", AssetProvider.RemoteBundleNames)}) nam o {remoteOutputPath} - " +
                      $"tu upload len CDN theo huong dan trong docs/asset-bundle.md.");
        }

        private static void CopyBundleFiles(string sourceDir, string destDir, string bundleName)
        {
            foreach (string suffix in new[] { "", ".manifest" })
            {
                string sourceFile = Path.Combine(sourceDir, bundleName + suffix);
                if (!File.Exists(sourceFile)) continue;
                File.Copy(sourceFile, Path.Combine(destDir, bundleName + suffix), true);
            }
        }

        private static void AssignBundleNames(string root)
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { root });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(assetPath)) continue;

                string logicalPath = AssetProvider.GetLogicalPath(assetPath);
                if (logicalPath == null) continue;

                string bundleName = AssetProvider.GetBundleNameForPath(logicalPath);
                if (bundleName == null) continue;

                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null) continue;

                importer.SetAssetBundleNameAndVariant(bundleName, string.Empty);
            }
        }
    }
}
