using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      // Player object (SnailShell)
    public Vector3 offset;        // Offset from the player position

    // Using LateUpdate() as this is called after all the other Update() calls, ensuring the camera always moves AFTER the player.
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