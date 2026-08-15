using UnityEngine;

namespace VRunner.Core
{
    public class AnalyticsTracker : MonoBehaviour
    {
        private void OnEnable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameOver += LogGameOver;
                EventManager.Instance.OnDistanceMilestone += LogMilestone;
            }
        }

        private void OnDisable()
        {

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnGameOver -= LogGameOver;
                EventManager.Instance.OnDistanceMilestone -= LogMilestone;
            }
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
}
