using UnityEngine;

public class Aiming : MonoBehaviour
{
    public Transform ball;              // Reference to the ball (player)
    public float cursorDistance = 2f;   // How far from the ball the cursor should be

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector3 direction = (mouseWorld - ball.position).normalized;
        transform.position = ball.position + direction * cursorDistance;
    }
}
