using UnityEngine;
using VRunner.Core;
using VRunner.Data;
using VRunner.Gameplay.Level;

namespace VRunner.Gameplay.Collectible
{
    [RequireComponent(typeof(Collider))]
    public class Powerup : MonoBehaviour
    {
        [SerializeField] private PowerupData data;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float floatAmplitude = 0.3f;
        [SerializeField] private float floatFrequency = 2f;

        [Header("Effects")]
        [SerializeField] private PooledVFX collectVFX;

        private Vector3 startPosition;
        private Collider powerupCollider;
        private bool isCollected;

        public PowerupData Data => data;

        private void Awake()
        {
            powerupCollider = GetComponent<Collider>();
            powerupCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            isCollected = false;
            startPosition = transform.localPosition;
        }

        private void Update()
        {
            // Rotate
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Float up/down
            float offset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.localPosition = startPosition + Vector3.up * offset;
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            if (collectVFX != null && PoolManager.Instance != null)
                PoolManager.Instance.GetVFX(collectVFX, transform.position);

            gameObject.SetActive(false);
        }
    }
}
