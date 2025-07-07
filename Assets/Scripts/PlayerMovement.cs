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

        if (TimerManager.instance != null)
        {
            TimerManager.instance.StartTimer();
        }

        DisableFinishPanel();
    }

    void Update()
    {
        if (LeaderboardManager.InputBlocked) return;
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
            // If paused, unpause before reset
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
            ResetPlayer();
        }
    }

    public void ResetPlayer()
    {
        DisableFinishPanel();
        Cursor.visible = false;
        EndZone.ResetLevelFlag();
        
        if (headControl != null)
            headControl.CancelGrapple();

        rb.bodyType = RigidbodyType2D.Dynamic; // Restore forces on rigidbody.
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = spawnPoint;

        if (TimerManager.instance != null)
        {
            TimerManager.instance.StartTimer();
        }
    }

    public void DisableFinishPanel()
    {
        GameObject finishPanel = GameObject.Find("FinishPanel");
        if (finishPanel != null)
            finishPanel.SetActive(false);
    }
}
