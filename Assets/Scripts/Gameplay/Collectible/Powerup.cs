using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Powerup : MonoBehaviour
{
    [SerializeField] private PowerupData data;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatFrequency = 2f;
    
    private Vector3 startPosition;
    private Collider powerupCollider;
    
    public PowerupData Data => data;
    
    private void Awake()
    {
        powerupCollider = GetComponent<Collider>();
        powerupCollider.isTrigger = true;
    }
    
    private void OnEnable()
    {
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
        gameObject.SetActive(false);
    }
}