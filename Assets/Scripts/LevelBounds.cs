using UnityEngine;

/// World-space rectangle the camera stays inside. One per level scene.
public class LevelBounds : MonoBehaviour
{
    [SerializeField] private Rect bounds = new Rect(-20f, -10f, 40f, 20f);

    public Rect Bounds => bounds;

    public void SetBounds(Rect value)
    {
        bounds = value;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
