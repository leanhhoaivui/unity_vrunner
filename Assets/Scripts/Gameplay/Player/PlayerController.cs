using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Định nghĩa các lanes player có thể di chuyển
/// Gán giá trị int (-1, 0, 1) để dễ tính toán position
/// Left = -1 → X position = -3 (nếu lane spacing = 3)
/// </summary>
public enum Lane
{
    Left = -1,
    Center = 0,
    Right = 1
}

/// <summary>
/// Quản lý movement của Player: auto-run, lane switching, gravity
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Serialized Fields
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float maxForwardSpeed = 20f;
    // [SerializeField] private float speedIncreaseRate = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Lane Settings")]
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private float laneChangeSpeed = 10f;
    [SerializeField] private float laneChangeCooldown = 0.3f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;        // Chiều cao nhảy (units)
    [SerializeField] private float coyoteTime = 0.1f;      // Thời gian nhảy sau khi rời ground
    [SerializeField] private float jumpBufferTime = 0.1f;  // Thời gian buffer input
    private bool isJumping = false;       // Đang trong trạng thái jump
    private float jumpVelocity;           // Vận tốc nhảy (tính từ jumpHeight)
    private float lastGroundedTime = 0f;  // Thời điểm cuối cùng trên ground
    private float lastJumpInputTime = 0f; // Thời điểm cuối cùng nhấn jump

    [Header("Animation")]
    [SerializeField] private PlayerAnimation playerAnimation;
    private bool wasGrounded = true;
    private PlayerState currentState = PlayerState.Normal;
    
    [Header("Speed Progression")]
    [SerializeField] private bool enableSpeedRampup = true;
    [SerializeField] private float speedIncreaseRate = 0.5f; // +0.5 units/s per 100m
    [SerializeField] private float speedIncreaseInterval = 100f; // Mỗi 100 meters
    [SerializeField] private float maxSpeed = 30f;

    private float baseSpeed;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugUI = false;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI distanceTraveledText;
    [SerializeField] private bool isPauseForward = false;
    #endregion

    #region Private Fields
    private CharacterController characterController;
    private float currentSpeed;
    private float verticalVelocity;

    private Lane currentLane = Lane.Center;
    private Lane targetLane = Lane.Center;
    private Vector3 targetPosition;

    private readonly Queue<bool> inputBuffer = new Queue<bool>();
    private float lastLaneChangeTime;
    private bool inputSubscribed;
    #endregion

    #region Public Properties
    public float DistanceTraveled { get; private set; }
    #endregion

    #region MonoBehaviour Callbacks
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ValidateComponents();
        if (playerAnimation == null)
            playerAnimation = GetComponent<PlayerAnimation>();
    }

    private void Start()
    {
        // Fallback nếu OnEnable chạy trước InputManager.Awake
        if (!inputSubscribed)
            SubscribeInput();

        currentSpeed = forwardSpeed;
        targetPosition = transform.position;

        // Tính jump velocity từ desired jump height
        CalculateJumpVelocity();

        baseSpeed = forwardSpeed;
    
        if (enableSpeedRampup)
        {
            StartCoroutine(SpeedRampupCoroutine());
        }
    }

    private void Update()
    {
        if (currentState == PlayerState.Dying)
            return;

        ProcessLaneBuffer();
        HandleForwardMovement();
        HandleLaneMovement();
        HandleVerticalMovement();
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUI.Label(new Rect(10, 10, 200, 20), $"Speed: {currentSpeed:F2} m/s");
        GUI.Label(new Rect(10, 30, 200, 20), $"Distance: {transform.position.z:F2} m");
        GUI.Label(new Rect(10, 50, 200, 20), $"Current Lane: {currentLane}");
        GUI.Label(new Rect(10, 70, 200, 20), $"Target Lane: {targetLane}");
        // FPS
        GUI.Label(new Rect(10, 90, 200, 20), $"FPS: {1.0f / Time.deltaTime:F2}");
    }

    private void OnEnable()
    {
        SubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    public void PlayRunning()
    {
        if (playerAnimation != null)
            playerAnimation.PlayRunning();
    }

    private void OnDrawGizmos()
    {
        float gizmoLength = 50f;

        Gizmos.color = currentLane == Lane.Left ? Color.green : Color.red;
        Gizmos.DrawLine(
            new Vector3(GetLaneXPosition(Lane.Left), 0.1f, -10f),
            new Vector3(GetLaneXPosition(Lane.Left), 0.1f, gizmoLength));

        Gizmos.color = currentLane == Lane.Center ? Color.green : Color.yellow;
        Gizmos.DrawLine(
            new Vector3(GetLaneXPosition(Lane.Center), 0.1f, -10f),
            new Vector3(GetLaneXPosition(Lane.Center), 0.1f, gizmoLength));

        Gizmos.color = currentLane == Lane.Right ? Color.green : Color.blue;
        Gizmos.DrawLine(
            new Vector3(GetLaneXPosition(Lane.Right), 0.1f, -10f),
            new Vector3(GetLaneXPosition(Lane.Right), 0.1f, gizmoLength));

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
    }
    #endregion

    #region UI Methods
    private void HandleUpdateUI()
    {
        if (!showDebugUI) return;

        speedText.text = $"Speed: {currentSpeed:F2} m/s";
        distanceText.text = $"Distance: {transform.position.z:F2} m";
        distanceTraveledText.text = $"Distance Traveled: {DistanceTraveled:F2} m";
    }
    #endregion

    #region Input Handling Methods
    private void SubscribeInput()
    {
        if (inputSubscribed || InputManager.Instance == null)
            return;

        InputManager.Instance.OnLaneLeft += OnLaneLeft;
        InputManager.Instance.OnLaneRight += OnLaneRight;
        InputManager.Instance.OnJump += OnJumpInput;
        InputManager.Instance.OnSlide += OnSlideInput;
        inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!inputSubscribed || InputManager.Instance == null)
            return;

        InputManager.Instance.OnLaneLeft -= OnLaneLeft;
        InputManager.Instance.OnLaneRight -= OnLaneRight;
        InputManager.Instance.OnJump -= OnJumpInput;
        InputManager.Instance.OnSlide -= OnSlideInput;
        inputSubscribed = false;
    }

    private void OnLaneLeft() {
        // Đổi chiều: bỏ các lệnh cùng chiều đang chờ
        if (inputBuffer.Count > 0 && inputBuffer.Peek() == true)
            inputBuffer.Clear();
        // Chỉ giữ tối đa 1 lệnh
        inputBuffer.Clear();
        inputBuffer.Enqueue(false);
    }

    private void OnLaneRight() {
        // Đổi chiều: bỏ các lệnh cùng chiều đang chờ
        if (inputBuffer.Count > 0 && inputBuffer.Peek() == true)
            inputBuffer.Clear();
        // Chỉ giữ tối đa 1 lệnh
        inputBuffer.Clear();
        inputBuffer.Enqueue(true);
    }

    private void OnJumpInput() {
        lastJumpInputTime = Time.time;
    }

    private void OnSlideInput()
    {
        // TODO: slide mechanic
    }

    /// <summary>
    /// Xử lý queue đổi lane theo cooldown (giữ buffer khi spam input).
    /// </summary>
    private void ProcessLaneBuffer()
    {
        if (inputBuffer.Count > 0 && Time.time - lastLaneChangeTime >= laneChangeCooldown)
        {
            bool moveRight = inputBuffer.Dequeue();
            ChangeLane(moveRight);
            lastLaneChangeTime = Time.time;
        }
    }
    #endregion

    #region Movement Methods
    private void HandleForwardMovement()
    {
        if (isPauseForward) {
            return;
        }

        if (currentSpeed < maxForwardSpeed)
        {
            currentSpeed += speedIncreaseRate * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxForwardSpeed);
        }

        Vector3 moveVector = transform.forward * currentSpeed;
        characterController.Move(moveVector * Time.deltaTime);
    }

    #endregion

    #region Lane Methods
    /// <summary>
    /// Tính vị trí X của một lane
    /// </summary>
    private float GetLaneXPosition(Lane lane)
    {
        return (int)lane * laneDistance;
    }

    
    /// <summary>
    /// Chuyển sang lane bên trái hoặc phải
    /// </summary>
    /// <param name="moveRight">True = sang phải, False = sang trái</param>
    private void ChangeLane(bool moveRight)
    {
        int direction = moveRight ? 1 : -1;
        Lane newLane = targetLane + direction;

        if (newLane < Lane.Left || newLane > Lane.Right)
        {
            return;
        }

        targetLane = newLane;

        float targetX = GetLaneXPosition(targetLane);
        targetPosition = new Vector3(targetX, transform.position.y, transform.position.z);

        Debug.Log($"Switching to lane: {targetLane}");
    }

    /// <summary>
    /// Di chuyển mượt mà đến target lane bằng Lerp + CharacterController
    /// </summary>
    private void HandleLaneMovement()
    {
        Vector3 currentPosition = transform.position;
        float distanceToTarget = Mathf.Abs(currentPosition.x - targetPosition.x);

        float newX;
        if (distanceToTarget < 0.01f)
        {
            newX = targetPosition.x;
            currentLane = targetLane;
        }
        else
        {
            newX = Mathf.Lerp(currentPosition.x, targetPosition.x, laneChangeSpeed * Time.deltaTime);
        }

        float deltaX = newX - currentPosition.x;
        if (Mathf.Abs(deltaX) > 0.0001f)
        {
            characterController.Move(new Vector3(deltaX, 0f, 0f));
        }
    }

    /// <summary>
    /// Tính jump velocity từ jump height và gravity
    /// </summary>
    private void CalculateJumpVelocity()
    {
        // Formula: v = sqrt(2 * h * g)
        //Lưu ý: trọng lực là âm trong Unity, vì vậy chúng ta cần sử dụng Mathf.Abs(Gravity) để làm cho nó dương
        //Vậy công thức phải là: v = sqrt(2 *h *-g)
        jumpVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
        Debug.Log($"Jump velocity calculated: {jumpVelocity}");
    }
    #endregion

    #region Jump and Gravity Methods
    // private void HandleGravity()
    // {
    //     if (characterController.isGrounded)
    //     {
    //         verticalVelocity = -2f;
    //         m_isJumping = false;
    //     }
    //     else
    //     {
    //         verticalVelocity += gravity * Time.deltaTime;
    //     }

    //     Vector3 verticalMove = new Vector3(0, verticalVelocity, 0);
    //     characterController.Move(verticalMove * Time.deltaTime);
    // }
    /// <summary>
    /// Xử lý jump và gravity
    /// </summary>
    private void HandleVerticalMovement()
    {
        bool grounded = characterController.isGrounded;

        // Update last grounded time
        if (grounded)
        {
            lastGroundedTime = Time.time;
            isJumping = false;

            // Vừa chạm đất lại → PlayLand
            if (!wasGrounded)
                playerAnimation?.PlayLand();
        }

        wasGrounded = grounded;
        
        if (CanJump())
            Jump();
        
        // Apply gravity
        ApplyGravity();
        
        // Apply vertical movement
        Vector3 verticalMove = new Vector3(0, verticalVelocity, 0);
        characterController.Move(verticalMove * Time.deltaTime);
    }
    /// <summary>
    /// Check xem player có thể jump không
    /// </summary>
    private bool CanJump()
    {
        // Check jump input (trong jump buffer time)
        bool hasJumpInput = Time.time - lastJumpInputTime < jumpBufferTime;
        
        if (!hasJumpInput)
            return false;
        
        // Check grounded (hoặc trong coyote time)
        bool isGroundedOrCoyote = Time.time - lastGroundedTime < coyoteTime;
        
        if (!isGroundedOrCoyote)
            return false;
        
        // Không cho double jump (nếu muốn double jump, bỏ điều kiện này)
        if (isJumping)
            return false;
        
        return true;
    }
    /// <summary>
    /// Thực hiện jump
    /// </summary>
    private void Jump()
    {
        // Set vertical velocity = jump velocity
        verticalVelocity = jumpVelocity;

        // Mark as jumping
        isJumping = true;

        // Reset jump input (để không jump liên tục)
        lastJumpInputTime = 0f;

        playerAnimation?.PlayJump();
        EventManager.Instance?.TriggerPlayerJump(jumpHeight);

        Debug.Log("Player jumped!");
    }
    /// <summary>
    /// Áp dụng gravity
    /// </summary>
    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0)
        {
            // Grounded: dính ground
            verticalVelocity = -2f;
        }
        else
        {
            // Falling: apply gravity
            verticalVelocity += gravity * Time.deltaTime;
            
            // Clamp fall velocity (tránh rơi quá nhanh)
            verticalVelocity = Mathf.Max(verticalVelocity, -50f);
        }
    }

    /// <summary>
    /// Tăng tốc độ dựa trên khoảng cách
    /// </summary>
    private IEnumerator SpeedRampupCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // Check mỗi 10 giây
            
            UpdateSpeed();
        }
    }
    private void UpdateSpeed()
    {
        if (ScoreManager.Instance == null) return;
        
        float distance = ScoreManager.Instance.DistanceTraveled;
        
        // Tính tốc độ dựa trên khoảng cách
        int speedTier = Mathf.FloorToInt(distance / speedIncreaseInterval);
        float newSpeed = baseSpeed + (speedTier * speedIncreaseRate);
        
        // Giữ ở mức tối đa
        newSpeed = Mathf.Min(newSpeed, maxSpeed);
        
        if (newSpeed > forwardSpeed)
        {
            forwardSpeed = newSpeed;
            Debug.Log($"Speed increased to {forwardSpeed} at distance {distance}m");
        }
    }
    #endregion

    #region Player State Methods
    // private void HandlePlayerState()
    // {
    //     if (currentState != PlayerState.Idle) return;

    //     if (characterController.isGrounded)
    //     {
    //         UpdatePlayerState(PlayerState.Running);
    //     }
    //     else
    //     {
    //         UpdatePlayerState(PlayerState.Falling);
    //     }
    // }
    // private void UpdatePlayerState(PlayerState newState)
    // {
    //     currentState = newState;
    //     Debug.Log($"Player state updated: {currentState}");
    // }
    #endregion

    #region Validation
    private void ValidateComponents()
    {
        if (characterController == null)
            Debug.LogError($"[PlayerController] Missing CharacterController on {gameObject.name}");
        if (playerAnimation == null)
            Debug.LogWarning($"[PlayerController] Missing PlayerAnimation on {gameObject.name}");
    }
    private void OnValidate()
    {
        // Gọi khi giá trị Inspector thay đổi
        if (Application.isPlaying)
        {
            CalculateJumpVelocity();
        }
    }
    #endregion

    #region Public Methods
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public void ResetSpeed()
    {
        currentSpeed = forwardSpeed;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeed *= multiplier;
    }

    /// <summary>
    /// Phát animation die (ngã ngang) và dừng điều khiển player.
    /// </summary>
    public void Die()
    {
        if (currentState == PlayerState.Dying)
            return;
        currentState = PlayerState.Dying;
        currentSpeed = 0f;
        playerAnimation?.PlayDeath();
    }
    #endregion
}
