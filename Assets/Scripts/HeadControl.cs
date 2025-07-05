using System.Collections;
using UnityEngine;

public class HeadControl : MonoBehaviour
{
    // Grapple components
    [SerializeField] private DistanceJoint2D grappleJoint;
    [SerializeField] private LineRenderer grappleLine;
    [SerializeField] private GameObject grappleAnchorPrefab; // This is needed to keep shell collisons with environment when grappled
    private GameObject currentAnchorInstance;
    [SerializeField] private LayerMask grappleLayerMask;

    [SerializeField] private Material grappleLineMaterial;
    [SerializeField] private PhysicsMaterial2D defaultMaterial;
    [SerializeField] private PhysicsMaterial2D grappledMaterial;

    // Shell components
    private Rigidbody2D rb; // The shell's rigidbody
    private Collider2D shellCollider;

    // Variables for actions
    //// Grapple
    private bool isFiringGrapple = false;
    private bool grappleHit = false;
    private float grappleCurrentLength = 0f;
    private Vector2 grappleDirection;
    private Vector2 grappleEnd;

    [SerializeField] private int segments = 10; // Number of points along the grapple line
    [SerializeField] private float grappleSpeed = 20f;
    [SerializeField] private float grappleMaxLength = 8f; // Perhaps adjust this by set factor for Quick Pull
    private float grappleMinLength = 0f;
    [SerializeField] private float grappleAdjustSpeed = 7f;

    [SerializeField] private float releaseBoost = 1f; // Applies to Quick pull too    

    //// Quick pull
    [SerializeField] private float quickPullSpeed = 10f;
    
    private bool isQuickPulling = false;
    private Vector2 quickPullTarget;

    //// Jump
    [SerializeField] private float jumpForce = 10f;

    void Awake()
    {
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
            CancelGrapple();

        if (Input.GetMouseButtonDown(1))
            StartQuickPull();

        if (Input.GetMouseButtonUp(1))
            CancelGrapple();

        if (Input.GetKeyDown(KeyCode.Space))
                PerformJump();
    }

    void FixedUpdate()
    {
        if (isFiringGrapple && !grappleHit)
            AnimateGrappleExtension();

        UpdateLineRenderer();
        HandleGrappleAdjust();
        HandleQuickPull();
    }

    // ----------------------- Core Mechanics -------------------------

    private void StartStandardGrapple()
    {
        SetupGrappleDirection();
        StartLineAnimation();
    }

    private void StartQuickPull()
    {
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
        float extendAmount = grappleSpeed * Time.fixedDeltaTime;
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
            Debug.Log("Grappled to: " + hit.collider.name);
            grappleHit = true;
            isFiringGrapple = false;

            RemakeAnchor(hit.point);

            // Wait until next FixedUpdate to ensure proper RigidBody2D initialisation
            var anchorRb = currentAnchorInstance.GetComponent<Rigidbody2D>();
            if (anchorRb == null)
            {
                Debug.LogError("GrappleAnchor prefab is missing a Rigidbody2D component!");
                return;
            }

            // Delay setting the connectedBody by one FixedUpdate
            StartCoroutine(EnableGrappleNextPhysicsFrame(anchorRb, hit.point));

            // Change shell material to make more bouncy when attached
            shellCollider.sharedMaterial = grappledMaterial;
        }
    }

    // Not private in case want environment or something to cancel grapple.
    public void CancelGrapple()
    {

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

            // Add sinusoidal wobble perpendicular to rope
            float wobbleAmplitude = 0.05f;
            float wave = Mathf.Sin(Time.time * 10f + t * 10f) * wobbleAmplitude;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized; // 2D perpendicular
            point += perpendicular * wave;

            grappleLine.SetPosition(i, point);
        }
    }

    private void PerformQuickPull()
    {
        SetupGrappleDirection();

        RaycastHit2D hit = Physics2D.Raycast(rb.position, grappleDirection, grappleMaxLength, grappleLayerMask);
        if (hit.collider != null)
        {
            quickPullTarget = hit.point;
            grappleHit = true;
            grappleEnd = quickPullTarget; // Used by lineRender

            RemakeAnchor(quickPullTarget);

            var anchorRb = currentAnchorInstance.GetComponent<Rigidbody2D>();
            // Can probably remove these debugs in the code now.
            if (anchorRb == null)
            {
                Debug.LogError("GrappleAnchor prefab is missing Rigidbody2D!");
                return;
            }

            // Connect to anchor using DistanceJoint2D, same as grapple but shorten automatically.
            grappleJoint.connectedBody = anchorRb;
            grappleJoint.distance = Vector2.Distance(rb.position, quickPullTarget);
            grappleJoint.autoConfigureConnectedAnchor = false;
            grappleJoint.enabled = true;

            StartLineAnimation();
            isQuickPulling = true;
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
        if (!isQuickPulling || !grappleJoint.enabled || !grappleHit)
            return;

        // Not currently used but may do
        float currentDistanceToAnchor = Vector2.Distance(rb.position, grappleJoint.connectedBody.position);

        grappleJoint.distance = Mathf.Min(grappleMaxLength, grappleJoint.distance - quickPullSpeed * Time.fixedDeltaTime);
        
    }

    private void RemakeAnchor(Vector2 target)
    {
        // Destroy any previuos grapple anchor prefab
        if (currentAnchorInstance != null)
            Destroy(currentAnchorInstance);
        // Create a new anchor at the target
        currentAnchorInstance = Instantiate(grappleAnchorPrefab, target, Quaternion.identity);
    }

}
