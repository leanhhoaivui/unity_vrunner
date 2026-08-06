using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;


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
    [SerializeField] private float forwardSpeed = 0.5f;
    [SerializeField] private float maxForwardSpeed = 20f;
    [SerializeField] private float speedIncreaseRate = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    [Header("Lane Settings")]
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private float laneChangeSpeed = 10f;
    [SerializeField] private float laneChangeCooldown = 0.3f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 3f;        // Chiều cao nhảy (units)
    [SerializeField] private float coyoteTime = 0.1f;      // Thời gian nhảy sau khi rời ground
    [SerializeField] private float jumpBufferTime = 0.1f;  // Thời gian buffer input
    private bool isJumping = false;       // Đang trong trạng thái jump
    private float jumpVelocity;           // Vận tốc nhảy (tính từ jumpHeight)
    private float lastGroundedTime = 0f;  // Thời điểm cuối cùng trên ground
    private float lastJumpInputTime = 0f; // Thời điểm cuối cùng nhấn jump

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI distanceTraveledText;
    [SerializeField] private bool isPauseForward = false;
    #endregion

    #region Input Actions
    public InputActionAsset InputActions;
    private InputAction m_moveAction;
    private InputAction m_lookAction;
    private InputAction m_jumpAction;
    private Vector2 m_moveAmt;
    private Vector2 m_lookAmt;
    private bool m_isJumping = false;
    // private Animation m_animation;
    // private Rigidbody m_rigidbody;
    private Animator m_animator;
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
    #endregion

    #region Public Properties
    public float DistanceTraveled { get; private set; }
    #endregion

    #region MonoBehaviour Callbacks
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ValidateComponents();

        m_moveAction = InputActions.FindActionMap("Player").FindAction("Move");
        m_lookAction = InputActions.FindActionMap("Player").FindAction("Look");
        m_jumpAction = InputActions.FindActionMap("Player").FindAction("Jump");

        // m_animation = GetComponent<Animation>();
        // m_rigidbody = GetComponent<Rigidbody>();
        m_animator = GetComponent<Animator>();
    }

    private void Start()
    {
        currentSpeed = forwardSpeed;
        targetPosition = transform.position;

        // Tính jump velocity từ desired jump height
        CalculateJumpVelocity();
    }

    private void Update()
    {
        HandleInput();
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
    }

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
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
    /// <summary>
    /// Xử lý input để chuyển lane (A/D hoặc Left/Right qua Input System Move)
    /// </summary>
    private void HandleInput()
    {
        m_moveAmt = m_moveAction.ReadValue<Vector2>();
        // m_lookAmt = m_lookAction.ReadValue<Vector2>();

        // Chỉ nhận input ngang — tránh W/S vô tình enqueue lane change
        if (m_moveAction.WasPressedThisFrame() && Mathf.Abs(m_moveAmt.x) > 0.5f)
        {
            inputBuffer.Enqueue(m_moveAmt.x > 0f);
        }

        if (inputBuffer.Count > 0 && Time.time - lastLaneChangeTime >= laneChangeCooldown)
        {
            bool moveRight = inputBuffer.Dequeue();
            ChangeLane(moveRight);
            lastLaneChangeTime = Time.time;
        }

        // Jump input - sử dụng Jump action
        if (m_jumpAction.WasPressedThisFrame())
        {
            lastJumpInputTime = Time.time; // Lưu thời điểm nhấn jump
        }
    }
    #endregion

    #region Movement Methods
    private void HandleForwardMovement()
    {
        if (isPauseForward) return;

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
        jumpVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(gravity));
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
        // Update last grounded time
        if (characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
            isJumping = false;
        }
        
        // Check jump conditions
        bool canJump = CanJump();
        
        if (canJump)
        {
            Jump();
        }
        
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
    #endregion

    #region Validation
    private void ValidateComponents()
    {
        if (characterController == null)
        {
            Debug.LogError($"[PlayerController] Missing CharacterController on {gameObject.name}");
        }
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
    #endregion
}
