using System.Security.Permissions;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{

    [Header("MovementSettings")]
    [SerializeField] private float normalMoveForce = 0.01f;
    [SerializeField] private float grappledMoveForce = 0.05f;

    [Header("Spin settings")]
    [SerializeField] private float normalTorqueForce = 0.05f;
    [SerializeField] private float grappledTorqueForce = 0.05f;

    [Header("References")]
    private Rigidbody2D rb;
    [SerializeField] private HeadControl headControl; // Assign in inspector

    private Vector2 spawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        spawnPoint = transform.position;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");

        float moveForce = headControl != null && headControl.isGrappled
            ? grappledMoveForce
            : normalMoveForce;

        float torqueForce = headControl != null && headControl.isGrappled
            ? grappledTorqueForce
            : normalTorqueForce;

        // Apply left/right force and torque
        rb.AddForce(new Vector2(horizontal * moveForce, 0f));
        rb.AddTorque(-horizontal * torqueForce);

        // Press 'R' to reset to spawn point
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPlayer();
        }
    }

    public void ResetPlayer()
    {
        if (headControl != null)
            headControl.CancelGrapple();

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = spawnPoint;
    }
}
