using UnityEngine;

public class CrumbleEffect : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Sprite[] slicedSprites;
    public int chunksX = 4;
    public int chunksY = 4;
    public float force = 2f;
    public float chunkScaleFactor = 2f;
    public float momentumStrength = 3f;

    public void TriggerCrumble()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (!sr || chunkPrefab == null || slicedSprites.Length == 0) return;

        Bounds bounds = sr.bounds;
        Vector2 chunkSize = new Vector2(bounds.size.x / chunksX, bounds.size.y / chunksY);

        int spriteIndex = 0;
        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                if (spriteIndex >= slicedSprites.Length) break;

                Vector2 pos = new Vector2(
                    bounds.min.x + chunkSize.x * (x + 0.5f),
                    bounds.min.y + chunkSize.y * (chunksY - y - 0.5f) // Y reversed
                );

                GameObject chunk = Instantiate(chunkPrefab, pos, Quaternion.identity);
                chunk.transform.localScale = chunkSize * chunkScaleFactor;

                var srChunk = chunk.GetComponent<SpriteRenderer>();
                if (srChunk != null)
                    srChunk.sprite = slicedSprites[spriteIndex];

                Rigidbody2D rb = chunk.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    // Transfer player's momentum into the chunks
                    Rigidbody2D playerRb = GetComponent<Rigidbody2D>();
                    Vector2 movementDir = playerRb != null ? playerRb.velocity.normalized : Vector2.zero;

                    // Add player direction momentum as force
                    rb.AddForce(movementDir * momentumStrength, ForceMode2D.Impulse);

                    // Random force for scatter
                    Vector2 randomDir = new Vector2(
                        Random.Range(-1f, 1f),
                        Random.Range(0.5f, 1.5f)
                    ).normalized;

                    rb.AddForce(randomDir * force, ForceMode2D.Impulse);
                }

                spriteIndex++;
            }
        }

        sr.enabled = false; // Hide the original sprite
        gameObject.SetActive(false);// Disable player gameOjevt
    }
}
