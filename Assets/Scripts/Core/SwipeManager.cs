using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class SwipeManager : MonoBehaviour
{
    private Vector2 startPosition;
    [SerializeField] private float minSwipeDistance = 50f; // Minimum pixels to qualify

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerUp += OnFingerUp;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerUp -= OnFingerUp;
        EnhancedTouchSupport.Disable();
    }

    private void OnFingerDown(Finger finger)
    {
        if (finger.index == 0) // Track primary finger
        {
            startPosition = finger.currentTouch.startScreenPosition;
        }
    }

    private void OnFingerUp(Finger finger)
    {
        if (finger.index == 0)
        {
            Vector2 endPosition = finger.currentTouch.screenPosition;
            Vector2 swipeDelta = endPosition - startPosition;

            if (swipeDelta.magnitude >= minSwipeDistance)
            {
                DetermineSwipeDirection(swipeDelta);
            }
        }
    }

    private void DetermineSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            Debug.Log(delta.x > 0 ? "Swiped Right" : "Swiped Left");
        }
        else
        {
            Debug.Log(delta.y > 0 ? "Swiped Up" : "Swiped Down");
        }
    }
}
