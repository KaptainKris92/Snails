using UnityEngine;
using UnityEngine.SceneManagement;

/// Level glue for the player: spawn point, R to reset, timer start and finish panel. Movement
/// itself (rope, A/D, ground hop) lives in RopeController.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private RopeController rope;

    private Rigidbody2D rb;
    private Vector2 spawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level"))
        {
            LeaderboardManager.Instance?.AssignLevelUIReferences();
        }
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

        if (rope != null)
            rope.ResetRope();

        rb.bodyType = RigidbodyType2D.Dynamic; // Restore forces on rigidbody.
        rb.linearVelocity = Vector2.zero;
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
