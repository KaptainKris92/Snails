using System.Collections;
using UnityEngine;

/// Death: crumble, freeze, then respawn at the spawn point without reloading the scene.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovement))]
public class PlayerDeathHandler : MonoBehaviour
{
    [Tooltip("Seconds between the crumble and the respawn.")]
    public float restartDelay = 0.35f;

    private bool isDying;
    private Rigidbody2D rb;
    private Collider2D shellCollider;
    private SpriteRenderer sprite;
    private PlayerMovement movement;
    private RopeController rope;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shellCollider = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        movement = GetComponent<PlayerMovement>();
        rope = GetComponent<RopeController>();
    }

    public void TriggerDeath()
    {
        if (isDying) return;
        isDying = true;

        if (rope != null)
            rope.ResetRope();

        GetComponent<CrumbleEffect>()?.TriggerCrumble();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;
        shellCollider.enabled = false;

        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);

        if (sprite != null)
            sprite.enabled = true;
        shellCollider.enabled = true;
        isDying = false;

        movement.ResetPlayer();
    }
}
