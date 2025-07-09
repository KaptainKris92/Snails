using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelInputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = 1f; //Unpause game (probably unnecessary?)
            SceneManager.LoadScene("MainMenu");
        }
    }
}
