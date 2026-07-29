using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of the player.")]
    public float moveSpeed = 6f;

    [Tooltip("Rotation speed when turning toward the movement direction.")]
    public float rotationSpeed = 720f;

    [Header("Dash Settings")]
    [Tooltip("Total distance covered during a dash.")]
    [SerializeField, Min(0.1f)]
    private float dashDistance = 5f;

    [Tooltip("How long the dash movement lasts.")]
    [SerializeField, Min(0.05f)]
    private float dashDuration = 0.18f;

    [Tooltip("Time required before the player can dash again.")]
    [SerializeField, Min(0f)]
    private float dashCooldown = 2.5f;

    [Tooltip("Makes the player immune to damage while dashing.")]
    [SerializeField]
    private bool invulnerableWhileDashing = true;

    [Header("Physics Settings")]
    [Tooltip("Gravity force applied to the player.")]
    public float gravity = -9.81f;

    public bool IsDashing => _isDashing;
    public float DashCooldownRemaining => _dashCooldownTimer;

    public float DashCooldownNormalized
    {
        get
        {
            if (dashCooldown <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(_dashCooldownTimer / dashCooldown);
        }
    }

    private CharacterController _controller;
    private PlayerStats _playerStats;

    private InputAction _moveAction;
    private InputAction _dashAction;

    private Vector3 _verticalVelocity;
    private Vector3 _lastMoveDirection;
    private Vector3 _dashDirection;

    private float _dashTimeRemaining;
    private float _dashCooldownTimer;

    private bool _isDashing;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();

        _playerStats = GetComponent<PlayerStats>();

        LoadPermanentMoveSpeed();
        FindInputActions();
        ResetDashState();

        _lastMoveDirection = transform.forward;
        _lastMoveDirection.y = 0f;

        if (_lastMoveDirection.sqrMagnitude < 0.01f)
        {
            _lastMoveDirection = Vector3.forward;
        }
    }

    private void Update()
    {
        // Prevent input from starting actions while menus
        // or upgrade selections have paused the game.
        if (Time.timeScale <= 0f)
        {
            return;
        }

        UpdateDashCooldown();

        Vector3 moveDirection = ReadMoveDirection();

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            _lastMoveDirection = moveDirection;
        }

        if (!_isDashing && WasDashPressed())
        {
            StartDash(moveDirection);
        }

        if (_isDashing)
        {
            UpdateDash();
        }
        else
        {
            UpdateNormalMovement(moveDirection);
        }

        ApplyGravity();
    }

    private void LoadPermanentMoveSpeed()
    {
        int speedLevel = PlayerPrefs.GetInt("Shop_MoveSpeed", 0);

        moveSpeed = 6f + speedLevel * 0.5f;
    }

    private void FindInputActions()
    {
        _moveAction = InputSystem.actions.FindAction("Player/Move");

        // Optional action. Left Shift still works when
        // Player/Dash has not been created.
        _dashAction = InputSystem.actions.FindAction("Player/Dash");

        if (_moveAction != null)
        {
            _moveAction.actionMap.Enable();
        }

        if (_dashAction != null && !_dashAction.enabled)
        {
            _dashAction.Enable();
        }
    }

    private Vector3 ReadMoveDirection()
    {
        if (_moveAction == null)
        {
            return Vector3.zero;
        }

        Vector2 input = _moveAction.ReadValue<Vector2>();

        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        return moveDirection;
    }

    private bool WasDashPressed()
    {
        if (_dashAction != null && _dashAction.WasPressedThisFrame())
        {
            return true;
        }

        return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
    }

    private void UpdateNormalMovement(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        _controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        RotateTowards(moveDirection);
    }

    private void StartDash(Vector3 currentMoveDirection)
    {
        if (_dashCooldownTimer > 0f)
        {
            return;
        }

        _dashDirection =
            currentMoveDirection.sqrMagnitude > 0.01f
                ? currentMoveDirection.normalized
                : _lastMoveDirection.normalized;

        if (_dashDirection.sqrMagnitude < 0.01f)
        {
            _dashDirection = transform.forward;
        }

        _dashDirection.y = 0f;
        _dashDirection.Normalize();

        _isDashing = true;
        _dashTimeRemaining = dashDuration;
        _dashCooldownTimer = dashCooldown;

        if (invulnerableWhileDashing && _playerStats != null)
        {
            _playerStats.SetInvulnerable(true);
        }

        RotateTowards(_dashDirection);
    }

    private void UpdateDash()
    {
        float dashSpeed = dashDistance / Mathf.Max(0.01f, dashDuration);

        CollisionFlags collisionFlags = _controller.Move(
            _dashDirection * dashSpeed * Time.deltaTime
        );

        RotateTowards(_dashDirection);

        _dashTimeRemaining -= Time.deltaTime;

        bool hitWall = (collisionFlags & CollisionFlags.Sides) != 0;

        if (_dashTimeRemaining <= 0f || hitWall)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        if (!_isDashing)
        {
            return;
        }

        _isDashing = false;
        _dashTimeRemaining = 0f;

        if (invulnerableWhileDashing && _playerStats != null)
        {
            _playerStats.SetInvulnerable(false);
        }
    }

    private void UpdateDashCooldown()
    {
        if (_dashCooldownTimer <= 0f)
        {
            return;
        }

        _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - Time.deltaTime);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity.y < 0f)
        {
            _verticalVelocity.y = -2f;
        }
        else
        {
            _verticalVelocity.y += gravity * Time.deltaTime;
        }

        _controller.Move(_verticalVelocity * Time.deltaTime);
    }

    private void ResetDashState()
    {
        _isDashing = false;
        _dashTimeRemaining = 0f;
        _dashCooldownTimer = 0f;

        if (_playerStats != null)
        {
            _playerStats.SetInvulnerable(false);
        }
    }

    private void OnDisable()
    {
        EndDash();
    }
}
