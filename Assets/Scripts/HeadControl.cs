using System.Collections;
using Unity.VisualScripting;
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

    private bool inputLocked = false; // To prevent player from holding input & pressing reset to continue existing move

    [Header("Shell components")]
    // Shell components
    private Rigidbody2D rb; // The shell's rigidbody
    private Collider2D shellCollider;
    private HeadControl headControl; // Used by PlayerMovement?

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

    // Make quickpull start slow and reach maximum by mid point
    [SerializeField] private float quickPullDuration = 0.6f;
    [SerializeField] private AnimationCurve quickPullSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private float quickPullElapsed = 0f;
    private float lastDistanceToAnchor = Mathf.Infinity;
    [SerializeField] private float quickPullGraceTime = 0.15f; // So that quick pull works on initial attach if player is still
    [SerializeField] private float quickPullStuckTolerance = 0.02f;

    [SerializeField] private float pullBoostFactor = 0.8f; // For adding 'pull' momentum upon quickpull release


    [Header("Jump settings")]
    //// Jump
    [SerializeField] private float jumpMaxLength = 10f;
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField] private AnimationCurve jumpPushCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool isJumping = false;
    private float jumpElapsed = 0f;
    private bool jumpActive = false;


    [Header("Gravity settings")]
    private float baseGravityScale = 1.0f;
    // [SerializeField] private float upwardGravityMultiplifer = 0.5f; // Lower = floatier
    [SerializeField] private float minUpwardGravityScale = 0.2f; // Prevent zero gravity
    [SerializeField] private float upwardVelocitySmoothing = 10f; // Higher = gravity decays slower on fast pulls


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
        if (inputLocked) return; // Don't count any of the key-presses if input is locked for 0.1s after reset.

        if (Input.GetMouseButtonDown(0) && !isFiringGrapple)
            StartStandardGrapple();

        if (Input.GetMouseButtonUp(0))
            StartRetraction();

        if (Input.GetMouseButtonDown(1))
            StartQuickPull();

        if (Input.GetMouseButtonUp(1))
            StartRetraction();

        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            StartJump();

        if (Input.GetKeyUp(KeyCode.Space))
            StartRetraction();
    }

    void FixedUpdate()
    {
        AdjustGravity();

        if (isFiringGrapple && !grappleHit)
            AnimateGrappleExtension();

        if (isRetractingGrapple)
            AnimateGrappleRetraction();

        UpdateLineRenderer();
        HandleGrappleAdjust();
        HandleQuickPull();
        HandleJumpPush();

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

    private void StartJump()
    {
        if (inputLocked) return; // Just in case jump still persists while holding space + R (can remove later probably)
        isJumping = true;
        SetupGrappleDirection();
        StartLineAnimation();
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
            grappleHit = true;
            isFiringGrapple = false;

            RemakeAnchor(hit.point);

            // Wait until next FixedUpdate to ensure proper RigidBody2D initialisation
            var anchorRb = currentAnchorInstance.GetComponent<Rigidbody2D>();

            // Delay setting the connectedBody by one FixedUpdate
            StartCoroutine(EnableGrappleNextPhysicsFrame(anchorRb, hit.point));

            if (isQuickPulling)
            {
                quickPullElapsed = 0f;
                lastDistanceToAnchor = Vector2.Distance(rb.position, hit.point);
            }

            if (isJumping)
            {
                jumpElapsed = 0f;
                lastDistanceToAnchor = Vector2.Distance(rb.position, hit.point);
                jumpActive = true;
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

    private void AdjustGravity()
    {
        float verticalVelocity = rb.velocity.y;

        if (verticalVelocity > 0f)
        {
            // When player moving upwards, reduce gravity based on player's speed
            float dynamicScale = Mathf.Lerp(minUpwardGravityScale, baseGravityScale, verticalVelocity / upwardVelocitySmoothing);
            rb.gravityScale = Mathf.Clamp(dynamicScale, minUpwardGravityScale, baseGravityScale);
        }
        else // Whenever falling or stationary
        {
            rb.gravityScale = baseGravityScale;
        }
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

        float currentDistanceToAnchor = Vector2.Distance(rb.position, grappleJoint.connectedBody.position);

        // Elapsed time only increased if player actively moving closer to point
        if (quickPullElapsed < quickPullGraceTime || currentDistanceToAnchor < lastDistanceToAnchor - quickPullStuckTolerance) // 0.01f is tolerance
        {
            quickPullElapsed += Time.fixedDeltaTime;
        }

        lastDistanceToAnchor = currentDistanceToAnchor;

        float t = Mathf.Clamp01(quickPullElapsed / quickPullDuration);
        float speedMultiplier = quickPullSpeedCurve.Evaluate(t);
        float dynamicSpeed = quickPullSpeed * speedMultiplier; // Map curve output to speed value

        grappleJoint.distance = Mathf.Max(grappleMinLength, grappleJoint.distance - dynamicSpeed * Time.fixedDeltaTime);
    }

    private void HandleJumpPush()
    {
        if (!jumpActive || !grappleJoint.enabled || !grappleHit || currentAnchorInstance == null)
            return;

        float currentDistanceToAnchor = Vector2.Distance(rb.position, grappleJoint.connectedBody.position);

        // If we’re not at full length and still holding jump, push outward
        if (currentDistanceToAnchor < jumpMaxLength && Input.GetKey(KeyCode.Space))
        {
            jumpElapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(jumpElapsed / jumpDuration);
            float speedMultiplier = jumpPushCurve.Evaluate(t);
            float dynamicSpeed = quickPullSpeed * speedMultiplier;

            grappleJoint.distance = Mathf.Min(jumpMaxLength, grappleJoint.distance + dynamicSpeed * Time.fixedDeltaTime);
            lastDistanceToAnchor = currentDistanceToAnchor;
        }
        else
        {
            // Otherwise retract
            StartRetraction();
        }
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

        // Inject impulse momentum immediately if quick pulling
        if (quickPullActive && currentAnchorInstance != null)
        {

            Vector2 anchorPos = currentAnchorInstance.transform.position;
            Vector2 toAnchor = (anchorPos - rb.position).normalized;

            // Project velocity toward anchor
            float currentDistanceToAnchor = Vector2.Distance(rb.position, grappleJoint.connectedBody.position);
            float radialSpeed = (lastDistanceToAnchor - currentDistanceToAnchor) / Time.fixedDeltaTime;

            // If moving towards the anchor, inject a bit of impulse in that direction
            if (radialSpeed > 0f)
            {
                Vector2 impulse = toAnchor * radialSpeed * pullBoostFactor;
                rb.AddForce(impulse, ForceMode2D.Impulse);
            }
            else
            {
                Debug.Log("[QuickPullRelease] Radial speed too low to apply impulse.");
            }
        }

        if (jumpActive && currentAnchorInstance != null)
        {

            Vector2 anchorPos = currentAnchorInstance.transform.position;
            Vector2 awayFromAnchor = (rb.position - anchorPos).normalized;

            // Project velocity toward anchor
            float currentDistanceToAnchor = Vector2.Distance(rb.position, grappleJoint.connectedBody.position);
            float radialSpeed = (currentDistanceToAnchor - lastDistanceToAnchor) / Time.fixedDeltaTime;

            // If moving towards the anchor, inject a bit of impulse in that direction
            if (radialSpeed > 0f)
            {
                Vector2 impulse = awayFromAnchor * radialSpeed * pullBoostFactor; // Maybe create jumpBoostFactor if having separate factors feels better.
                rb.AddForce(impulse, ForceMode2D.Impulse);
            }
            else
            {
                Debug.Log("[JumpRelease] Radial speed too low to apply impulse.");
            }
        }

        isRetractingGrapple = true;
        isFiringGrapple = false;
        grappleHit = false;
        _isGrappled = false;
        // Maybe move jumping bools to start.
        isJumping = false;
        jumpActive = false;

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

    public void ResetState()
    {
         // Fully cancel grapple and joint (instead of using CancelGrapple() because it reacts too slowly it seems)
        if (grappleJoint != null)
        {
            grappleJoint.enabled = false;
            grappleJoint.connectedBody = null;
        }

        if (currentAnchorInstance != null)
        {
            Destroy(currentAnchorInstance);
            currentAnchorInstance = null;
        }

        // Cancel LineRenderer
        if (grappleLine != null)
        {
            grappleLine.enabled = false;
        }

        // Reset all action flags
        isFiringGrapple = false;
        isRetractingGrapple = false;
        grappleHit = false;
        _isGrappled = false;
        isQuickPulling = false;
        quickPullActive = false;
        isJumping = false;
        jumpActive = false;

        // Reset timers
        quickPullElapsed = 0f;
        jumpElapsed = 0f;
        wobbleElapsed = 0f;
        isWobbling = false;

        // Reset material
        if (shellCollider != null)
            shellCollider.sharedMaterial = defaultMaterial;

        inputLocked = true;
        StartCoroutine(UnlockInputAfterDelay(0.1f)); // Unlock input after 0.5s
    }

    private IEnumerator UnlockInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        inputLocked = false;
    }

}
