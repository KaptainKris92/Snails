using UnityEngine;
using UnityEngine.AI;

public class TimerZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {   
            // If TimerZone touches player, trigger ResetPlayer() from PlayerMovement.cs
            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.ResetPlayer();
            }
        }
    }
}
