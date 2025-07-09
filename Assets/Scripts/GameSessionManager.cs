using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    public string PlayerName { get; private set; }
    public string NgrokURL { get; private set; }

    public bool SetupComplete { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist this game object across scenes        
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void SetPlayerInfo(string playerName, string ngrokURL)
    {
        PlayerName = playerName;
        NgrokURL = ngrokURL;
        SetupComplete = true;
    }

    

}
