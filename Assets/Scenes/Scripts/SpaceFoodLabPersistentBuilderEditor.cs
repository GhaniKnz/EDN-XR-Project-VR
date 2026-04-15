#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpaceFoodLabPersistentBuilder))]
public class SpaceFoodLabPersistentBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(12);

        SpaceFoodLabPersistentBuilder builder = (SpaceFoodLabPersistentBuilder)target;

        GUI.backgroundColor = new Color(0.35f, 0.85f, 1f);
        if (GUILayout.Button("Build Persistent Scene", GUILayout.Height(35)))
        {
            builder.BuildPersistentScene();
        }

        GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
        if (GUILayout.Button("Clear Generated Scene", GUILayout.Height(30)))
        {
            builder.ClearGeneratedScene();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif