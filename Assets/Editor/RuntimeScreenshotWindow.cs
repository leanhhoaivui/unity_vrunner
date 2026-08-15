using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRunner.Utilities;

/// <summary>
/// Editor tool: chụp ảnh Game View khi đang Play Mode (runtime).
/// Menu: Tools → Images → Runtime Screenshot
/// Hotkey: Ctrl/Cmd + Shift + S
/// </summary>
public class RuntimeScreenshotWindow : EditorWindow
{
    private const string DefaultOutputFolder = "Assets/Art/Screenshots";
    private const string PrefKeyFolder = "RuntimeScreenshot.OutputFolder";
    private const string PrefKeySuperSize = "RuntimeScreenshot.SuperSize";
    private const string PrefKeyIncludeTimestamp = "RuntimeScreenshot.IncludeTimestamp";
    private const string PrefKeyFilePrefix = "RuntimeScreenshot.FilePrefix";

    private string outputFolder = DefaultOutputFolder;
    private string filePrefix = "Screenshot";
    private int superSize = 1;
    private bool includeTimestamp = true;
    private bool openAfterCapture = true;
    private Texture2D lastPreview;
    private string lastSavedPath = "";
    private Vector2 scroll;
    private bool isCapturing;

    [MenuItem("Tools/Images/Runtime Screenshot")]
    public static void Open()
    {
        var window = GetWindow<RuntimeScreenshotWindow>("Runtime Screenshot");
        window.minSize = new Vector2(360f, 420f);
    }

    [MenuItem("Tools/Images/Capture Runtime Screenshot %#s")] // Ctrl/Cmd + Shift + S
    public static void CaptureFromMenu()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Runtime Screenshot",
                "Chỉ chụp được khi đang Play Mode (runtime).",
                "OK");
            return;
        }

        var window = GetWindow<RuntimeScreenshotWindow>("Runtime Screenshot");
        window.LoadPrefs();
        window.Capture();
    }

    private void OnEnable()
    {
        LoadPrefs();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SavePrefs();
        DestroyPreview();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            isCapturing = false;
            DestroyPreview();
            Repaint();
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Play Mode → PNG Screenshot", EditorStyles.boldLabel);

        bool isPlaying = EditorApplication.isPlaying;
        string help = !isPlaying
            ? "Chưa Play Mode. Bấm Play rồi mới chụp được."
            : EditorApplication.isPaused
                ? "Editor đang Pause — vẫn chụp được (tool tạm unpause 1 frame rồi pause lại)."
                : "Đang Play Mode. Tool focus Game View rồi chụp cuối frame (không lấy Scene View).";
        EditorGUILayout.HelpBox(help, isPlaying ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            if (GUILayout.Button("...", GUILayout.Width(32)))
            {
                string selected = EditorUtility.OpenFolderPanel(
                    "Select Output Folder",
                    Application.dataPath,
                    "");
                if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                {
                    outputFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }

        filePrefix = EditorGUILayout.TextField("File Prefix", filePrefix);
        includeTimestamp = EditorGUILayout.Toggle("Include Timestamp", includeTimestamp);
        superSize = EditorGUILayout.IntSlider("Super Size", superSize, 1, 4);
        openAfterCapture = EditorGUILayout.Toggle("Ping After Capture", openAfterCapture);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            $"Hotkey: Ctrl/Cmd + Shift + S  |  Resolution ×{superSize}",
            EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!isPlaying || isCapturing))
        {
            string label = isCapturing ? "Capturing..." : "Capture Screenshot";
            if (GUILayout.Button(label, GUILayout.Height(40)))
            {
                Capture();
            }
        }

        if (!string.IsNullOrEmpty(lastSavedPath))
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Last Saved", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(lastSavedPath, GUILayout.Height(18));
        }

        if (lastPreview != null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            float maxWidth = position.width - 40f;
            float aspect = (float)lastPreview.width / Mathf.Max(1, lastPreview.height);
            float previewHeight = Mathf.Min(280f, maxWidth / aspect);
            float previewWidth = previewHeight * aspect;

            Rect previewRect = GUILayoutUtility.GetRect(
                previewWidth,
                previewHeight,
                GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            GUI.DrawTexture(previewRect, lastPreview, ScaleMode.ScaleToFit, false);

            EditorGUILayout.LabelField(
                $"{lastPreview.width} × {lastPreview.height}",
                EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void Capture()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Runtime Screenshot",
                "Chỉ chụp được khi đang Play Mode (runtime).",
                "OK");
            return;
        }

        if (isCapturing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(filePrefix))
        {
            filePrefix = "Screenshot";
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            EnsureFolderExists(outputFolder);
        }

        string fileName = BuildFileName();
        string assetPath = Path.Combine(outputFolder, fileName).Replace('\\', '/');
        string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        // Focus Game View trước — nếu focus Scene/Editor Window thì ScreenCapture dễ lấy sai buffer
        FocusGameView();

        // Editor Pause dừng player loop → WaitForEndOfFrame không chạy.
        // Tạm unpause 1 frame để chụp, rồi restore lại.
        bool wasEditorPaused = EditorApplication.isPaused;
        if (wasEditorPaused)
        {
            EditorApplication.isPaused = false;
        }

        isCapturing = true;
        Repaint();

        int size = superSize;
        RuntimeScreenshotHelper.Capture(size, texture =>
        {
            isCapturing = false;

            if (wasEditorPaused && EditorApplication.isPlaying)
            {
                EditorApplication.isPaused = true;
            }

            if (texture == null)
            {
                EditorUtility.DisplayDialog(
                    "Runtime Screenshot",
                    "Không capture được Game View. Hãy mở tab Game và thử lại.",
                    "OK");
                Repaint();
                return;
            }

            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());

            DestroyPreview();
            lastPreview = texture;
            lastSavedPath = assetPath;

            AssetDatabase.Refresh();

            if (openAfterCapture)
            {
                UnityEngine.Object saved = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (saved != null)
                {
                    EditorGUIUtility.PingObject(saved);
                    Selection.activeObject = saved;
                }
            }

            SavePrefs();
            Repaint();
            Debug.Log($"[RuntimeScreenshot] Saved Game View: {assetPath} ({texture.width}x{texture.height})");
        });
    }

    private static void FocusGameView()
    {
        // Ưu tiên mở/focus đúng cửa sổ Game View
        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
        {
            EditorWindow gameView = GetWindow(gameViewType, false, null, true);
            if (gameView != null)
            {
                gameView.Show();
                gameView.Focus();
                return;
            }
        }

        // Fallback menu path (Unity version khác nhau)
        if (!EditorApplication.ExecuteMenuItem("Window/General/Game"))
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
        }
    }

    private string BuildFileName()
    {
        if (includeTimestamp)
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{filePrefix}_{stamp}.png";
        }

        string baseName = filePrefix;
        string candidate = baseName + ".png";
        string absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder, candidate));
        int index = 1;
        while (File.Exists(absolute))
        {
            candidate = $"{baseName}_{index}.png";
            absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder, candidate));
            index++;
        }

        return candidate;
    }

    private void LoadPrefs()
    {
        outputFolder = EditorPrefs.GetString(PrefKeyFolder, DefaultOutputFolder);
        filePrefix = EditorPrefs.GetString(PrefKeyFilePrefix, "Screenshot");
        superSize = EditorPrefs.GetInt(PrefKeySuperSize, 1);
        includeTimestamp = EditorPrefs.GetBool(PrefKeyIncludeTimestamp, true);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(PrefKeyFolder, outputFolder);
        EditorPrefs.SetString(PrefKeyFilePrefix, filePrefix);
        EditorPrefs.SetInt(PrefKeySuperSize, superSize);
        EditorPrefs.SetBool(PrefKeyIncludeTimestamp, includeTimestamp);
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

    private void DestroyPreview()
    {
        if (lastPreview != null)
        {
            DestroyImmediate(lastPreview);
            lastPreview = null;
        }
    }
}
