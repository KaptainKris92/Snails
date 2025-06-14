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

    private FixedJoint2D fixedJoint;

    public enum ActionType
    {
        Jump,
        QuickPull,
        Grapple
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
    }


    void Update()
    {
        // Snail head always follows mouse direction
        RotateToMouse();



        // Left click = Jump
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
        Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
        shellRb.AddForce(action.direction * jumpForce, ForceMode2D.Impulse);

        // Optional: halt head for visual impact
        currentDistance = extendDistance;
        extending = false;
        retracting = true;
    }

}
