using UnityEditor;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibraryEditor.XericComponent
{
    [CustomEditor(typeof(UILineRendererV2))]
    public class UILineRendererV2Editor : Editor
    {
        private UILineRendererV2 script;

        private SerializedProperty m_RaycastTarget;
        private SerializedProperty m_Maskable;
        private SerializedProperty m_Material;
        private SerializedProperty m_Color;
        private SerializedProperty defaultThicknessProp;
        private SerializedProperty defaultColorProp;
        private SerializedProperty defaultUVModeProp;

        protected virtual void OnEnable()
        {
            script = (UILineRendererV2)target;

            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            m_Maskable = serializedObject.FindProperty("m_Maskable");
            m_Material = serializedObject.FindProperty("m_Material");
            m_Color = serializedObject.FindProperty("m_Color");
            defaultThicknessProp = serializedObject.FindProperty("defaultThickness");
            defaultColorProp = serializedObject.FindProperty("defaultColor");
            defaultUVModeProp = serializedObject.FindProperty("defaultUVMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // MaskableGraphic 基础属性
            EditorGUILayout.PropertyField(m_RaycastTarget);
            EditorGUILayout.PropertyField(m_Maskable);
            EditorGUILayout.PropertyField(m_Material, new GUIContent("材质"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("全局默认设置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultThicknessProp);
            EditorGUILayout.PropertyField(defaultColorProp);
            EditorGUILayout.PropertyField(defaultUVModeProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
