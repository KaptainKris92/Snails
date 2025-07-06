using UnityEngine;

public class Aiming : MonoBehaviour
{
    public Transform shell;              // Reference to the player    
    [SerializeField] private float maxDistance = 5f;
    


    void Start()
    {
        if (shell == null)
        {
            Debug.LogError("Aiming: Ball reference is not assigned!");
        }
    }

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector3 toMouse = mouseWorld - shell.position;

        // Clamp distance to maxDistance
        if (toMouse.magnitude > maxDistance)
        {
            toMouse = toMouse.normalized * maxDistance;
        }

        transform.position = shell.position + toMouse;
    }

}
