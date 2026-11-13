using UnityEditor;
using UnityEngine;
using UnityUtilities.FlexibleGridLayout;

namespace UnityUtilities.FlexibleGridLayout.EditorTools
{
    // Custom inspector so the Flexible Grid Layout Group reads like one of Unity's
    // own layout components: grouped sections instead of a flat list of fields.
    [CustomEditor(typeof(FlexibleGridLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class FlexibleGridLayoutGroupEditor : UnityEditor.Editor
    {
        private SerializedProperty m_Padding;
        private SerializedProperty m_Columns;
        private SerializedProperty m_CellSize;
        private SerializedProperty m_WidthMode;
        private SerializedProperty m_HeightMode;
        private SerializedProperty m_Spacing;
        private SerializedProperty m_StartCorner;
        private SerializedProperty m_FillGaps;
        private SerializedProperty m_HorizontalAlign;
        private SerializedProperty m_VerticalAlign;

        private void OnEnable()
        {
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_Columns = serializedObject.FindProperty("m_Columns");
            m_CellSize = serializedObject.FindProperty("m_CellSize");
            m_WidthMode = serializedObject.FindProperty("m_WidthMode");
            m_HeightMode = serializedObject.FindProperty("m_HeightMode");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_StartCorner = serializedObject.FindProperty("m_StartCorner");
            m_FillGaps = serializedObject.FindProperty("m_FillGaps");
            m_HorizontalAlign = serializedObject.FindProperty("m_HorizontalAlign");
            m_VerticalAlign = serializedObject.FindProperty("m_VerticalAlign");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Padding, true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_Columns, new GUIContent("Columns"));
                EditorGUILayout.PropertyField(m_CellSize, new GUIContent("Cell Size",
                    "Reference size of one cell. Used directly in Fixed mode, and as the " +
                    "aspect ratio in Aspect mode."));
                EditorGUILayout.PropertyField(m_WidthMode, new GUIContent("Width Mode",
                    "Fixed: use Cell Size. Expand: stretch cells to fill the container width. " +
                    "Aspect: derive width from height using the Cell Size ratio."));
                EditorGUILayout.PropertyField(m_HeightMode, new GUIContent("Height Mode",
                    "Fixed: use Cell Size. Expand: stretch cells to fill the container height. " +
                    "Aspect: derive height from width using the Cell Size ratio."));
                EditorGUILayout.PropertyField(m_Spacing, new GUIContent("Spacing"));
                EditorGUILayout.PropertyField(m_StartCorner, new GUIContent("Start Corner"));
                EditorGUILayout.PropertyField(m_FillGaps, new GUIContent("Fill Gaps",
                    "Slot smaller elements into free gaps (dense packing). " +
                    "When off, elements flow sequentially and leave holes behind large ones."));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Content Alignment", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_HorizontalAlign, new GUIContent("Horizontal"));
                EditorGUILayout.PropertyField(m_VerticalAlign, new GUIContent("Vertical"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
