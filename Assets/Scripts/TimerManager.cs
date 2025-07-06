using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TimerManager : MonoBehaviour
{
    public static TimerManager instance;
    [SerializeField] private TextMeshProUGUI timerText;
    private float timeElapsed = 0f;
    private bool isRunning = false;

    // Temporary scoreboard (last 5 times)
    public List<float> previousTimes = new List<float>();

    void Awake()
    {
        // Singleton for easy access
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (isRunning)
        {
            timeElapsed += Time.deltaTime;
            timerText.text = timeElapsed.ToString("F2") + "s"; // Presuambly F3 would be 3 decimal places.
        }
    }

    public void StartTimer()
    {
        timeElapsed = 0f;
        isRunning = true;
    }

    public void ResetTimer()
    {
        timeElapsed = 0f;
        timerText.text = "0.00s";
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public float GetTime()
    {
        return timeElapsed;
    }

    public void RecordTime()
    {
        previousTimes.Insert(0, timeElapsed);
        if (previousTimes.Count > 5)
            previousTimes.RemoveAt(5);
    }

    public List<float> GetTimes()
    {
        return previousTimes;
    }
}
