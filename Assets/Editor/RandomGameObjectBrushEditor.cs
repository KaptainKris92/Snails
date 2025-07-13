using UnityEditor;
using UnityEditor.Tilemaps;

[CustomEditor(typeof(RandomGameObjectBrush))]
public class RandomGameObjectBrushEditor : GridBrushEditorBase
{
    public override void OnPaintInspectorGUI()
    {
        var brush = target as RandomGameObjectBrush;

        EditorGUILayout.LabelField("Random GameObject Brush", EditorStyles.boldLabel);
        SerializedObject so = new SerializedObject(brush);
        SerializedProperty variants = so.FindProperty("prefabVariants");

        EditorGUILayout.PropertyField(variants, true);
        so.ApplyModifiedProperties();
    }
}
