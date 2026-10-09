using UnityEngine;
using UnityEngine.SceneManagement;

public class EndZone : MonoBehaviour
{
    private static bool levelCompleted = false; // This is shared by all EndZone objects in a level

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
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static; // Prevents all movements and forces
            }
            other.GetComponent<RopeController>()?.ResetRope();

            TimerManager.instance.RecordTime();
            TimerManager.instance.StopTimer();
            float finalTime = TimerManager.instance.GetTime();

            RecordPersonalBest(SceneManager.GetActiveScene().name, finalTime);

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

    // ---- personal bests, stored locally per scene

    public static string PersonalBestKey(string sceneName) => "PB_" + sceneName;

    public static bool TryGetPersonalBest(string sceneName, out float seconds)
    {
        seconds = PlayerPrefs.GetFloat(PersonalBestKey(sceneName), -1f);
        return seconds >= 0f;
    }

    private static void RecordPersonalBest(string sceneName, float seconds)
    {
        if (TryGetPersonalBest(sceneName, out float best) && best <= seconds)
            return;
        PlayerPrefs.SetFloat(PersonalBestKey(sceneName), seconds);
        PlayerPrefs.Save();
    }
}
