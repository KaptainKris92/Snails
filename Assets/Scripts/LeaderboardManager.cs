using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("Status Popup UI")]
    public GameObject statusPopupPanel;
    public TextMeshProUGUI statusPopupText;
    public GameObject levelSelectPanel; // Shown after clicking OK
    public GameObject setupPanel;

    [Header("Leaderboard UI")]
    public GameObject leaderboardPanel;
    public TextMeshProUGUI leaderboardText;

    private string playerName;
    private string baseUrl;
    private float playerScore;
    private bool leaderboardOnline = false;
    public static bool InputBlocked = true;
    private bool pendingStatusCheck = false;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Singleton accessor
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {                       
        InputBlocked = false;
    }

    void Update()
    {
        // Keep cursor visible and unlocked while setup screen is active or game is paused
        if (Time.timeScale == 0f || IsInputFieldFocused())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded: " + scene.name);

        if (scene.name == "MainMenu")
        {
            AssignMainMenuUIReferences();             

            if (GameSessionManager.Instance.SetupComplete)
            {                
                Debug.Log("Setup complete: hiding SetupPanel and showing LevelSelectPanel.");
                setupPanel?.SetActive(false);
                statusPopupPanel?.SetActive(false);
                levelSelectPanel?.SetActive(true);  
            }
            else
            {
                Debug.Log("Setup NOT complete: showing SetupPanel.");
                setupPanel?.SetActive(true);
                levelSelectPanel?.SetActive(false);
                statusPopupPanel?.SetActive(false);
            }
            
            if (pendingStatusCheck)
            {
                Debug.Log("Running pending status check...");
                pendingStatusCheck = false;
                StartCoroutine(CheckLeaderboardStatus());                
            }
        }

        if (scene.name.Contains("Level"))
        {
            Debug.Log("Scene name contains the word 'Level'. Assigning UI references");
            AssignLevelUIReferences();
        }
    }

    // UI Elements for Main Menu
    private void AssignMainMenuUIReferences()
    {
        setupPanel = FindInactiveGameObjectByName("SetupPanel");
        levelSelectPanel = FindInactiveGameObjectByName("LevelSelectPanel");
        statusPopupPanel = FindInactiveGameObjectByName("StatusPopupPanel");


        if (statusPopupPanel != null)
        {
            statusPopupText = statusPopupPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Debug.Log($"AssignMainMenuUIReferences() called. Found statusPopupPanel? {statusPopupPanel != null}, statusPopupText? {statusPopupText != null}");
    }

    // UI Elements for Levels 
    public void AssignLevelUIReferences()
    {
        leaderboardPanel = GameObject.Find("FinishPanel");
        leaderboardText = GameObject.Find("FinalTimeText")?.GetComponent<TextMeshProUGUI>();

        if (leaderboardPanel == null || leaderboardText == null)
        {
            Debug.LogWarning("Leaderboard UI references not found in scene.");
        }
        else
        {
            Debug.Log("Leaderboard UI references assigned.");
        }
    }


    public IEnumerator CheckLeaderboardStatus()
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("baseUrl is null or empty before checking server status!");
            statusPopupPanel.SetActive(true);
            statusPopupText.text = "<color=red><b>No server URL set.</b></color>";
            yield break;
        }

        Debug.Log("Checking leaderboard at: " + baseUrl + "/status");
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/status");
        yield return www.SendWebRequest();

        statusPopupPanel.SetActive(true); // Show popup 

        if (www.result != UnityWebRequest.Result.Success)
        {
            leaderboardOnline = false;
            statusPopupText.text = "<color=red>Could not find server.</color>";
        }
        else
        {
            leaderboardOnline = true;
            statusPopupText.text = "<color=green>Server found!</color>";
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
        StartCoroutine(CheckIfTop10AndSubmit(playerScore));
    }

    private IEnumerator CheckIfTop10AndSubmit(float score)
    {

        string levelName = SceneManager.GetActiveScene().name;
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/top_scores?level_name = {UnityWebRequest.EscapeURL(levelName)}");
        yield return www.SendWebRequest();

        bool isTop10 = false;

        if (www.result == UnityWebRequest.Result.Success)
        {
            List<ScoreEntry> topScores = JsonUtilityWrapper.FromJsonList(www.downloadHandler.text);
            isTop10 = topScores.Count < 10 || score < topScores.Max(e => e.score);
        }

        // Submit the score regardless
        yield return StartCoroutine(SubmitScore(playerName, score));

        // Now re-fetch top scores and display with message
        StartCoroutine(GetTopScores(score, isTop10));
    }

    private IEnumerator SubmitScore(string name, float score)
    {
        string levelName = SceneManager.GetActiveScene().name;
        ScoreEntry entry = new ScoreEntry
        {
            player_name = name,
            score = score,
            level_name = levelName
        };

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

    private IEnumerator GetTopScores(float playerScore, bool isTop10)
    {
        string levelName = SceneManager.GetActiveScene().name;
        UnityWebRequest www = UnityWebRequest.Get($"{baseUrl}/top_scores?level_name={UnityWebRequest.EscapeURL(levelName)}");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch leaderboard: " + www.error);
            yield break;
        }

        List<ScoreEntry> entries = JsonUtilityWrapper.FromJsonList(www.downloadHandler.text);
        DisplayLeaderboard(entries, playerScore, isTop10);
    }


    private void DisplayLeaderboard(List<ScoreEntry> entries, float playerTime, bool isTop10)
    {
        if (Time.timeScale == 0f) // Only show leaderboard if game is paused
        {
            leaderboardPanel.SetActive(true);
        }
        else
        {
            Debug.Log("Skipped showing leaderboard because game isn't paused");
        }

        string header = $"Your time: {playerTime:F2}s\n";
        if (isTop10)
            header += "<color=green>You reached the top 10!</color>\n";
        else
            header += "<color=yellow>Try again to reach the leaderboard!</color>\n";

        leaderboardText.text = header + "\nTop 10 Times:\n";

        int rank = 1;
        foreach (var entry in entries.OrderBy(e => e.score))
        {
            if (System.DateTime.TryParse(entry.created_at, out var utcTime))
            {
                var ukTime = utcTime.AddHours(1);
                leaderboardText.text += $"{rank++}. {entry.player_name} - {entry.score:F2}s  ({ukTime:yyyy-MM-dd HH:mm})\n";
            }
            else
            {
                leaderboardText.text += $"{rank++}. {entry.player_name} - {entry.score:F2}s  ({entry.created_at})\n";
            }
        }

        leaderboardText.text += "\nPress R to Restart\nor\nEsc for Main Menu";
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
            callback?.Invoke("Failed to load leaderboard.");
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
        public string level_name;
        public string created_at;
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

    private bool IsInputFieldFocused()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        return selected != null && selected.GetComponent<TMP_InputField>() != null;
    }

    public void SetSessionValues()
    {
        playerName = GameSessionManager.Instance.PlayerName;
        baseUrl = GameSessionManager.Instance.NgrokURL;
        Debug.Log($"SetSessionValues: Name = {playerName}, baseUrl = {baseUrl}");
    }

    public void OnStatusPopupOK()
    {
        statusPopupPanel.SetActive(false);
        setupPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
    }

    public void StartStatusCheck()
    {
        SetSessionValues();
        // pendingStatusCheck = true; // Mark that status check needs to be done, but don't run it yet to avoid timing mismatches.        

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            Debug.Log("StartStatusCheck(): Already in MainMenu, running immediately.");
            AssignMainMenuUIReferences();

            if (statusPopupPanel == null || statusPopupText == null)
            {
                Debug.LogError("UI references not assigned before CheckLeaderboardStatus!");
                return;
            }

            Debug.Log("StartStatusCheck(): Already in MainMenu, running immediately.");

            // pendingStatusCheck = false;
            StartCoroutine(CheckLeaderboardStatus());
        }
        else
        {
            Debug.Log("StartStatusCheck(): Scene not MainMenu, delaying until loaded.");
            pendingStatusCheck = true;
        }
    }

    private GameObject FindInactiveGameObjectByName(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name == name && t.hideFlags == HideFlags.None)
                return t.gameObject;
        }
        return null;
    }

    public void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
