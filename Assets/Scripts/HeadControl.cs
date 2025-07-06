using System.Collections;
using UnityEngine;

public class HeadControl : MonoBehaviour
{
    [Header("Grapple components")]
    // Grapple components
    [SerializeField] private DistanceJoint2D grappleJoint;
    [SerializeField] private LineRenderer grappleLine;
    [SerializeField] private GameObject grappleAnchorPrefab; // This is needed to keep shell collisons with environment when grappled
    private GameObject currentAnchorInstance;
    [SerializeField] private LayerMask grappleLayerMask;

    [SerializeField] private Material grappleLineMaterial;
    [SerializeField] private PhysicsMaterial2D defaultMaterial;
    [SerializeField] private PhysicsMaterial2D grappledMaterial;

    [Header("Shell components")]
    // Shell components
    private Rigidbody2D rb; // The shell's rigidbody
    private Collider2D shellCollider;
    private HeadControl headControl;

    [Header("Grapple settings")]
    // Variables for actions
    //// Grapple
    private bool isFiringGrapple = false;
    private bool isRetractingGrapple = false;
    private bool grappleHit = false;

    private bool _isGrappled = false;
    public bool isGrappled => _isGrappled;

    private float grappleCurrentLength = 0f;
    private Vector2 grappleDirection;
    private Vector2 grappleEnd;

    [SerializeField] private int segments = 10; // Number of points along the grapple line
    [SerializeField] private float grappleSpeed = 20f; // Extension speed
    [SerializeField] private float grappleRetractionSpeed = 20f; // Retraction speed
    [SerializeField] private float grappleMaxLength = 8f; // Perhaps adjust this by set factor for Quick Pull
    private float grappleMinLength = 0f;
    [SerializeField] private float grappleAdjustSpeed = 7f;

    [Header("Grapple wiggle settings")]
    // Settings controlling how the grapple behaves when it first attaches
    [SerializeField] private float initialWobbleAmplitude = 0.15f;
    [SerializeField] private float initialWobbleFrequency = 20f;
    [SerializeField] private float wobbleDecayDuration = 0.5f;


    private float wobbleElapsed = 0f;
    private bool isWobbling = false;

    [Header("Quick pull settings")]
    //// Quick pull
    [SerializeField] private float quickPullSpeed = 10f;
    [SerializeField] private float quickPullExtendSpeed = 30f;

    private bool isQuickPulling = false;
    private bool quickPullActive = false; // Only when joint is connected during quick pull
    private Vector2 quickPullTarget;

    // Momentum conservation
    [SerializeField] private int momentumCarryFrames = 8;
    [SerializeField] private float momentumDecay = 0.9f; // per frame multiplier
    private int momentumFramesRemaining = 0;
    private Vector2 cachedMomentum = Vector2.zero;

    // Make quickpull start slow and reach maximum by mid point
    [SerializeField] private float quickPullDuration = 0.6f;
    [SerializeField] private AnimationCurve quickPullSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private float quickPullElapsed = 0f;


    [Header("Jump settings")]
    //// Jump
    [SerializeField] private float jumpForce = 10f;


    void Awake()
    {
        headControl = GetComponent<HeadControl>();
        // Initialise shell materials
        rb = GetComponent<Rigidbody2D>();
        shellCollider = GetComponent<Collider2D>();
        shellCollider.sharedMaterial = defaultMaterial;

        InitialiseGrapple();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isFiringGrapple)
            StartStandardGrapple();

        if (Input.GetMouseButtonUp(0))
            StartRetraction();

        if (Input.GetMouseButtonDown(1))
            StartQuickPull();

        if (Input.GetMouseButtonUp(1))
            StartRetraction();

        if (Input.GetKeyDown(KeyCode.Space))
            PerformJump();
    }

    void FixedUpdate()
    {
        if (isFiringGrapple && !grappleHit)
            AnimateGrappleExtension();

        if (isRetractingGrapple)
            AnimateGrappleRetraction();

        UpdateLineRenderer();
        HandleGrappleAdjust();
        HandleQuickPull();

        // Adds decaying force in cached direction when quick pull is released. // CHECK IF ACTUALLY BEING USED
        if (momentumFramesRemaining > 0)
        {
            rb.AddForce(cachedMomentum * (Mathf.Pow(momentumDecay, momentumCarryFrames - momentumFramesRemaining)), ForceMode2D.Force);
            momentumFramesRemaining--;
        }

        if (isWobbling)
        {
            wobbleElapsed += Time.fixedDeltaTime;
            if (wobbleElapsed >= wobbleDecayDuration)
                isWobbling = false;
        }
    }

    // ----------------------- Core Mechanics -------------------------

    private void StartStandardGrapple()
    {
        SetupGrappleDirection();
        StartLineAnimation();
    }

    private void StartQuickPull()
    {
        isQuickPulling = true; // Reflects player's intent to quick pull, not that it the physics are happening yet.
        SetupGrappleDirection();
        PerformQuickPull();
    }

    private void PerformJump()
    {
        SetupGrappleDirection();
        StartLineAnimation();
        rb.AddForce(-grappleDirection * jumpForce, ForceMode2D.Impulse);
    }

    // ----------------------- Helpers -------------------------

    private void InitialiseGrapple()
    {
        // Grapple JOINT init
        grappleJoint = GetComponent<DistanceJoint2D>();
        grappleJoint.enabled = false;
        grappleJoint.autoConfigureConnectedAnchor = false;

        // Grapple LINE init
        grappleLine.enabled = false; // Disable, otherwise spawns to centre of scene
        // Init all segment positions to players current position to avoid frame-1 interpolation glitches
        grappleLine.positionCount = segments;
        for (int i = 0; i < segments; i++)
            grappleLine.SetPosition(i, rb.position);

        grappleLine.material = grappleLineMaterial;
        grappleLine.textureMode = LineTextureMode.Tile;

        grappleLine.numCapVertices = 8; // Rounded start and end 
    }

    void SetupGrappleDirection()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        grappleDirection = (mouseWorld - rb.position).normalized;
        grappleEnd = rb.position;
        grappleCurrentLength = 0f;
    }

    private void StartLineAnimation()
    {
        isFiringGrapple = true;
        grappleHit = false;

        // Initialise all segment positions to the same starting point (player position) to avoid frame of incorrect LineRenderer
        Vector3 startPos = rb.position;
        for (int i = 0; i < segments; i++)
            grappleLine.SetPosition(i, startPos);

        grappleLine.enabled = true;
    }

    private void AnimateGrappleExtension()
    {

        float extendSpeedToUse = isQuickPulling ? quickPullExtendSpeed : grappleSpeed;
        float extendAmount = extendSpeedToUse * Time.fixedDeltaTime;
        grappleCurrentLength += extendAmount;

        if (grappleCurrentLength >= grappleMaxLength)
        {
            CancelGrapple();
            return;
        }

        grappleEnd = (Vector2)rb.position + grappleDirection * grappleCurrentLength;

        RaycastHit2D hit = Physics2D.Raycast(rb.position, grappleDirection, grappleCurrentLength, grappleLayerMask);

        if (hit.collider != null)
        {
            // Debug.Log("Grappled to: " + hit.collider.name);
            grappleHit = true;
            isFiringGrapple = false;

            RemakeAnchor(hit.point);

            // Wait until next FixedUpdate to ensure proper RigidBody2D initialisation
            var anchorRb = currentAnchorInstance.GetComponent<Rigidbody2D>();

            // Delay setting the connectedBody by one FixedUpdate
            StartCoroutine(EnableGrappleNextPhysicsFrame(anchorRb, hit.point));

            if (isQuickPulling)
            {
                quickPullTarget = hit.point;
                quickPullElapsed = 0f;
            }
        }
    }

    private void AnimateGrappleRetraction()
    {
        float retractSpeed = grappleRetractionSpeed;
        grappleCurrentLength -= retractSpeed * Time.fixedDeltaTime;

        if (grappleCurrentLength <= 0f)
        {
            // Set to 0 to prevent negative length
            grappleCurrentLength = 0f;

            CancelGrapple();
            return;
        }

        grappleEnd = (Vector2)rb.position + grappleDirection * grappleCurrentLength;
    }

    // Not private in case want environment or something to cancel grapple.
    public void CancelGrapple()
    {

        if (grappleLine.enabled && !isRetractingGrapple)
        {
            isRetractingGrapple = true;
            return; // Delay reset until retraction finishes.
        }
        // Capture the current momentum
        cachedMomentum = rb.velocity;
        momentumFramesRemaining = momentumCarryFrames;

        // Disable the joint
        grappleJoint.enabled = false;
        grappleJoint.connectedBody = null;

        if (currentAnchorInstance != null)
        {
            Destroy(currentAnchorInstance);
            currentAnchorInstance = null;
        }

        grappleLine.enabled = false;
        grappleHit = false;
        isFiringGrapple = false;
        isQuickPulling = false;
        quickPullActive = false;
        _isGrappled = false;
        isRetractingGrapple = false;

        // Switch back to default material
        shellCollider.sharedMaterial = defaultMaterial;
    }

    // Waits until next FixedUpdate (frame) before enabling lineRenderer to avoid flickering.
    private IEnumerator EnableGrappleNextPhysicsFrame(Rigidbody2D anchorRb, Vector2 hitPoint)
    {
        yield return new WaitForFixedUpdate();

        grappleJoint.connectedBody = anchorRb;
        grappleJoint.autoConfigureConnectedAnchor = false;
        grappleJoint.distance = Vector2.Distance(rb.position, hitPoint);
        grappleJoint.enabled = true;
        grappleLine.SetPosition(1, hitPoint);
        _isGrappled = true;

        // Start dynamic rope wobble
        isWobbling = true;
        wobbleElapsed = 0f;

        // Change shell material to make more bouncy when attached
        shellCollider.sharedMaterial = grappledMaterial;

        if (isQuickPulling)
            quickPullActive = true; // Trigger quick pull physics
    }

    private void UpdateLineRenderer()
    {
        // Only runs when grappleLine
        if (!grappleLine.enabled)
            return;

        // Endpoint = anchor if grapple hit, otherwise max length of grapple.
        Vector3 endPoint = (grappleHit && grappleJoint.connectedBody != null)
            ? (Vector3)grappleJoint.connectedBody.position
            : grappleEnd;

        // Direction of the rope
        Vector3 startPoint = rb.position;
        Vector3 direction = (endPoint - startPoint).normalized;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);

            // Direction perpendicular to grapple vector
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized;

            float wave = 0f;

            if (isFiringGrapple || isRetractingGrapple)
            {
                // Constant wobble during extension
                float baseAmplitude = 0.075f;
                float baseFrequency = 10f;
                wave = Mathf.Sin(Time.time * baseFrequency + t * baseFrequency) * baseAmplitude; // Add sinusoidal wobble perpendicular to rope

            }
            else if (isWobbling)
            {
                float wobbleAmplitude = Mathf.Lerp(initialWobbleAmplitude, 0f, wobbleElapsed / wobbleDecayDuration);
                float wobbleFrequency = Mathf.Lerp(initialWobbleFrequency, 0f, wobbleElapsed / wobbleDecayDuration);
                wave = Mathf.Sin(Time.time * wobbleFrequency + t * wobbleFrequency) * wobbleAmplitude;
            }

            point += perpendicular * wave;
            grappleLine.SetPosition(i, point);
        }
    }

    private void PerformQuickPull()
    {
        SetupGrappleDirection();
        StartLineAnimation();
    }

    private void HandleGrappleAdjust()
    // Adjust grapple length with keys
    {
        // Only possible if grapple attached and player isn't using quickPull
        if (isQuickPulling || !grappleJoint.enabled || !grappleHit)
            return;

        if (Input.GetKey("w"))
        {
            grappleJoint.distance = Mathf.Max(grappleMinLength, grappleJoint.distance - grappleAdjustSpeed * Time.fixedDeltaTime);
        }
        else if (Input.GetKey("s"))
        {
            grappleJoint.distance = Mathf.Min(grappleMaxLength, grappleJoint.distance + grappleAdjustSpeed * Time.fixedDeltaTime);
        }
    }

    private void HandleQuickPull()
    {
        // Only if quick pulling and attached
        if (!quickPullActive || !grappleJoint.enabled || !grappleHit || currentAnchorInstance == null)
            return;

        // Not currently used but may do
        float currentDistanceToAnchor = Vector2.Distance(rb.position, grappleJoint.connectedBody.position);

        quickPullElapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(quickPullElapsed / quickPullDuration);
        float speedMultiplier = quickPullSpeedCurve.Evaluate(t);

        // Optional: map curve output to speed value
        float dynamicSpeed = quickPullSpeed * speedMultiplier;

        grappleJoint.distance = Mathf.Max(grappleMinLength, grappleJoint.distance - dynamicSpeed * Time.fixedDeltaTime);


        // grappleJoint.distance = Mathf.Min(grappleMaxLength, grappleJoint.distance - quickPullSpeed * Time.fixedDeltaTime);

    }

    private void RemakeAnchor(Vector2 target)
    {
        // Destroy any previuos grapple anchor prefab
        if (currentAnchorInstance != null)
            Destroy(currentAnchorInstance);
        // Create a new anchor at the target
        currentAnchorInstance = Instantiate(grappleAnchorPrefab, target, Quaternion.identity);
    }

    private void StartRetraction()
    {
        if (!grappleLine.enabled || isRetractingGrapple)
            return;

        isRetractingGrapple = true;
        isFiringGrapple = false;
        grappleHit = false;
        _isGrappled = false;

        // Disable joint if connected
        if (grappleJoint.enabled)
        {
            grappleJoint.enabled = false;
            grappleJoint.connectedBody = null;
        }

        if (currentAnchorInstance != null)
        {
            Destroy(currentAnchorInstance);
            currentAnchorInstance = null;
        }

        // Revert shell material just in case
        shellCollider.sharedMaterial = defaultMaterial;
    }

}
