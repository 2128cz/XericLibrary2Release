using UnityEditor;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibraryEditor.XericComponent
{
    [CustomEditor(typeof(UIPrimitiveRenderer))]
    public class UIPrimitiveRendererEditor : Editor
    {
        private UIPrimitiveRenderer script;

        private SerializedProperty m_RaycastTarget;
        private SerializedProperty m_Maskable;
        private SerializedProperty m_Material;
        private SerializedProperty m_Color;
        private SerializedProperty primitiveTypeProp;
        private SerializedProperty sizeModeProp;
        private SerializedProperty sizeProp;
        private SerializedProperty centerOffsetProp;
        private SerializedProperty sideCountProp;
        private SerializedProperty chamferSizeProp;
        private SerializedProperty chamferSegmentsProp;
        private SerializedProperty bgColorProp;
        private SerializedProperty centerColorProp;
        private SerializedProperty borderColorProp;
        private SerializedProperty borderThicknessProp;
        private SerializedProperty autoRebuildProp;

        protected virtual void OnEnable()
        {
            script = (UIPrimitiveRenderer)target;

            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            m_Maskable = serializedObject.FindProperty("m_Maskable");
            m_Material = serializedObject.FindProperty("m_Material");
            m_Color = serializedObject.FindProperty("m_Color");
            primitiveTypeProp = serializedObject.FindProperty("primitiveType");
            sizeModeProp = serializedObject.FindProperty("sizeMode");
            sizeProp = serializedObject.FindProperty("size");
            centerOffsetProp = serializedObject.FindProperty("centerOffset");
            sideCountProp = serializedObject.FindProperty("sideCount");
            chamferSizeProp = serializedObject.FindProperty("chamferSize");
            chamferSegmentsProp = serializedObject.FindProperty("chamferSegments");
            bgColorProp = serializedObject.FindProperty("bgColor");
            centerColorProp = serializedObject.FindProperty("centerColor");
            borderColorProp = serializedObject.FindProperty("borderColor");
            borderThicknessProp = serializedObject.FindProperty("borderThickness");
            autoRebuildProp = serializedObject.FindProperty("autoRebuild");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // MaskableGraphic 基础属性
            EditorGUILayout.PropertyField(m_RaycastTarget);
            EditorGUILayout.PropertyField(m_Maskable);
            EditorGUILayout.PropertyField(m_Material, new GUIContent("材质"));
            EditorGUILayout.PropertyField(m_Color, new GUIContent("顶点颜色"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("图元类型", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(primitiveTypeProp);
            EditorGUILayout.PropertyField(sizeModeProp);

            bool isPolygon = script.primitiveType == XericLibrary.Runtime.UIGraph.PrimitiveType.Polygon;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("尺寸", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sizeProp, new GUIContent("尺寸 (XY)"));
            EditorGUILayout.PropertyField(centerOffsetProp, new GUIContent("中心偏移"));

            if (isPolygon)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("多边形设置", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(sideCountProp, new GUIContent("边数"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("倒角", EditorStyles.boldLabel);
            EditorGUILayout.Slider(chamferSizeProp, 0f, 0.5f, new GUIContent("倒角尺寸"));
            EditorGUILayout.IntSlider(chamferSegmentsProp, 1, 16, new GUIContent("细分段数"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("颜色", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bgColorProp, new GUIContent("背景颜色"));
            EditorGUILayout.PropertyField(centerColorProp, new GUIContent("中心颜色"));
            EditorGUILayout.PropertyField(borderColorProp, new GUIContent("边框颜色"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("边框", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(borderThicknessProp, new GUIContent("边框厚度"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(autoRebuildProp, new GUIContent("自动刷新"));

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                if (script.autoRebuild && script.isActiveAndEnabled)
                {
                    script.RebuildPrimitive();
                }
            }
        }
    }
}
