using UnityEngine;

public class HeadJump : MonoBehaviour
{
    public Transform shell;                // shell's transform
    public Rigidbody2D rb;               // Piston's rigidbody
    public float extendDistance = 2f;
    public float extendSpeed = 5f;
    public float retractSpeed = 7f;
    public float jumpForce = 10f;

    private float currentDistance = 0f;
    private bool extending = false;
    private bool retracting = false;

    private Vector3 direction => transform.up;

    void Update()
    {
        


        // Rotate toward the mouse
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3 dir = (mouseWorld - shell.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Trigger extension on click
        if (Input.GetMouseButtonDown(0) && !extending && !retracting)
        {
            extending = true;
            currentDistance = 0f;
        }
    }

    void FixedUpdate()
    {
        if (extending)
        {
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
            }
        }

        // Move outward in the direction the head is currently facing
        rb.MovePosition(shell.position - transform.up * currentDistance);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Piston hit: {collision.collider.name} on layer {LayerMask.LayerToName(collision.collider.gameObject.layer)}");
        if (extending && collision.collider.gameObject.layer != LayerMask.NameToLayer("ShellLayer"))
            {
                Rigidbody2D shellRb = shell.GetComponent<Rigidbody2D>();
                shellRb.AddForce(-transform.up * jumpForce, ForceMode2D.Impulse);

                // Optional: halt head for visual impact
                currentDistance = extendDistance;
                extending = false;
                retracting = true;
            }
    }

}
