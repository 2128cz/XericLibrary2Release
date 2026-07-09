using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 新一代线段渲染器组件
    /// 基于 CacheLineRendererBase 的缓存 + Jobs 架构
    /// </summary>
    [AddComponentMenu("Xeric Library/UI/UILineRenderer V2", 15)]
    public class UILineRendererV2 : CacheLineRendererBase
    {
        [Header("全局默认设置")]
        [Range(0.01f, 200f)]
        [Tooltip("默认线段宽度")]
        public float defaultThickness = 10f;

        [Tooltip("默认线段颜色")]
        public Color defaultColor = Color.white;

        [Tooltip("默认 UV1 均分模式")]
        public UVMode defaultUVMode = UVMode.ByDistance;

        /// <summary>
        /// 绘制一条简单线段（2个端点）
        /// </summary>
        public void DrawLine(Vector2 from, Vector2 to)
        {
            DrawLine(from, to, defaultColor, defaultThickness, defaultUVMode);
        }

        /// <summary>
        /// 绘制一条简单线段（指定颜色和宽度）
        /// </summary>
        public void DrawLine(Vector2 from, Vector2 to, Color color, float thickness, UVMode uvMode = UVMode.ByDistance)
        {
            BeginLine(uvMode);
            AddLineNode(from, color, thickness);
            AddLineNode(to, color, thickness);
            EndLine();
        }

        /// <summary>
        /// 绘制折线（多点连接）
        /// </summary>
        public void DrawPolyline(Vector2[] points, Color color, float thickness, UVMode uvMode = UVMode.ByDistance)
        {
            if (points == null || points.Length < 2) return;

            LineVertexData[] vertices = new LineVertexData[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                vertices[i] = new LineVertexData
                {
                    position = points[i],
                    color = color,
                    thickness = thickness,
                    userData = Vector4.zero
                };
            }
            AddLine(vertices, uvMode);
        }

        /// <summary>
        /// 绘制折线（每个端点独立颜色/宽度）
        /// </summary>
        public void DrawPolyline(LineVertexData[] vertices, UVMode uvMode = UVMode.ByDistance)
        {
            if (vertices == null || vertices.Length < 2) return;
            AddLine(vertices, uvMode);
        }

        /// <summary>
        /// 简单绘制矩形框
        /// </summary>
        public void DrawRect(Rect rect, Color color, float thickness)
        {
            Vector2 center = rect.center;
            float hw = rect.width * 0.5f;
            float hh = rect.height * 0.5f;

            var pts = new Vector2[]
            {
                new Vector2(center.x - hw, center.y - hh),
                new Vector2(center.x + hw, center.y - hh),
                new Vector2(center.x + hw, center.y + hh),
                new Vector2(center.x - hw, center.y + hh),
                new Vector2(center.x - hw, center.y - hh),
            };
            DrawPolyline(pts, color, thickness, UVMode.ByDistance);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (defaultThickness < 0.01f)
                defaultThickness = 0.01f;
        }

        [MenuItem("GameObject/Xeric Library/UI/UILineRenderer V2", false, 10)]
        private static void CreateGameObject()
        {
            var go = new GameObject(nameof(UILineRendererV2), typeof(UILineRendererV2));
            if (Selection.activeGameObject != null)
                go.transform.SetParent(Selection.activeGameObject.transform);
            Undo.RegisterCreatedObjectUndo(go, $"Create {nameof(UILineRendererV2)}");
            Selection.activeObject = go;
        }
#endif
    }
}
