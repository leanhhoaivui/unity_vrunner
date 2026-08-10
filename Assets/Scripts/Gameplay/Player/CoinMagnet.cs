using UnityEngine;

/// <summary>
/// Hút coin trong bán kính magnetRadius về phía player.
/// Bật magnetActive để test bài 3; Tutorial 11 sẽ nối với Powerup Magnet.
/// </summary>
public class CoinMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private bool magnetActive = true;
    [SerializeField] private float magnetRadius = 5f;
    [SerializeField] private LayerMask coinLayer;

    public bool MagnetActive
    {
        get => magnetActive;
        set => magnetActive = value;
    }

    public float MagnetRadius => magnetRadius;

    private void Update()
    {
        if (!magnetActive) return;

        PullCoinsInRange();
    }

    private void PullCoinsInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius, coinLayer);

        foreach (Collider hit in hits)
        {
            Coin coin = hit.GetComponent<Coin>();
            if (coin != null)
            {
                coin.EnableMagnet(transform);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!magnetActive) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
