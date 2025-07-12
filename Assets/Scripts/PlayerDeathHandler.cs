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

        GetComponent<CrumbleEffect>().TriggerCrumble();

        // Stops velocity
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        GetComponent<Rigidbody2D>().isKinematic = true;

        Invoke(nameof(RestartLevel), restartDelay);
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
