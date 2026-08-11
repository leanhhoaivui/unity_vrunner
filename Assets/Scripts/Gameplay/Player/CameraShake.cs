using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        originalPosition = transform.localPosition;
        
        // Subscribe to hit event
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnObstacleHit += (type) => Shake(0.3f, 0.3f);
        }
    }
    
    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }
    
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPosition;
    }
}