using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 曲线绘制器组件
    /// 支持多阶贝塞尔曲线，可配置控制点、宽度渐变、颜色和箭头装饰
    /// </summary>
    [AddComponentMenu("Xeric Library/UI/UICurveRenderer", 16)]
    public class UICurveRenderer : CurveCacheRendererBase
    {
        /// <summary>控制点，(x,y)=位置, z=宽度</summary>
        public Vector3[] controlPoints = new Vector3[]
        {
            new Vector3(-150f, 0f, 10f),
            new Vector3(-50f, 100f, 10f),
            new Vector3(50f, -100f, 10f),
            new Vector3(150f, 0f, 10f),
        };

        public Color32 startColor = Color.white;
        public Color32 endColor = Color.white;

        [Range(4, 128)]
        public int tessellationSegments = 32;

        public List<ArrowHeadData> arrows = new List<ArrowHeadData>();

        public bool autoRebuild = true;

        private CurveCacheEntry? m_CurrentEntry;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (autoRebuild) RebuildCurve();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (controlPoints == null || controlPoints.Length < 2)
                controlPoints = new Vector3[] { new Vector3(0f, 0f, 10f), new Vector3(100f, 0f, 10f) };
            if (tessellationSegments < 4) tessellationSegments = 4;
            if (autoRebuild && isActiveAndEnabled)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this != null && isActiveAndEnabled)
                        RebuildCurve();
                };
            }
        }
#endif

        /// <summary>
        /// 重建曲线
        /// </summary>
        public void RebuildCurve()
        {
            ClearAll();

            if (controlPoints == null || controlPoints.Length < 2) return;

            // 从 Vector3[] 中提取 Vector2[] 位置和 float[] 宽度
            int count = controlPoints.Length;
            var ctrlPts2 = new Vector2[count];
            var widths = new float[count];
            for (int i = 0; i < count; i++)
            {
                ctrlPts2[i] = (Vector2)controlPoints[i];
                widths[i] = controlPoints[i].z;
            }

            var entry = new CurveCacheEntry
            {
                startColor = startColor,
                endColor = endColor,
                tessellationSegments = tessellationSegments,
            };

            AddCurve(entry, ctrlPts2, widths, arrows?.ToArray());
            m_CurrentEntry = m_Curves.Count > 0 ? m_Curves[0] : (CurveCacheEntry?)null;
        }

        /// <summary>
        /// 设置控制点（z=宽度）
        /// </summary>
        public void SetControlPoints(Vector3[] points)
        {
            controlPoints = points;
            RebuildCurve();
        }

        /// <summary>
        /// 添加箭头
        /// </summary>
        public void AddArrow(ArrowHeadData arrow)
        {
            if (arrows == null) arrows = new List<ArrowHeadData>();
            arrows.Add(arrow);
            RebuildCurve();
        }

        /// <summary>
        /// 移除箭头
        /// </summary>
        public void RemoveArrow(int index)
        {
            if (arrows == null || index < 0 || index >= arrows.Count) return;
            arrows.RemoveAt(index);
            RebuildCurve();
        }
    }
}
