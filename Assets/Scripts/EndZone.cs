using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class EndZone : MonoBehaviour
{
    
    [SerializeField] private GameObject finishPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;

    void Start()
    {
        // Objects must have exact same names in Scene.
        if (finishPanel == null)
            finishPanel = GameObject.Find("FinishPanel");

            finishPanel.SetActive(false);

        if (finalTimeText == null)
            finalTimeText = GameObject.Find("FinalTimeText").GetComponent<TextMeshProUGUI>(); 
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player"))
        {
            TimerManager.instance.RecordTime();
            List<float> times = TimerManager.instance.GetTimes();
            string leaderboard = "Past 5 times:\n";

            for (int i = 0; i < times.Count; i++)
            {
                leaderboard += $"{i + 1}. {times[i]:F2}s\n";
            }
            
            TimerManager.instance.StopTimer();
            float finalTime = TimerManager.instance.GetTime();
            finalTimeText.text = $"Final time: {finalTime:F2}s\n\n{leaderboard}\nPress R to Restart";

            finishPanel.SetActive(true);
            Time.timeScale = 0f; //Pause the game            
        }
    }
}
