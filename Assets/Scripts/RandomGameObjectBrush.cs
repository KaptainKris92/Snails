using UnityEditor;
using UnityEngine;
using UnityEditor.Tilemaps;


[CreateAssetMenu(fileName = "RandomGameObjectBrush", menuName = "Brushes/Random GameObject Brush")]
[CustomGridBrush(false, true, false, "Random GameObject Brush")]
public class RandomGameObjectBrush : GridBrush
{
    public GameObject[] prefabVariants;

    public override void Paint(GridLayout grid, GameObject brushTarget, Vector3Int position)
    {
        if (prefabVariants == null || prefabVariants.Length == 0)
        {
            Debug.LogWarning("No prefab variants set!");
            return;
        }

        GameObject selected = prefabVariants[Random.Range(0, prefabVariants.Length)];
        if (selected == null)
        {
            Debug.LogWarning("Selected prefab was null.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selected, brushTarget.scene);
        instance.transform.SetParent(brushTarget.transform);
        instance.transform.position = grid.CellToWorld(position);
        Undo.RegisterCreatedObjectUndo(instance, "Paint Random Prefab");
    }
}

