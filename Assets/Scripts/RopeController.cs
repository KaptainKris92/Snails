using UnityEngine;

/// The grapple rope. One rope, one explicit state:
///
///   Idle -> Firing -> Attached -> Idle
///                  \-> Missed  -> Idle
///
/// Input is read in Update into a small buffer and consumed in FixedUpdate, so a press is never
/// lost between physics steps. While attached the shell hangs from a rigid DistanceJoint2D whose
/// connectedAnchor is a world-space point (the last pivot); the rope bends around terrain corners
/// by adding pivots and unwraps when the shell swings back.
[RequireComponent(typeof(Rigidbody2D), typeof(DistanceJoint2D), typeof(CircleCollider2D))]
public class RopeController : MonoBehaviour
{
    public enum RopeState { Idle, Firing, Attached, Missed }

    [SerializeField] private RopeSettings settings;
    [SerializeField] private LineRenderer line;
    [SerializeField] private LayerMask terrainMask;
    [SerializeField] private PhysicsMaterial2D freeMaterial;
    [SerializeField] private PhysicsMaterial2D ropedMaterial;

    public RopeState State { get; private set; } = RopeState.Idle;
    public bool IsAttached => State == RopeState.Attached;

    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private CircleCollider2D shell;

    // Input buffer: written in Update, consumed in FixedUpdate.
    private float firePressedAt = float.NegativeInfinity;
    private Vector2 fireAimWorld;
    private bool fireHeld;
    private float hopPressedAt = float.NegativeInfinity;
    private float reel;       // +1 reel in (W), -1 reel out (S)
    private float horizontal; // -1..1 (A/D)

    // Rope state
    private Vector2 fireDirection;
    private float tipLength; // distance of the visual tip from the shell while Firing / Missed
    private readonly RopePivots pivots = new RopePivots();
    private Vector3[] linePoints;

    private const float WrapCastShortening = 0.05f; // stop the wrap raycast just short of the pivot

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        joint = GetComponent<DistanceJoint2D>();
        shell = GetComponent<CircleCollider2D>();

        joint.enabled = false;
        joint.connectedBody = null;
        joint.autoConfigureConnectedAnchor = false;
        joint.autoConfigureDistance = false;
        joint.maxDistanceOnly = false; // rigid tether: full loops over the anchor are allowed

        linePoints = new Vector3[settings.maxPivots + 2];
        line.useWorldSpace = true;
        line.enabled = false;

        shell.sharedMaterial = freeMaterial;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            firePressedAt = Time.unscaledTime;
            fireAimWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        fireHeld = Input.GetMouseButton(0);
        if (Input.GetKeyDown(KeyCode.Space))
            hopPressedAt = Time.unscaledTime;

        reel = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        horizontal = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate()
    {
        ConsumeFireInput();

        switch (State)
        {
            case RopeState.Firing: StepFiring(); break;
            case RopeState.Attached: StepAttached(); break;
            case RopeState.Missed: StepMissed(); break;
        }

        ApplyHorizontalInput();
        ConsumeHopInput();
    }

    void LateUpdate()
    {
        DrawRope();
    }

    // ---------------------------------------------------------------- input

    private void ConsumeFireInput()
    {
        if (settings.holdToStayAttached && !fireHeld)
        {
            // Button is up: let go of whatever the rope is doing, and drop any press that was
            // already released before it could be consumed.
            if (State == RopeState.Attached) Release();
            else if (State == RopeState.Firing) State = RopeState.Missed;
            firePressedAt = float.NegativeInfinity;
            return;
        }

        bool pressBuffered = Time.unscaledTime - firePressedAt <= settings.inputBuffer;
        if (!pressBuffered)
            return;
        firePressedAt = float.NegativeInfinity;

        if (State == RopeState.Attached)
        {
            // Toggle mode: a second press lets go. (In hold mode the button is already down while
            // attached, so a new press cannot arrive here.)
            Release();
            return;
        }

        // Idle, Missed or still Firing: fire (a re-fire replaces the shot in flight).
        StartFiring(fireAimWorld);
    }

    private void ConsumeHopInput()
    {
        if (Time.unscaledTime - hopPressedAt > settings.inputBuffer)
            return;
        if (!IsGrounded())
            return; // keep the press buffered until it expires, so a press just before landing still hops

        hopPressedAt = float.NegativeInfinity;
        rb.velocity = new Vector2(rb.velocity.x, settings.groundHopVelocity);
    }

    private void ApplyHorizontalInput()
    {
        if (horizontal == 0f)
            return;

        float gMultiple = IsAttached ? settings.ropedHorizontalAccelG : settings.airHorizontalAccelG;
        float accel = gMultiple * Mathf.Abs(Physics2D.gravity.y);
        rb.AddForce(Vector2.right * (horizontal * accel * rb.mass));
    }

    // ---------------------------------------------------------------- states

    private void StartFiring(Vector2 aimWorld)
    {
        Vector2 dir = aimWorld - rb.position;
        fireDirection = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector2.up;
        tipLength = 0f;
        State = RopeState.Firing;
        line.enabled = true;
    }

    private void StepFiring()
    {
        tipLength += settings.ropeExtendSpeed * Time.fixedDeltaTime;
        if (tipLength >= settings.ropeMaxLength)
        {
            tipLength = settings.ropeMaxLength;
            State = RopeState.Missed;
            return;
        }

        RaycastHit2D hit = Physics2D.Raycast(rb.position, fireDirection, tipLength, terrainMask);
        if (hit.collider != null)
            Attach(hit.point);
    }

    private void Attach(Vector2 anchor)
    {
        pivots.Reset(anchor);

        // Joint enabled in the same physics step as the hit. Distance is the current distance, so
        // there is no snap; the joint only removes the radial velocity component.
        joint.connectedAnchor = anchor;
        joint.distance = Vector2.Distance(rb.position, anchor);
        joint.enabled = true;

        shell.sharedMaterial = ropedMaterial;
        State = RopeState.Attached;
    }

    private void StepAttached()
    {
        float dt = Time.fixedDeltaTime;

        if (reel > 0f)
        {
            joint.distance = Mathf.Max(settings.ropeMinLength, joint.distance - settings.reelSpeed * dt);
        }
        else if (reel < 0f)
        {
            float maxSegment = settings.ropeMaxLength - pivots.WrappedLength;
            if (joint.distance < maxSegment)
                joint.distance = Mathf.Min(maxSegment, joint.distance + settings.reelSpeed * dt);
        }

        UpdateWrapping();

        float maxSpeed = settings.maxRopedSpeed;
        if (rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;
    }

    private void Release()
    {
        // Velocity is left exactly as it is: no impulse, no boost.
        joint.enabled = false;
        shell.sharedMaterial = freeMaterial;
        pivots.Clear();
        State = RopeState.Idle;
        line.enabled = false;
    }

    private void StepMissed()
    {
        tipLength -= settings.ropeRetractSpeed * Time.fixedDeltaTime;
        if (tipLength <= 0f)
        {
            tipLength = 0f;
            State = RopeState.Idle;
            line.enabled = false;
        }
    }

    /// Called on a level reset: drop the rope and forget any buffered input.
    public void ResetRope()
    {
        Release();
        firePressedAt = float.NegativeInfinity;
        hopPressedAt = float.NegativeInfinity;
    }

    // ---------------------------------------------------------------- wrapping

    private void UpdateWrapping()
    {
        Vector2 body = rb.position;
        bool changed = false;

        // Unwrap first, so a freed pivot can immediately be re-tested for a new wrap below.
        for (int i = 0; i < pivots.Count; i++)
        {
            if (pivots.TryUnwrap(body) <= 0f)
                break;
            joint.distance = Vector2.Distance(body, pivots.Last);
            changed = true;
        }

        if (pivots.Count < settings.maxPivots)
        {
            Vector2 toPivot = pivots.Last - body;
            float distance = toPivot.magnitude;
            float castLength = distance - WrapCastShortening;
            if (castLength > settings.minWrapDistance)
            {
                RaycastHit2D hit = Physics2D.Raycast(body, toPivot / distance, castLength, terrainMask);
                if (hit.collider != null && hit.distance > settings.minWrapDistance)
                {
                    Vector2 pivot = RopeWrapMath.PivotFromHit(hit.point, hit.normal, settings.pivotSurfaceOffset);
                    pivots.Wrap(pivot, body);
                    joint.distance = Vector2.Distance(body, pivot); // current distance: no snap
                    changed = true;
                }
            }
        }

        if (changed)
            joint.connectedAnchor = pivots.Last;
    }

    // ---------------------------------------------------------------- helpers

    private bool IsGrounded()
    {
        float radius = shell.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        return Physics2D.CircleCast(rb.position, radius, Vector2.down, settings.groundCheckDistance, terrainMask).collider != null;
    }

    private void DrawRope()
    {
        if (!line.enabled)
            return;

        Vector3 body = transform.position; // interpolated render position, not rb.position
        int count;

        if (State == RopeState.Attached)
        {
            count = pivots.Count + 1;
            linePoints[0] = body;
            for (int i = 0; i < pivots.Count; i++)
                linePoints[1 + i] = pivots[pivots.Count - 1 - i];
        }
        else
        {
            count = 2;
            linePoints[0] = body;
            linePoints[1] = body + (Vector3)(fireDirection * tipLength);
        }

        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            linePoints[i].z = 0f;
            line.SetPosition(i, linePoints[i]);
        }
    }
}
