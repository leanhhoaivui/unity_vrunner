using UnityEngine;
public class AnalyticsTracker : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.Instance.OnGameOver += LogGameOver;
        EventManager.Instance.OnDistanceMilestone += LogMilestone;
    }
    
    private void OnDisable()
    {
        // Unsubscribe...
    }
    
    private void LogGameOver()
    {
        Debug.Log("Analytics: Game Over");
        // Send to analytics service
    }
    
    private void LogMilestone(float distance)
    {
        Debug.Log($"Analytics: Milestone {distance}m");
    }
}