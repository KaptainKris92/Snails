using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveForce = 10f;
    public float torqueForce = 5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");

        // Add force to move left/right
        rb.AddForce(new Vector2(horizontal * moveForce, 0f));

        // Add torque to roll the ball
        rb.AddTorque(-horizontal * torqueForce);
    }
}
