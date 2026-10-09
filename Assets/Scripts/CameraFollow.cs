using UnityEngine;
using UnityEngine.SceneManagement;

/// Level camera: damped follow with velocity look-ahead, a mild zoom-out with speed, and an
/// optional clamp to the LevelBounds in the scene.
public class CameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;      // Player object (SnailShell)
    public Vector3 offset;        // Offset from the player position
    [Tooltip("Seconds the camera takes to catch up. Small keeps it tight.")]
    public float smoothTime = 0.06f;
    [Tooltip("The camera leads the shell by its velocity times this many seconds.")]
    public float lookAheadTime = 0.25f;
    [Tooltip("Maximum lead in units.")]
    public float maxLookAhead = 4f;
    [Tooltip("If the target jumps further than this in one frame (a respawn), snap instead of easing.")]
    public float snapDistance = 15f;

    [Header("Zoom")]
    public float baseOrthoSize = 5.31f;
    public float maxOrthoSize = 6.5f;
    [Tooltip("Speed at which the zoom-out reaches maxOrthoSize, units per second.")]
    public float zoomSpeed = 22f;
    public float zoomSmoothTime = 0.3f;

    private Camera cam;
    private Rigidbody2D targetBody;
    private LevelBounds bounds;
    private Vector3 velocity;
    private float zoomVelocity;
    private Vector3 lastTargetPosition;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Only follow in level scenes, and only on the main camera.
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.Contains("Level") || Camera.main != cam)
        {
            enabled = false;
            return;
        }

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        if (target != null)
            targetBody = target.GetComponent<Rigidbody2D>();

        bounds = FindAnyObjectByType<LevelBounds>();
        if (cam != null)
            cam.orthographicSize = baseOrthoSize;
    }

    void Start()
    {
        if (target != null)
            Snap();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if ((target.position - lastTargetPosition).sqrMagnitude > snapDistance * snapDistance)
            Snap();
        lastTargetPosition = target.position;

        Vector2 v = targetBody != null ? targetBody.linearVelocity : Vector2.zero;
        Vector2 lead = Vector2.ClampMagnitude(v * lookAheadTime, maxLookAhead);
        Vector3 desired = target.position + (Vector3)lead + offset;
        desired.z = transform.position.z;

        Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        next.z = transform.position.z;

        if (cam != null && cam.orthographic)
        {
            float t = Mathf.Clamp01(v.magnitude / zoomSpeed);
            float wanted = Mathf.Lerp(baseOrthoSize, maxOrthoSize, t);
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, wanted, ref zoomVelocity, zoomSmoothTime);
        }

        transform.position = Clamp(next);
        transform.rotation = Quaternion.identity;
    }

    /// Cut straight to the target (used on spawn and respawn).
    public void Snap()
    {
        velocity = Vector3.zero;
        Vector3 p = target.position + offset;
        p.z = transform.position.z;
        transform.position = Clamp(p);
        lastTargetPosition = target.position;
    }

    private Vector3 Clamp(Vector3 p)
    {
        if (bounds == null || cam == null || !cam.orthographic)
            return p;

        Rect r = bounds.Bounds;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // If the level is narrower than the view, centre on it instead of clamping.
        p.x = r.width <= halfW * 2f ? r.center.x : Mathf.Clamp(p.x, r.xMin + halfW, r.xMax - halfW);
        p.y = r.height <= halfH * 2f ? r.center.y : Mathf.Clamp(p.y, r.yMin + halfH, r.yMax - halfH);
        return p;
    }
}
