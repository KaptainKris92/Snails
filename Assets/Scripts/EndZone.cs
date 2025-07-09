using UnityEngine;

public class EndZone : MonoBehaviour
{
    
    private static bool levelCompleted = false; // This is shared by all EndZone prefabs

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
            Time.timeScale = 0f; //Pause the game            

            // Unlock and show (if hidden) the cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
                        
        }
    }

    public static void ResetLevelFlag()
    {
        levelCompleted = false;
    }

    
}
