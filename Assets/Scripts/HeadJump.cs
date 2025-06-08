using UnityEngine;

public class HeadJump : MonoBehaviour
{
    public GameObject jumpCylinderPrefab;
    public float jumpForce = 6f;
    public float pistonLength = 1.5f;
    public float pistonDuration = 0.2f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;

            Vector2 direction = (mouseWorld - transform.position).normalized;

            // Apply opposite jump force to the ball
            rb.AddForce(-direction * jumpForce, ForceMode2D.Impulse);

            // Spawn piston at the center of the ball
            GameObject piston = Instantiate(jumpCylinderPrefab, transform.position, Quaternion.identity);

            // Rotate it to face the mouse
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            piston.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Animate piston scale outward like it's extending
            StartCoroutine(ExtendAndDestroyPiston(piston.transform, pistonLength, pistonDuration));
        }
    }

    System.Collections.IEnumerator ExtendAndDestroyPiston(Transform piston, float targetScale, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = piston.localScale;
        Vector3 endScale = new Vector3(startScale.x, targetScale, startScale.z); // stretch in Y

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            piston.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        piston.localScale = endScale;
        Destroy(piston.gameObject, 0.1f); // cleanup
    }
}
