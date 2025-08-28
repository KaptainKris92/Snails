using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class SetupPanelController : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject setupPanel;

    public void Submit()
    {
        string name = nameInput.text.Trim();        

        Debug.Log($"Submit() called. Name: {name}");

        if (!string.IsNullOrEmpty(name))
        {
            GameSessionManager.Instance.SetPlayerInfo(name);
            setupPanel.SetActive(false); // Hide the panel after 'Save' pressed'

            Time.timeScale = 1f; // Unpause the game (probably unnecessary in main menu)
            LeaderboardManager.InputBlocked = false;

            // Sync data and start pinging leaderboard
            LeaderboardManager.Instance?.StartStatusCheck();
        }
        else
        {
            Debug.LogWarning("No Name entered.");
        }
    }
}
