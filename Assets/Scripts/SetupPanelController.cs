using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class SetupPanelController : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField ngrokInput;
    [SerializeField] private GameObject setupPanel;

    public void Submit()
    {
        string name = nameInput.text.Trim();
        string ngrok = ngrokInput.text.Trim();

        Debug.Log($"Submit() called. Name: {name}, URL: {ngrok}");

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ngrok))
        {
            GameSessionManager.Instance.SetPlayerInfo(name, ngrok);
            setupPanel.SetActive(false); // Hide the panel after 'Save' pressed'

            Time.timeScale = 1f; // Unpause the game (probably unnecessary in main menu)
            LeaderboardManager.InputBlocked = false;

            // Sync data and start pinging leaderboard
            LeaderboardManager.Instance?.StartStatusCheck();
        }
        else
        {
            Debug.LogWarning("Name or Ngrok URL is missing.");
        }
    }
}
