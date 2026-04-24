using UnityEngine;
using UnityEditor;

namespace SkillSystem
{
    [CustomEditor(typeof(CurveClipAsset))]
    public class CurveClipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CurveClipAsset clip = target as CurveClipAsset;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("target_trans_path_"));

            EditorGUILayout.LabelField("Move Curves", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("curve_x_"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("curve_y_"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("curve_z_"));

            serializedObject.ApplyModifiedProperties();

            // Scene视图刷新
            if (GUI.changed)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
        }
    }
}