using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [Header("Target settings")]
    [Tooltip("The player transform that the camera will follow.")]
    public Transform target;

    [Header("Offset Settings")]
    [Tooltip("Distance offset from the player.")]
    public Vector3 offset = new Vector3(0f, 12f, -8f);

    [Header("Movement Settings")]
    [Tooltip("How smoothly the camera catches up to its target. Lower values are smoother.")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;

    [Header("Angle Settings")]
    [Tooltip("Pitch (X rotation) of the camera looking down at the player.")]
    public float cameraPitch = 55f;

    private Vector3 _currentVelocity;

    private void Start()
    {
        // Align the camera rotation to look downwards at the specified angle
        transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate target position based on the offset
        Vector3 targetPosition = target.position + offset;

        // Smoothly interpolate camera position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothSpeed);
    }
}
