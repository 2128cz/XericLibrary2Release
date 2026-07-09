using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibraryEditor.XericComponent
{
    [CustomEditor(typeof(UICurveRenderer))]
    public class UICurveRendererEditor : Editor
    {
        private UICurveRenderer script;

        // MaskableGraphic 基础属性
        private SerializedProperty m_RaycastTarget;
        private SerializedProperty m_Maskable;
        private SerializedProperty m_Material;
        private SerializedProperty m_Color;

        // 曲线属性
        private SerializedProperty controlPointsProp;
        private SerializedProperty startColorProp;
        private SerializedProperty endColorProp;
        private SerializedProperty tessellationSegmentsProp;
        private SerializedProperty arrowsProp;
        private SerializedProperty autoRebuildProp;

        // 折叠状态
        private bool m_ControlPointsFoldout = true;
        private bool m_ArrowsFoldout = true;

        protected void OnEnable()
        {
            script = (UICurveRenderer)target;
            FetchProperties();
        }

        private void FetchProperties()
        {
            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
            m_Maskable = serializedObject.FindProperty("m_Maskable");
            m_Material = serializedObject.FindProperty("m_Material");
            m_Color = serializedObject.FindProperty("m_Color");

            controlPointsProp = serializedObject.FindProperty("controlPoints");
            startColorProp = serializedObject.FindProperty("startColor");
            endColorProp = serializedObject.FindProperty("endColor");
            tessellationSegmentsProp = serializedObject.FindProperty("tessellationSegments");
            arrowsProp = serializedObject.FindProperty("arrows");
            autoRebuildProp = serializedObject.FindProperty("autoRebuild");
        }

        public override void OnInspectorGUI()
        {
            // 确保 SerializedProperty 已初始化
            if (arrowsProp == null)
                FetchProperties();

            serializedObject.Update();

            // MaskableGraphic 基础属性
            EditorGUILayout.PropertyField(m_RaycastTarget);
            EditorGUILayout.PropertyField(m_Maskable);
            EditorGUILayout.PropertyField(m_Material, new GUIContent("材质"));
            EditorGUILayout.PropertyField(m_Color, new GUIContent("顶点颜色"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("颜色", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startColorProp, new GUIContent("起始颜色"));
            EditorGUILayout.PropertyField(endColorProp, new GUIContent("结束颜色"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("曲线参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tessellationSegmentsProp, new GUIContent("细分段数"));

            // 控制点折叠列表
            EditorGUILayout.Space();
            m_ControlPointsFoldout = EditorGUILayout.Foldout(m_ControlPointsFoldout,
                new GUIContent($"控制点 ({GetArrayLength(controlPointsProp)})"), true);
            if (m_ControlPointsFoldout)
            {
                EditorGUI.indentLevel++;
                DrawControlPointsList();
                EditorGUI.indentLevel--;
            }

            // 箭头折叠列表
            EditorGUILayout.Space();
            m_ArrowsFoldout = EditorGUILayout.Foldout(m_ArrowsFoldout,
                new GUIContent($"箭头装饰 ({arrowsProp.arraySize})"), true);
            if (m_ArrowsFoldout)
            {
                EditorGUI.indentLevel++;
                DrawArrowsList();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(autoRebuildProp, new GUIContent("自动刷新"));

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                if (script.autoRebuild && script.isActiveAndEnabled)
                {
                    script.RebuildCurve();
                }
            }
        }

        private int GetArrayLength(SerializedProperty prop)
        {
            if (prop == null) return 0;
            return prop.arraySize;
        }

        private void DrawControlPointsList()
        {
            if (controlPointsProp == null) return;

            int count = controlPointsProp.arraySize;
            int newCount = EditorGUILayout.IntField("数量", count);
            if (newCount < 2) newCount = 2;
            if (newCount != count)
            {
                controlPointsProp.arraySize = newCount;
            }

            for (int i = 0; i < controlPointsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var cp = controlPointsProp.GetArrayElementAtIndex(i);
                // Vector3: (x,y)=位置, z=宽度
                Vector3 val = cp.vector3Value;
                EditorGUI.BeginChangeCheck();
                val = EditorGUILayout.Vector3Field($"CP {i}", val);
                if (EditorGUI.EndChangeCheck())
                    cp.vector3Value = val;

                // 删除按钮（至少保留 2 个控制点）
                GUI.enabled = controlPointsProp.arraySize > 2;
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    RemoveControlPointAt(i);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            // 添加按钮
            if (GUILayout.Button("+ 添加控制点"))
            {
                int idx = controlPointsProp.arraySize;
                controlPointsProp.arraySize++;

                if (idx > 0)
                {
                    var prev = controlPointsProp.GetArrayElementAtIndex(idx - 1);
                    var prev2 = idx > 1 ? controlPointsProp.GetArrayElementAtIndex(idx - 2) : prev;
                    Vector3 prevVal = prev.vector3Value;
                    Vector3 prev2Val = prev2.vector3Value;
                    Vector3 dir = new Vector3(
                        (prevVal.x - prev2Val.x) * 0.5f,
                        (prevVal.y - prev2Val.y) * 0.5f,
                        10f);
                    dir = dir.normalized * 50f;

                    var newCp = controlPointsProp.GetArrayElementAtIndex(idx);
                    newCp.vector3Value = new Vector3(
                        prevVal.x + dir.x,
                        prevVal.y + dir.y,
                        prevVal.z);
                }
                else
                {
                    var newCp = controlPointsProp.GetArrayElementAtIndex(0);
                    newCp.vector3Value = Vector3.zero;
                }
            }
        }

        private void RemoveControlPointAt(int index)
        {
            controlPointsProp.DeleteArrayElementAtIndex(index);
        }

        private void DrawArrowsList()
        {
            if (arrowsProp == null) return;

            for (int i = 0; i < arrowsProp.arraySize; i++)
            {
                var arrow = arrowsProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var shapeProp = arrow.FindPropertyRelative("shape");
                var reversedProp = arrow.FindPropertyRelative("reversed");
                var progressProp = arrow.FindPropertyRelative("progress");
                var widthProp = arrow.FindPropertyRelative("width");
                var heightProp = arrow.FindPropertyRelative("height");
                var depthProp = arrow.FindPropertyRelative("depthCompensation");
                var colorProp = arrow.FindPropertyRelative("color");

                EditorGUILayout.PropertyField(shapeProp, new GUIContent("形状"));
                EditorGUILayout.PropertyField(reversedProp, new GUIContent("反向"));
                EditorGUILayout.Slider(progressProp, 0f, 1f, new GUIContent("进度"));
                EditorGUILayout.PropertyField(widthProp, new GUIContent("宽度"));
                EditorGUILayout.PropertyField(heightProp, new GUIContent("高度"));
                EditorGUILayout.Slider(depthProp, -1f, 1f, new GUIContent("深度补偿"));
                EditorGUILayout.PropertyField(colorProp, new GUIContent("颜色"));

                if (GUILayout.Button("× 删除此箭头"))
                {
                    arrowsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ 添加箭头"))
            {
                int idx = arrowsProp.arraySize;
                arrowsProp.arraySize++;

                var newArrow = arrowsProp.GetArrayElementAtIndex(idx);
                var shapeProp = newArrow.FindPropertyRelative("shape");
                var reversedProp = newArrow.FindPropertyRelative("reversed");
                var progressProp = newArrow.FindPropertyRelative("progress");
                var widthProp = newArrow.FindPropertyRelative("width");
                var heightProp = newArrow.FindPropertyRelative("height");
                var depthProp = newArrow.FindPropertyRelative("depthCompensation");
                var colorProp = newArrow.FindPropertyRelative("color");

                shapeProp.enumValueIndex = 0;
                reversedProp.boolValue = false;
                progressProp.floatValue = 0.5f;
                widthProp.floatValue = 20f;
                heightProp.floatValue = 20f;
                depthProp.floatValue = 0f;
                colorProp.colorValue = Color.white;
            }
        }
    }
}
