using UnityEngine;
using VRunner.Gameplay.Collectible;

namespace VRunner.Gameplay.Player
{
    /// <summary>
    /// Hút coin trong bán kính magnetRadius về phía player.
    /// Bật qua PowerupManager khi collect Magnet.
    /// </summary>
    public class CoinMagnet : MonoBehaviour
    {
        [Header("Magnet Settings")]
        [SerializeField] private bool magnetActive;
        [SerializeField] private float magnetRadius = 5f;
        [SerializeField] private LayerMask coinLayer;

        public bool MagnetActive
        {
            get => magnetActive;
            set => magnetActive = value;
        }

        public float MagnetRadius => magnetRadius;

        public void SetRadius(float radius)
        {
            magnetRadius = Mathf.Max(0.1f, radius);
        }

        public void SetCoinLayer(LayerMask layer)
        {
            coinLayer = layer;
        }

        private void Awake()
        {
            if (coinLayer.value == 0)
            {
                int layer = LayerMask.NameToLayer("CollectibleLayer");
                if (layer >= 0)
                    coinLayer = 1 << layer;
            }
        }

        private void Update()
        {
            if (!magnetActive) return;

            PullCoinsInRange();
        }

        private void PullCoinsInRange()
        {
            if (coinLayer.value == 0) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius, coinLayer);

            foreach (Collider hit in hits)
            {
                Coin coin = hit.GetComponent<Coin>();
                if (coin != null)
                    coin.EnableMagnet(transform);
            }
        }

        private void OnDrawGizmos()
        {
            if (!magnetActive) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, magnetRadius);
        }
    }
}
