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

    [Header("Shake Settings")]
    [Tooltip("How fast a shake dies down. Higher = snappier.")]
    public float shakeDecay = 6f;
    [Tooltip("Upper limit on shake strength so big hits can't throw the camera off.")]
    public float maxShake = 0.6f;

    private Vector3 _currentVelocity;
    private float _shake; // current shake magnitude, decays to 0

    // Lets other systems (e.g. JuiceManager on enemy hit/death) request a shake.
    public static TopDownCameraFollow Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Align the camera rotation to look downwards at the specified angle
        transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    /// <summary>Adds to the current shake (clamped). Call on impacts, deaths, etc.</summary>
    public void AddShake(float intensity)
    {
        _shake = Mathf.Min(_shake + intensity, maxShake);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate target position based on the offset
        Vector3 targetPosition = target.position + offset;

        // Smoothly interpolate camera position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothSpeed);

        // Add a decaying random offset on top for screen shake. unscaledDeltaTime so
        // the shake keeps animating through hit-stop (when timeScale is 0).
        if (_shake > 0f)
        {
            Vector3 rnd = Random.insideUnitSphere * _shake;
            transform.position += new Vector3(rnd.x, rnd.y * 0.4f, rnd.z);
            _shake = Mathf.MoveTowards(_shake, 0f, shakeDecay * Time.unscaledDeltaTime);
        }
    }
}
