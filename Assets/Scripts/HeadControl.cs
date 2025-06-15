using UnityEngine;

public class HeadControl : MonoBehaviour
{
    public Transform shell;
    public Rigidbody2D rb;               // Snail head's rigidbody
    public float extendDistance = 2f;
    public float extendSpeed = 5f;
    public float retractSpeed = 7f;
    public float jumpForce = 10f;

    private float currentDistance = 0f;
    private bool extending = false;
    private bool retracting = false;
    private ActionSettings currentAction;
    private Rigidbody2D shellRb;


    // Grapple variables
    private FixedJoint2D fixedJoint;
    public DistanceJoint2D grappleJoint;
    public float grappleMaxDistance = 500f; // Refine later
    public LineRenderer grappleLine;
    private bool isFiringGrapple = false;
    private Vector2 grappleEnd;
    private Vector2 grappleDirection;
    [SerializeField] private float grappleSpeed = 20f;
    [SerializeField] private float grappleMaxLength = 10f;
    [SerializeField] private float grappleCurrentLength = 0f;
    private bool grappleHit = false;
    [SerializeField] private LayerMask grappleLayerMask;
    // A grapple anchor prefab with a rigidbody is used to ensure that the shell doesn't lose physics once connected to environment.
    public GameObject grappleAnchorPrefab;
    private GameObject currentAnchorInstance;
    // Variables for adjusting grapple length
    [SerializeField] private float grappleAdjustSpeed = 5f; // How fast to extend/retract. 
    [SerializeField] private float grappleMinDistance = 1f; // Avoid collapsing the joint completely

    public enum ActionType
    {
        Jump,
        QuickPull,
        Grapple // Not currently used
    }

    [System.Serializable]
    public struct ActionSettings
    {
        public ActionType type;
        public Vector2 direction;

    }

    void Awake()
    {
        shellRb = shell.GetComponent<Rigidbody2D>();

        // Get fixed joint and it's connected rb 
        fixedJoint = GetComponent<FixedJoint2D>();
        fixedJoint.connectedBody = shellRb;

        // Get distance joint and turn it off at start
        grappleJoint = shell.GetComponent<DistanceJoint2D>();
        grappleJoint.autoConfigureConnectedAnchor = false;
        grappleJoint.enabled = false;

        // Disable lineRenderer 
        grappleLine.enabled = false;
        // grappleLine.positionCount = 2;        
    }

    void Update()
    {
        // Snail head always follows mouse direction
        RotateToMouse();

        // Space = Jump
        if (Input.GetKeyDown("space") && !extending && !retracting)
        {
            extending = true;
            currentDistance = 0f;

            currentAction = new ActionSettings
            {
                type = ActionType.Jump,
                direction = transform.up
            };
        }

        // Right click = Quick pull
        else if (Input.GetMouseButtonDown(1) && !extending && !retracting)
        {
            extending = true;
            currentDistance = 0f;

            currentAction = new ActionSettings
            {
                type = ActionType.QuickPull,
                direction = -transform.up
            };
        }
        // Left click hold for grapple
        else if (Input.GetMouseButtonDown(0) && !isFiringGrapple)
        {
            isFiringGrapple = true;
            grappleHit = false;
            grappleDirection = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) - shellRb.position).normalized;
            grappleEnd = shellRb.position;
            grappleCurrentLength = 0f;

            grappleLine.enabled = true;
            // Prevents LineRenderer flicker 
            grappleLine.positionCount = 2;            
            grappleLine.SetPosition(0, shellRb.position);
            grappleLine.SetPosition(1, shellRb.position); 
        }
        // Break grapple when released
        else if (Input.GetMouseButtonUp(0))
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

    void FixedUpdate()
    {

        if (extending)
        {
            // Disable the join when head is extending
            if (fixedJoint.enabled) fixedJoint.enabled = false;

            currentDistance += extendSpeed * Time.fixedDeltaTime;
            if (currentDistance >= extendDistance)
            {
                currentDistance = extendDistance;
                extending = false;
                retracting = true;
            }
        }
        else if (retracting)
        {


            currentDistance -= retractSpeed * Time.fixedDeltaTime;
            if (currentDistance <= 0f)
            {
                currentDistance = 0f;
                retracting = false;

                // Re-enable joint once finished retracting.
                if (!fixedJoint.enabled) fixedJoint.enabled = true;
            }

        }

        // Apply movement only when the fixedJoint is disabled
        if (!fixedJoint.enabled)
        {
            Vector2 targetPosition = shellRb.position - (Vector2)(transform.up * currentDistance);
            rb.MovePosition(targetPosition);
        }


        // Grapple logic
        if (isFiringGrapple && !grappleHit)
        {
            float extendAmount = grappleSpeed * Time.fixedDeltaTime;

            // While not reached full length
            if (grappleCurrentLength < grappleMaxLength)
            {
                grappleCurrentLength += extendAmount;
                grappleEnd = (Vector2)shellRb.position + grappleDirection * grappleCurrentLength;

                // Check for hit while extending
                RaycastHit2D hit = Physics2D.Raycast(shellRb.position, grappleDirection, grappleCurrentLength, grappleLayerMask);


                Debug.DrawLine(shellRb.position, shellRb.position + grappleDirection * grappleCurrentLength, Color.green, 1f);

                // Once hit something
                if (hit.collider != null)
                {
                    Debug.Log("Grappled to: " + hit.collider.name);
                    grappleHit = true;
                    isFiringGrapple = false;

                    // Destroy any old anchors
                    if (currentAnchorInstance != null)
                        Destroy(currentAnchorInstance);

                    // Create new anchor at hit point
                    currentAnchorInstance = Instantiate(grappleAnchorPrefab, hit.point, Quaternion.identity);

                    // Get the newly instantiated grapple anchor rigidbody. 
                    Rigidbody2D anchorRb = currentAnchorInstance.GetComponent<Rigidbody2D>();
                    if (anchorRb == null)
                    {
                        Debug.LogError("GrappleAnchor prefab is missing a Rigidbody2D component!");
                        return;
                    }


                    // Connect the shell to the anchor's rigidbody instead of a world-space point
                    grappleJoint.connectedBody = anchorRb;
                    grappleJoint.autoConfigureConnectedAnchor = false;
                    grappleJoint.distance = Vector2.Distance(shellRb.position, hit.point);
                    grappleJoint.enabled = true;

                    grappleLine.SetPosition(1, hit.point);
                }
            }
            else
            {
                // Smooth retract back into shell if reached max length without hitting anything
                grappleCurrentLength -= extendAmount;
                if (grappleCurrentLength <= 0f)
                {
                    grappleCurrentLength = 0f;
                    isFiringGrapple = false;
                    grappleLine.enabled = false;
                }
                else
                {
                    grappleEnd = (Vector2)shellRb.position + grappleDirection * grappleCurrentLength;
                }
            }
        }
        // Grapple renderer logic
        if (grappleLine.enabled)
        {

            // Continuously update line renderer 
            grappleLine.SetPosition(0, shellRb.position);

            if (grappleHit && grappleJoint.connectedBody != null)
                // Connected to a point after hit                
                grappleLine.SetPosition(1, grappleJoint.connectedBody.position);
            // grappleLine.SetPosition(1, grappleJoint.connectedAnchor);
            else
                // Grapple is still extending
                grappleLine.SetPosition(1, grappleEnd);
        }

        // Extend/retract grapple with W and S, respectively, if it's connected.
        if (grappleJoint.enabled && grappleHit)
        {
            float maxDistance = grappleMaxLength;

            if (Input.GetKey("w"))
            {
                grappleJoint.distance = Mathf.Max(grappleMinDistance, grappleJoint.distance - grappleAdjustSpeed * Time.fixedDeltaTime);
            }
            else if (Input.GetKey("s"))
            {
                grappleJoint.distance = Mathf.Min(maxDistance, grappleJoint.distance + grappleAdjustSpeed * Time.fixedDeltaTime);
            }
        }
            
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Piston hit: {collision.collider.name} on layer {LayerMask.LayerToName(collision.collider.gameObject.layer)}");

        // Snail head leaving shell and hits an object (that isn't the shell itself)
        if (extending && collision.collider.gameObject.layer != LayerMask.NameToLayer("ShellLayer"))
        {
            HeadReaction(currentAction);
        }
    }

    void RotateToMouse()
    {
        // Rotate toward the mouse
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f; // Ignore 3D axis
        Vector3 dir = (mouseWorld - shell.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // Moves the shell towards/away from the head impact depending on input type (left vs right click)
    void HeadReaction(ActionSettings action)
    {
        // Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
        if (action.type == ActionType.Grapple)
        {
            float distanceToShell = Vector2.Distance(rb.position, shellRb.position);
            if (distanceToShell <= grappleMaxDistance)
            {
                grappleJoint.connectedAnchor = rb.position;
                grappleJoint.distance = distanceToShell;
                grappleJoint.enabled = true;
            }
            else
            {
                Debug.Log("Grapple point too far.");
            }
        }

        else
        {
            shellRb.AddForce(action.direction * jumpForce, ForceMode2D.Impulse);
        }
        

        // End extension
        // TODO: halt head for visual impact
        currentDistance = extendDistance;
        extending = false;
        retracting = true;
    }


}
