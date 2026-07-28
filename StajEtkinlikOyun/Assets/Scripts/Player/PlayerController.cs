using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of the player.")]
    public float moveSpeed = 6f;

    [Tooltip("Rotation speed of the player turning towards movement direction.")]
    public float rotationSpeed = 720f;

    [Header("Physics Settings")]
    [Tooltip("Gravity force applied to the player.")]
    public float gravity = -9.81f;

    private CharacterController _controller;
    private InputAction _moveAction;
    private Vector3 _velocity;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        
        // Load baseline speed upgrade from PlayerPrefs (meta-progression)
        int speedLevel = PlayerPrefs.GetInt("Shop_MoveSpeed", 0);
        moveSpeed = 6f + (speedLevel * 0.5f);
        
        // Find the "Move" action directly from the project's global Input Actions
        _moveAction = InputSystem.actions.FindAction("Player/Move");
        
        // We MUST enable the action or its action map to start receiving inputs
        if (_moveAction != null)
        {
            _moveAction.actionMap.Enable();
        }
    }

    private void Update()
    {
        Vector2 input = Vector2.zero;
        if (_moveAction != null)
        {
            input = _moveAction.ReadValue<Vector2>();
        }

        // Calculate direction in world space
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y).normalized;

        // Apply movement if there is input
        if (moveDirection.magnitude >= 0.1f)
        {
            // Move character using CharacterController
            _controller.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Rotate character towards movement direction smoothly
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Apply gravity so player stays grounded
        if (_controller.isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Slight downward force to keep grounded
        }
        else
        {
            _velocity.y += gravity * Time.deltaTime;
        }

        // Apply gravity velocity
        _controller.Move(_velocity * Time.deltaTime);
    }
}


