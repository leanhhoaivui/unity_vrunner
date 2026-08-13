using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Helper runtime: chụp Game View đúng lúc cuối frame (WaitForEndOfFrame).
/// Chỉ dùng bởi Editor tool RuntimeScreenshotWindow.
/// </summary>
public class RuntimeScreenshotHelper : MonoBehaviour
{
    public static void Capture(int superSize, Action<Texture2D> onComplete)
    {
        var go = new GameObject("RuntimeScreenshotHelper")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        var helper = go.AddComponent<RuntimeScreenshotHelper>();
        helper.StartCoroutine(helper.CaptureRoutine(superSize, onComplete));
    }

    private IEnumerator CaptureRoutine(int superSize, Action<Texture2D> onComplete)
    {
        // Đợi 1 frame (kể cả sau khi Editor vừa unpause) rồi chụp cuối frame.
        // WaitForEndOfFrame không phụ thuộc Time.timeScale nên game pause (timeScale=0) vẫn OK.
        yield return null;
        yield return new WaitForEndOfFrame();

        Texture2D texture = null;
        try
        {
            texture = ScreenCapture.CaptureScreenshotAsTexture(Mathf.Max(1, superSize));
        }
        catch (Exception e)
        {
            Debug.LogError($"[RuntimeScreenshot] Capture failed: {e.Message}");
        }

        onComplete?.Invoke(texture);
        Destroy(gameObject);
    }
}
