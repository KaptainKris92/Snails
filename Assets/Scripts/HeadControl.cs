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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        grappleJoint = GetComponent<DistanceJoint2D>();
        grappleJoint.enabled = false;
        grappleJoint.autoConfigureConnectedAnchor = false;

        grappleLine.enabled = false;
        grappleLine.positionCount = 2;
        grappleLine.SetPosition(0, Vector3.zero);
        grappleLine.SetPosition(1, Vector3.zero);
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

                // Connect DistanceJoint2D to the anchor's RigidBody2D
                grappleJoint.connectedBody = anchorRb;
                grappleJoint.autoConfigureConnectedAnchor = false;                
                grappleJoint.distance = Vector2.Distance(rb.position, hit.point);
                grappleJoint.enabled = true;

                grappleLine.SetPosition(1, hit.point);
            }
        }

        if (grappleLine.enabled)
        {
            grappleLine.SetPosition(0, rb.position);

            if (grappleHit && grappleJoint.connectedBody != null)
            {
                grappleLine.SetPosition(1, grappleJoint.connectedBody.position);
            }
            else
            {
                // Still firing or retracting
                grappleLine.SetPosition(1, grappleEnd);
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
    }
}
