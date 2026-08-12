using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: chọn Prefab → chụp PNG icon (transparent) để dùng cho UI.
/// Menu: Tools → Prefab Icon Capture
/// </summary>
public class PrefabIconCaptureWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Art/UI/Icons";

    private GameObject prefab;
    private int resolution = 256;
    private float padding = 1.15f;
    private Color backgroundColor = new Color(0f, 0f, 0f, 0f);
    private Vector3 cameraRotation = new Vector3(20f, -30f, 0f);
    private string outputFolder = DefaultOutputFolder;
    private string fileName = "";
    private bool markAsSprite = true;
    private Texture2D lastPreview;

    [MenuItem("Tools/Images/Prefab Icon Capture")]
    public static void Open()
    {
        var window = GetWindow<PrefabIconCaptureWindow>("Prefab Icon Capture");
        window.minSize = new Vector2(360f, 480f);
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Prefab → PNG Icon", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chọn prefab (ví dụ Coin_Gold), chỉnh góc camera, rồi Capture PNG để dùng làm icon UI.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck() && prefab != null && string.IsNullOrEmpty(fileName))
        {
            fileName = prefab.name + "_Icon";
        }

        EditorGUILayout.Space(6);
        resolution = EditorGUILayout.IntSlider("Resolution", resolution, 64, 1024);
        padding = EditorGUILayout.Slider("Padding", padding, 1.0f, 2.0f);
        backgroundColor = EditorGUILayout.ColorField("Background", backgroundColor);
        cameraRotation = EditorGUILayout.Vector3Field("Camera Rotation", cameraRotation);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            if (GUILayout.Button("...", GUILayout.Width(32)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                {
                    outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }

        fileName = EditorGUILayout.TextField("File Name", fileName);
        markAsSprite = EditorGUILayout.Toggle("Import As Sprite", markAsSprite);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(prefab == null))
        {
            if (GUILayout.Button("Preview", GUILayout.Height(28)))
            {
                Capture(previewOnly: true);
            }

            if (GUILayout.Button("Capture PNG", GUILayout.Height(36)))
            {
                Capture(previewOnly: false);
            }
        }

        if (lastPreview != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            float previewSize = Mathf.Min(position.width - 40f, 256f);
            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            GUI.DrawTexture(previewRect, lastPreview, ScaleMode.ScaleToFit, true);
        }
    }

    private void Capture(bool previewOnly)
    {
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Prefab Icon Capture", "Hãy chọn một Prefab.", "OK");
            return;
        }

        Texture2D texture = RenderPrefabToTexture(prefab, resolution, padding, backgroundColor, cameraRotation);
        if (texture == null)
        {
            EditorUtility.DisplayDialog("Prefab Icon Capture", "Không render được prefab.", "OK");
            return;
        }

        DestroyPreview();
        lastPreview = texture;
        Repaint();

        if (previewOnly)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = prefab.name + "_Icon";
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            EnsureFolderExists(outputFolder);
        }

        string assetPath = Path.Combine(outputFolder, fileName + ".png").Replace('\\', '/');
        string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        AssetDatabase.Refresh();

        if (markAsSprite)
        {
            ApplySpriteImportSettings(assetPath);
        }

        Object saved = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        EditorGUIUtility.PingObject(saved);
        Selection.activeObject = saved;

        Debug.Log($"[PrefabIconCapture] Saved: {assetPath}");
    }

    private static Texture2D RenderPrefabToTexture(
        GameObject prefab,
        int size,
        float fitPadding,
        Color clearColor,
        Vector3 eulerRotation)
    {
        // Isolate render: hide existing scene objects via custom layer
        const int previewLayer = 31;

        GameObject root = null;
        Camera cam = null;
        RenderTexture rt = null;
        Light light = null;

        try
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (root == null)
            {
                root = Instantiate(prefab);
            }

            root.hideFlags = HideFlags.HideAndDontSave;
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            SetLayerRecursively(root, previewLayer);

            Bounds bounds = CalculateBounds(root);
            if (bounds.size == Vector3.zero)
            {
                bounds = new Bounds(root.transform.position, Vector3.one);
            }

            // Camera
            var camGo = new GameObject("IconCaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clearColor;
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.cullingMask = 1 << previewLayer;
            cam.allowHDR = false;
            cam.allowMSAA = true;

            float extent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            cam.orthographicSize = extent * fitPadding;

            Quaternion rotation = Quaternion.Euler(eulerRotation);
            Vector3 direction = rotation * Vector3.back;
            float distance = extent * 3f + 1f;
            cam.transform.position = bounds.center + direction * distance;
            cam.transform.LookAt(bounds.center);

            // Simple lighting
            var lightGo = new GameObject("IconCaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.cullingMask = 1 << previewLayer;

            rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            cam.targetTexture = rt;

            // Transparent clear
            var previousRT = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, clearColor);
            cam.Render();

            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            result.Apply();

            RenderTexture.active = previousRT;
            return result;
        }
        finally
        {
            if (cam != null)
            {
                cam.targetTexture = null;
                DestroyImmediate(cam.gameObject);
            }

            if (light != null)
            {
                DestroyImmediate(light.gameObject);
            }

            if (rt != null)
            {
                rt.Release();
                DestroyImmediate(rt);
            }

            if (root != null)
            {
                DestroyImmediate(root);
            }
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one * 0.5f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void EnsureFolderExists(string assetFolder)
    {
        string[] parts = assetFolder.Replace('\\', '/').Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            return;
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
            {
                continue;
            }

            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void ApplySpriteImportSettings(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private void DestroyPreview()
    {
        if (lastPreview != null)
        {
            DestroyImmediate(lastPreview);
            lastPreview = null;
        }
    }
}
