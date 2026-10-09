using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Fills the level select panel with one button per level scene in the build settings, showing
/// the local personal best under each name. Buttons are clones of an inactive template button.
public class LevelSelectMenu : MonoBehaviour
{
    [SerializeField] private Button buttonTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private LevelLoader loader;

    private readonly List<GameObject> spawned = new List<GameObject>();

    void OnEnable()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        foreach (var go in spawned)
            Destroy(go);
        spawned.Clear();

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = Path.GetFileNameWithoutExtension(path);
            if (!sceneName.Contains("Level"))
                continue;

            Button button = Instantiate(buttonTemplate, container);
            button.gameObject.SetActive(true);
            button.name = "Level_" + sceneName;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = Label(sceneName);

            string captured = sceneName;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => loader.LoadLevel(captured));

            spawned.Add(button.gameObject);
        }
    }

    private static string Label(string sceneName)
    {
        string title = sceneName == "OG Level" ? "OG" : sceneName.Replace("Level ", "");
        if (EndZone.TryGetPersonalBest(sceneName, out float pb))
            return title + "\n<size=55%>" + pb.ToString("F2") + "s</size>";
        return title + "\n<size=55%>--</size>";
    }
}
