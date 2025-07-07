using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set;  }

    [Header("Setup UI")]
    public GameObject setupPanel;
    public TMP_InputField nameInputField;
    public TMP_InputField urlInputField;
    public TextMeshProUGUI statusText;

    [Header("Leaderboard UI")]
    public GameObject leaderboardPanel;
    public TextMeshProUGUI leaderboardText;

    private string playerName;
    private string baseUrl;
    private float playerScore;
    private bool leaderboardOnline = false;
    public static bool InputBlocked = true;

    void Awake()
    {
        // Singleton accessor
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 0f; // Pause the game until setup is complete
        statusText.text = "Checking leaderboard...";
    }

    public void OnPressGo()
    {
        Debug.Log("Button pressed!");
        playerName = nameInputField.text.Trim();
        baseUrl = urlInputField.text.Trim().TrimEnd('/');

        if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogWarning("Both name and URL must be filled in.");
            return;
        }

        setupPanel.SetActive(false);
        Time.timeScale = 1f; // Unpause game
        StartCoroutine(CheckLeaderboardStatus());
        InputBlocked = false;
    }

    private IEnumerator CheckLeaderboardStatus()
    {
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/status");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            leaderboardOnline = false;
            statusText.text = "<color=yellow>⚠️ Leaderboard is down</color>";
        }
        else
        {
            leaderboardOnline = true;
            statusText.text = "<color=green>✅ Leaderboard active</color>";
        }
    }

    public void TrySubmitScore(float time)
    {
        if (!leaderboardOnline)
        {
            Debug.LogWarning("Leaderboard is offline, not submitting.");
            return;
        }

        playerScore = time;
        StartCoroutine(CheckIfTop10(playerScore));
    }

    private IEnumerator CheckIfTop10(float score)
    {
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/top_scores");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to get top scores: " + www.error);
            yield break;
        }

        List<ScoreEntry> topScores = JsonUtilityWrapper.FromJsonList(www.downloadHandler.text);
        bool isTop10 = topScores.Count < 10 || score < topScores.Max(e => e.score);

        if (isTop10)
        {
            StartCoroutine(SubmitScore(playerName, score));
        }

        // Always show updated leaderboard
        StartCoroutine(GetTopScores());
    }

    private IEnumerator SubmitScore(string name, float score)
    {
        ScoreEntry entry = new ScoreEntry { player_name = name, score = score };
        string json = JsonUtility.ToJson(entry);

        UnityWebRequest www = new UnityWebRequest($"{baseUrl}/submit_score", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Submit failed: " + www.error);
        }
        else
        {
            Debug.Log("Score submitted.");
        }
    }

    private IEnumerator GetTopScores()
    {
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/top_scores");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch leaderboard: " + www.error);
            yield break;
        }

        List<ScoreEntry> entries = JsonUtilityWrapper.FromJsonList(www.downloadHandler.text);
        DisplayLeaderboard(entries);
    }

    private void DisplayLeaderboard(List<ScoreEntry> entries)
    {
        leaderboardPanel.SetActive(true);
        leaderboardText.text = "Top 10 Times:\n";

        int rank = 1;
        foreach (var entry in entries.OrderBy(e => e.score))
        {
            leaderboardText.text += $"{rank++}. {entry.player_name} - {entry.score:F2}s\n";
        }
    }

    public void GetTopScoresAsString(System.Action<string> onResult)
    {
        StartCoroutine(FetchTopScoresAsString(onResult));
    }

    private IEnumerator FetchTopScoresAsString(System.Action<string> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/top_scores");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callback?.Invoke("⚠️ Failed to load leaderboard.");
            yield break;
        }

        List<ScoreEntry> entries = JsonUtilityWrapper.FromJsonList(www.downloadHandler.text);

        string leaderboard = "Top 10 Times:\n";
        int rank = 1;
        foreach (var entry in entries.OrderBy(e => e.score))
        {
            leaderboard += $"{rank++}. {entry.player_name} - {entry.score:F2}s\n";
        }

        callback?.Invoke(leaderboard);
    }


    [System.Serializable]
    public class ScoreEntry
    {
        public string player_name;
        public float score;
    }

    public static class JsonUtilityWrapper
    {
        [System.Serializable]
        private class ScoreListWrapper { public List<ScoreEntry> list; }

        public static List<ScoreEntry> FromJsonList(string json)
        {
            string wrapped = "{\"list\":" + json + "}";
            return JsonUtility.FromJson<ScoreListWrapper>(wrapped).list;
        }
    }
}
