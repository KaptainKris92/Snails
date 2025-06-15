using UnityEngine;
using UnityEditor;

public class TileBrushEditor : EditorWindow
{
    GameObject squareTilePrefab;
    GameObject triangleTilePrefab;
    GameObject selectedPrefab;
    Vector2 tileSize = new Vector2(1f, 1f);
    GameObject previewTile;
    float currentRotation = 0f;

    [MenuItem("Tools/Tile Brush")]
    public static void ShowWindow() => GetWindow<TileBrushEditor>("Tile Brush");

    private bool TileExistsAtPosition(Vector2 pos)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Tile"))
        {
            // Ignore preview tile
            if (obj == previewTile) continue;
            // If previewTile isn't in the scence
            if (!obj.scene.IsValid()) continue;

            Vector2 objPos = obj.transform.position;
            Vector2 snappedObjPos = new Vector2(Mathf.Floor(objPos.x), Mathf.Floor(objPos.y));

            if (snappedObjPos == pos)
                return true;
        }
        return false;
    }

    private GameObject FindTileAtPosition(Vector2 pos)
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Tile"))
        {
            if ((Vector2)obj.transform.position == pos)
                return obj;
        }
        return null;
    }

    void OnGUI()
    {
        GUILayout.Label("Tile Prefabs", EditorStyles.boldLabel);
        squareTilePrefab = (GameObject)EditorGUILayout.ObjectField("Square", squareTilePrefab, typeof(GameObject), false);
        triangleTilePrefab = (GameObject)EditorGUILayout.ObjectField("Triangle", triangleTilePrefab, typeof(GameObject), false);

        selectedPrefab = GUILayout.Toggle(selectedPrefab == squareTilePrefab, "Use Square Tile", "Button") ? squareTilePrefab : selectedPrefab;
        selectedPrefab = GUILayout.Toggle(selectedPrefab == triangleTilePrefab, "Use Triangle Tile", "Button") ? triangleTilePrefab : selectedPrefab;
    }

    void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (previewTile != null) DestroyImmediate(previewTile);
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Preview tile
        if (selectedPrefab != null)
        {
            Vector2 snappedPos = GetSnappedMouseWorldPosition(e);

            if (previewTile == null || previewTile.name != selectedPrefab.name + " (Preview)")
            {
                if (previewTile != null) DestroyImmediate(previewTile);
                previewTile = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                previewTile.name = selectedPrefab.name + " (Preview)";
                previewTile.GetComponent<Collider2D>().enabled = false;
                foreach (var r in previewTile.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = new Material(r.sharedMaterial) { color = new Color(1f, 1f, 1f, 0.3f) };
                previewTile.hideFlags = HideFlags.HideAndDontSave;
            }

            previewTile.transform.position = snappedPos;
            previewTile.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            SceneView.RepaintAll();
        }

        // Rotate with R
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            currentRotation += 90f;
            if (currentRotation >= 360f) currentRotation = 0f;

            if (previewTile != null)
                previewTile.transform.rotation = Quaternion.Euler(0, 0, currentRotation);

            e.Use();
        }

        // Place tile if one is selected and left click is pressed OR held (for painting quicker)
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && selectedPrefab)
        {
            Vector2 snappedPos = GetSnappedMouseWorldPosition(e);

            if (!TileExistsAtPosition(snappedPos))
            {
                GameObject newTile = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                newTile.transform.position = snappedPos;
                newTile.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
                newTile.tag = "Tile";  // Ensure tag is set
                Undo.RegisterCreatedObjectUndo(newTile, "Place Tile");
            }
            e.Use();
        }

        // Erase with right mouse
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1)
        {
            Vector2 snappedPos = GetSnappedMouseWorldPosition(e);
            GameObject tileToRemove = FindTileAtPosition(snappedPos);
            if (tileToRemove != null)
            {
                Undo.DestroyObjectImmediate(tileToRemove);
                e.Use();
            }
        }
    }

    private Vector2 GetSnappedMouseWorldPosition(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector2 worldPos = ray.origin;
        return new Vector2(Mathf.Floor(worldPos.x), Mathf.Floor(worldPos.y));
    }
}
