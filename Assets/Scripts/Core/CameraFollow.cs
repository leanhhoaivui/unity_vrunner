using UnityEngine;
using VRunner.Gameplay.Player;

namespace VRunner.Core
{
    /// <summary>
    /// Camera follow player với smooth damping
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target; // CameraTarget to follow
        [SerializeField] private bool autoFindTarget = true;

        [Header("Follow Settings")]
        [SerializeField] private float smoothSpeed = 5f; // Tốc độ smooth (càng cao càng nhanh)
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f); // Camera offset

        [Header("Look At Settings")]
        [SerializeField] private bool useLookAt = true;
        [SerializeField] private Transform lookAtTarget; // Thường là Player
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

        [Header("Bounds (Optional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 20f;

        private Vector3 velocity = Vector3.zero; // Dùng cho SmoothDamp

        private void Start()
        {
            // Auto find target nếu chưa assign
            if (autoFindTarget && target == null)
            {
                Transform player = FindPlayerTransform();
                if (player != null)
                {
                    // Tìm CameraTarget child
                    Transform cameraTarget = player.Find("CameraTarget");
                    if (cameraTarget != null)
                    {
                        target = cameraTarget;
                        Debug.Log("CameraFollow: Auto-found CameraTarget");
                    }
                    else
                    {
                        // Fallback: follow player directly
                        target = player;
                        Debug.LogWarning("CameraFollow: CameraTarget not found, following Player directly");
                    }

                    // Set lookAtTarget
                    if (lookAtTarget == null)
                    {
                        lookAtTarget = player;
                    }
                }
                else
                {
                    Debug.LogError("CameraFollow: Player not found! Make sure PlayerSpawner ran and prefab has 'Player' tag.");
                }
            }

            // Set initial position
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        private static Transform FindPlayerTransform()
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
                return tagged.transform;

            PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
            return controller != null ? controller.transform : null;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Follow target
            FollowTarget();

            // Look at target
            if (useLookAt && lookAtTarget != null)
            {
                LookAtTarget();
            }
        }

        /// <summary>
        /// Smooth follow target
        /// </summary>
        private void FollowTarget()
        {
            // Calculate desired position
            Vector3 desiredPosition = target.position + offset;

            // Apply bounds nếu enabled
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }

            // Smooth movement
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position, 
                desiredPosition, 
                ref velocity, 
                1f / smoothSpeed
            );

            transform.position = smoothedPosition;
        }

        /// <summary>
        /// Look at target với offset
        /// </summary>
        private void LookAtTarget()
        {
            Vector3 lookPosition = lookAtTarget.position + lookAtOffset;
            transform.LookAt(lookPosition);
        }

        /// <summary>
        /// Set target at runtime
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Shake camera (for effects)
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            Vector3 originalOffset = offset;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                offset = originalOffset + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            offset = originalOffset;
        }
    }
}
