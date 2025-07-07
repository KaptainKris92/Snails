using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.AI;

public class EndZone : MonoBehaviour
{

    [SerializeField] private GameObject finishPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;

    private static bool levelCompleted = false; // This is shared by all EndZone prefabs

    void Start()
    {
        // Objects must have exact same names in Scene.
        if (finishPanel == null)
            finishPanel = GameObject.Find("FinishPanel");

        if (finalTimeText == null)
            finalTimeText = GameObject.Find("FinalTimeText").GetComponent<TextMeshProUGUI>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (levelCompleted)
            return;

        if (other.CompareTag("Player"))
        {
            levelCompleted = true; // This should only get triggered once.

            // Stop player gaining momentum in final panel.
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static; // Prevents all movements and forces

            }

            TimerManager.instance.RecordTime();

            TimerManager.instance.StopTimer();
            float finalTime = TimerManager.instance.GetTime();

            LeaderboardManager.Instance.TrySubmitScore(finalTime);
            finishPanel.SetActive(true);
            Time.timeScale = 0f; //Pause the game            

            // Unlock and show (if hidden) the cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Fetch and show leaderboard
            finalTimeText.text = $"Final time: {finalTime:F2}s\n\nLoading leaderboard...";
            LeaderboardManager.Instance.GetTopScoresAsString((string leaderboard) =>
            {
                finalTimeText.text = $"Final time: {finalTime:F2}s\n\n{leaderboard}\nPress R to Restart";
            });
        }
    }

    public static void ResetLevelFlag()
    {
        levelCompleted = false;
    }

    
}
