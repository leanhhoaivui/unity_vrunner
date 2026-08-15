using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace VRunner.Core
{
    /// <summary>
    /// Facade input event-driven: gộp Keyboard/Gamepad (Input Actions) + swipe Touch
    /// thành semantic events cho endless runner.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;

        [Header("Input Actions")] // InputActionAsset chứa các action map và action.
        [SerializeField] private InputActionAsset inputActions; // InputActionAsset chứa các action map và action.

        [Header("Thresholds")] // Ngưỡng di chuyển làn và swipe.
        [SerializeField] private float laneAxisThreshold = 0.5f; // Ngưỡng di chuyển làn.
        [SerializeField] private float minSwipeDistance = 50f; // Ngưỡng swipe.

        public event Action OnLaneLeft; // Event khi người chơi di chuyển sang làn trái.
        public event Action OnLaneRight; // Event khi người chơi di chuyển sang làn phải.
        public event Action OnJump; // Event khi người chơi nhảy.
        public event Action OnSlide; // Event khi người chơi trượt.

        private InputActionMap playerMap; // Action map của người chơi.
        private InputAction moveAction; // Action di chuyển.
        private InputAction jumpAction; // Action nhảy.
        private InputAction crouchAction; // Action trượt.

        private Vector2 swipeStartPosition; // Vị trí bắt đầu swipe.
        private bool moveAxisLatched; // Flag để kiểm tra xem người chơi đã di chuyển làn hay chưa.
        private bool gameplayInputEnabled = true; // Flag để kiểm tra xem gameplay input đã bật hay chưa.
        private bool callbacksBound; // Flag để kiểm tra xem callbacks đã được bind hay chưa.

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            CacheActions();
        }

        private void OnEnable()
        {
            if (Instance != this)
                return;

            BindCallbacks();
            SetGameplayInputEnabled(true);

            EnhancedTouchSupport.Enable();
            Touch.onFingerDown += OnFingerDown;
            Touch.onFingerUp += OnFingerUp;
        }

        private void OnDisable()
        {
            if (Instance != this)
                return;

            Touch.onFingerDown -= OnFingerDown;
            Touch.onFingerUp -= OnFingerUp;
            EnhancedTouchSupport.Disable();

            UnbindCallbacks();
            playerMap?.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Bật/tắt gameplay input (Pause / Resume / GameOver).
        /// </summary>
        public void SetGameplayInputEnabled(bool enabled)
        {
            gameplayInputEnabled = enabled;

            if (playerMap == null)
                return;

            if (enabled)
                playerMap.Enable();
            else
                playerMap.Disable();
        }

        private void CacheActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("[InputManager] InputActionAsset chưa được assign.");
                return;
            }

            playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError("[InputManager] Không tìm thấy action map 'Player'.");
                return;
            }

            moveAction = playerMap.FindAction("Move");
            jumpAction = playerMap.FindAction("Jump");
            crouchAction = playerMap.FindAction("Crouch");
        }

        private void BindCallbacks()
        {
            if (callbacksBound || playerMap == null)
                return;

            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMoveCanceled;
            }

            if (jumpAction != null)
                jumpAction.performed += OnJumpPerformed;

            if (crouchAction != null)
                crouchAction.performed += OnCrouchPerformed;

            callbacksBound = true;
        }

        private void UnbindCallbacks()
        {
            if (!callbacksBound)
                return;

            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMoveCanceled;
            }

            if (jumpAction != null)
                jumpAction.performed -= OnJumpPerformed;

            if (crouchAction != null)
                crouchAction.performed -= OnCrouchPerformed;

            callbacksBound = false;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            if (!gameplayInputEnabled)
                return;

            Vector2 value = context.ReadValue<Vector2>();
            if (Mathf.Abs(value.x) <= laneAxisThreshold)
            {
                moveAxisLatched = false;
                return;
            }

            // Chỉ fire 1 lần mỗi lần vượt ngưỡng (tránh spam stick)
            if (moveAxisLatched)
                return;

            moveAxisLatched = true;
            if (value.x > 0f)
                OnLaneRight?.Invoke();
            else
                OnLaneLeft?.Invoke();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            moveAxisLatched = false;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!gameplayInputEnabled)
                return;

            OnJump?.Invoke();
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            if (!gameplayInputEnabled)
                return;

            OnSlide?.Invoke();
        }

        private void OnFingerDown(Finger finger)
        {
            if (!gameplayInputEnabled || finger.index != 0)
                return;

            swipeStartPosition = finger.currentTouch.startScreenPosition;
        }

        private void OnFingerUp(Finger finger)
        {
            if (!gameplayInputEnabled || finger.index != 0)
                return;

            Vector2 endPosition = finger.currentTouch.screenPosition;
            Vector2 swipeDelta = endPosition - swipeStartPosition;

            if (swipeDelta.magnitude < minSwipeDistance)
                return;

            if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
            {
                if (swipeDelta.x > 0f)
                    OnLaneRight?.Invoke();
                else
                    OnLaneLeft?.Invoke();
            }
            else
            {
                if (swipeDelta.y > 0f)
                    OnJump?.Invoke();
                else
                    OnSlide?.Invoke();
            }
        }
    }
}
