using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // Player object (SnailShell)
    public Vector3 offset;        // Offset from the player position

    void Awake()
    {
        // Only proceed if scene name includes "Level"
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Level"))
        {
            // Make sure this script is on the Main Camera
            if (Camera.main == GetComponent<Camera>())
            {
                // Auto-assign player target (e.g., by tag)
                if (target == null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        target = player.transform;
                    }
                }
            }
            else
            {
                // Disable this component if not on main camera
                enabled = false;
            }
        }
        else
        {
            // Disable this component in non-level scenes
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 newPos = target.position;
            newPos.z = transform.position.z; // Keep camera's current Z position
            transform.position = newPos + offset;

            // Ensure camera doesn't rotate
            transform.rotation = Quaternion.identity;
        }
    }
    
}
