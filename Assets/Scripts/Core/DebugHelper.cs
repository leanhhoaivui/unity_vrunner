using UnityEngine;

namespace VRunner.Core
{
    public class DebugHelper : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showLanePositions = true;
        [SerializeField] private float laneSpacing = 3f;

        private void OnDrawGizmos()
        {
            if (!showLanePositions) return;

            // Vẽ 3 lanes trong Scene view
            Gizmos.color = Color.red;
            DrawLaneLine(-laneSpacing); // Left

            Gizmos.color = Color.green;
            DrawLaneLine(0f); // Center

            Gizmos.color = Color.blue;
            DrawLaneLine(laneSpacing); // Right
        }

        private void DrawLaneLine(float xPosition)
        {
            Vector3 start = new Vector3(xPosition, 0.1f, -10f);
            Vector3 end = new Vector3(xPosition, 0.1f, 50f);
            Gizmos.DrawLine(start, end);
        }
    }
}
