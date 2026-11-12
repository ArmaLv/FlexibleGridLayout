using UnityEditor;
using UnityEngine;
using UnityUtilities.FlexibleGridLayout;

namespace UnityUtilities.FlexibleGridLayout.EditorTools
{
    // Custom inspector for a single flexible cell: span on top, optional fixed
    // placement below, matching the tidy grouping of Unity's layout inspectors.
    [CustomEditor(typeof(FlexibleGridCell))]
    [CanEditMultipleObjects]
    public class FlexibleGridCellEditor : UnityEditor.Editor
    {
        private SerializedProperty m_ColumnSpan;
        private SerializedProperty m_RowSpan;
        private SerializedProperty m_Column;
        private SerializedProperty m_Row;

        private void OnEnable()
        {
            m_ColumnSpan = serializedObject.FindProperty("columnSpan");
            m_RowSpan = serializedObject.FindProperty("rowSpan");
            m_Column = serializedObject.FindProperty("column");
            m_Row = serializedObject.FindProperty("row");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Span", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_ColumnSpan, new GUIContent("Column Span"));
                EditorGUILayout.PropertyField(m_RowSpan, new GUIContent("Row Span"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fixed Position", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Leave a field at -1 for automatic placement.", MessageType.None);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_Column, new GUIContent("Column"));
                EditorGUILayout.PropertyField(m_Row, new GUIContent("Row"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
