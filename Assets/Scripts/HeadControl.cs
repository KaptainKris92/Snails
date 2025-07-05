using System.Collections;
using UnityEngine;

public class HeadControl : MonoBehaviour
{
    public DistanceJoint2D grappleJoint;
    public LineRenderer grappleLine;
    public GameObject grappleAnchorPrefab; // This is needed to keep shell collisons with environment when grappled
    private GameObject currentAnchorInstance;
    public float grappleSpeed = 20f;
    public float grappleMaxLength = 10f;
    public LayerMask grappleLayerMask;

    private Rigidbody2D rb; // The shell's rigidbody
    private bool isFiringGrapple = false;
    private bool grappleHit = false;
    private float grappleCurrentLength = 0f;
    private Vector2 grappleDirection;
    private Vector2 grappleEnd;
    [SerializeField] private Material grappleLineMaterial;
    [SerializeField] private PhysicsMaterial2D defaultMaterial;
    [SerializeField] private PhysicsMaterial2D grappledMaterial;

    private Collider2D shellCollider;

    [SerializeField] private int segments = 10; // Number of points along the grapple line

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Initialise grapple joint
        AwakeGrapple();

        // Initialise shell materials
        shellCollider = GetComponent<Collider2D>();
        shellCollider.sharedMaterial = defaultMaterial;

    }

    private void AwakeGrapple()
    {
        grappleJoint = GetComponent<DistanceJoint2D>();
        grappleJoint.enabled = false;
        grappleJoint.autoConfigureConnectedAnchor = false;

        grappleLine.enabled = false; // Disable, otherwise spawns to centre of scene


        // Initialise all positions to players current position to avoid frame-1 interpolation glitches
        grappleLine.positionCount = segments; // Necessary?
        for (int i = 0; i < segments; i++)
        {
            grappleLine.SetPosition(i, rb.position);
        }

        grappleLine.material = grappleLineMaterial;
        grappleLine.textureMode = LineTextureMode.Tile;

        // Rounded start and end 
        grappleLine.numCapVertices = 8;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isFiringGrapple)
        {
            isFiringGrapple = true;
            grappleHit = false;

            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            grappleDirection = (mouseWorld - rb.position).normalized;
            grappleEnd = rb.position;
            grappleCurrentLength = 0f;

            // Set up segment count
            grappleLine.positionCount = segments;

            // Initialise all segment positions to the same starting point (player position) to avoid frame of incorrect LineRenderer
            Vector3 startPos = rb.position;
            for (int i = 0; i < segments; i++)
            {
                grappleLine.SetPosition(i, startPos);
            }

            grappleLine.enabled = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            CancelGrapple();
        }
    }

    void FixedUpdate()
    {
        if (isFiringGrapple && !grappleHit)
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
            Debug.DrawLine(rb.position, grappleEnd, Color.green, 0.1f);

            if (hit.collider != null)
            {
                Debug.Log("Grappled to: " + hit.collider.name);
                grappleHit = true;
                isFiringGrapple = false;

                // Destroy any previuos grapple anchor prefab
                if (currentAnchorInstance != null)
                    Destroy(currentAnchorInstance);

                // Create a new anchor at the hit point
                currentAnchorInstance = Instantiate(grappleAnchorPrefab, hit.point, Quaternion.identity);

                // Wait until next FixedUpdate to ensure proper RigidBody2D initialisation
                Rigidbody2D anchorRb = currentAnchorInstance.GetComponent<Rigidbody2D>();
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

        // When grapple line is attached
        if (grappleLine.enabled)
        {
            grappleLine.positionCount = segments;
            // Determine the current target endpoint
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

        // Extend/retract grapple if it's active
        if (grappleJoint.enabled && grappleHit)
        {
            float adjustSpeed = 5f; // units per second
            float minDistance = 1f; // avoid collapsing the joint
            float maxDistance = grappleMaxLength;

            if (Input.GetKey("w"))
            {
                grappleJoint.distance = Mathf.Max(minDistance, grappleJoint.distance - adjustSpeed * Time.fixedDeltaTime);
            }
            else if (Input.GetKey("s"))
            {
                grappleJoint.distance = Mathf.Min(maxDistance, grappleJoint.distance + adjustSpeed * Time.fixedDeltaTime);
            }
        }

    }

    // Not private in case want environment or something to cancel grapple.
    void CancelGrapple()
    {
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
}