using UnityEngine;

public class FPSController : MonoBehaviour
{
    void Awake()
    {
        // 1. Tắt tính năng VSync để quyền kiểm soát FPS thuộc về targetFrameRate
        QualitySettings.vSyncCount = 0;

        // 2. Thiết lập số FPS mục tiêu mong muốn (Ví dụ: 60, 90 hoặc 120)
        Application.targetFrameRate = 60; 
    }
}
