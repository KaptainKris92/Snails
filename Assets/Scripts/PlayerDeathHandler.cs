using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    public float restartDelay = 0.5f;

    private bool isDying = false;

    public void TriggerDeath()
    {
        if (isDying) return;

        isDying = true;        

        // Optional: disable input here
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        GetComponent<Rigidbody2D>().isKinematic = true;

        GetComponent<CrumbleEffect>().TriggerCrumble();

        Invoke(nameof(RestartLevel), restartDelay);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
