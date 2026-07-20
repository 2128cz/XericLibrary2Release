using System.Reflection;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Chart
{
    /// <summary>
    /// 折线图表渲染工具。
    ///
    /// 创建 UILineRendererV2 子组件绘制折线。
    /// 在 OnRender 中将数据坐标映射为图表区域局部坐标后绘制。
    ///
    /// 子节点结构：
    ///   [charttool_line_ugui]
    ///     └─ LineRenderer (UILineRendererV2)
    /// </summary>
    [AddComponentMenu("Xeric/Chart/Line Chart Tool")]
    public class ChartToolLineUgui : ChartToolBase
    {
        // ── 外观配置 ──────────────────────────────────────────────

        /// <summary>折线颜色</summary>
        public Color LineColor { get; set; } = new Color(0.33f, 0.67f, 1f, 1f);

        /// <summary>折线宽度</summary>
        public float LineThickness { get; set; } = 4f;

        /// <summary>图表内边距（左、下、右、上，像素）</summary>
        public Vector4 Margin { get; set; } = new Vector4(50f, 40f, 20f, 20f);

        /// <summary>是否自动清除旧线段</summary>
        public bool AutoClear { get; set; } = true;

        /// <summary>是否启用 Catmull-Rom 平滑线</summary>
        public bool SmoothLine { get; set; }

        /// <summary>平滑分段数</summary>
        public int SmoothSegments { get; set; } = 8;

        // ── 内部组件 ──────────────────────────────────────────────

        /// <summary>折线渲染器</summary>
        public UILineRendererV2 LineRenderer { get; private set; }

        // ── 反射助手 ──────────────────────────────────────────────

        private static FieldInfo s_VertexField;
        private static FieldInfo s_SegmentField;
        private static FieldInfo s_DirtyStartField;

        private static void EnsureReflection()
        {
            if (s_VertexField != null) return;

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var baseType = typeof(CacheLineRendererBase);
            s_VertexField = baseType.GetField("m_VertexData", flags);
            s_SegmentField = baseType.GetField("m_SegmentRanges", flags);
            s_DirtyStartField = baseType.GetField("m_DirtyStartSegment", flags);
        }

        /// <summary>清除线段渲染器内部缓存</summary>
        private static void ClearLineInternal(CacheLineRendererBase renderer)
        {
            if (renderer == null) return;
            EnsureReflection();

            var vList = s_VertexField?.GetValue(renderer) as System.Collections.Generic.List<LineVertexData>;
            var sList = s_SegmentField?.GetValue(renderer) as System.Collections.Generic.List<LineSegmentRange>;

            vList?.Clear();
            sList?.Clear();
            s_DirtyStartField?.SetValue(renderer, 0);
        }

        // ── 初始化 ────────────────────────────────────────────────

        protected override void OnInitialize()
        {
            // 1. 默认可见范围
            SetViewRange(0, ChartViewRange.ValueMode(-100f, 100f, 200));
            SetViewRange(1, ChartViewRange.ValueMode(-100f, 100f, 200));

            // 2. 创建线段渲染器
            LineRenderer = CreateChildComponent<UILineRendererV2>("LineRenderer");
            if (LineRenderer != null)
            {
                LineRenderer.defaultColor = LineColor;
                LineRenderer.defaultThickness = LineThickness;

                var rt = LineRenderer.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(Margin.x, Margin.y);
                rt.offsetMax = new Vector2(-Margin.z, -Margin.w);
            }

            SetVisible(true);
        }

        // ── 渲染 ──────────────────────────────────────────────────

        public override void OnRender()
        {
            base.OnRender();
            if (LineRenderer == null || DataModel == null) return;

            var xRange = GetViewRange(0);
            var yRange = GetViewRange(1);

            // 创建泛型索引器
            var xData = DataModel.GetDimension(0);
            var yData = DataModel.GetDimension(1);
            var xIndexer = xRange.CreateIndexer(xData);
            var yIndexer = yRange.CreateIndexer(yData);

            int pointCount = Mathf.Min(xIndexer.Count, yIndexer.Count);
            if (pointCount < 2) return;

            var rt = LineRenderer.rectTransform;
            Vector2 plotSize = rt.rect.size;
            if (plotSize.x < 1f || plotSize.y < 1f) return;

            // 生成局部坐标点
            Vector2[] rawPoints = new Vector2[pointCount];
            for (int i = 0; i < pointCount; i++)
                rawPoints[i] = MapVisiblePoint(i, xIndexer, yIndexer, xRange, yRange, plotSize);

            // 平滑
            if (SmoothLine && rawPoints.Length > 2)
                rawPoints = GenerateSmoothPoints(rawPoints, SmoothSegments);

            // 清空旧数据 + 绘制新折线
            if (AutoClear)
                ClearLineInternal(LineRenderer);

            LineVertexData[] vertices = new LineVertexData[rawPoints.Length];
            for (int i = 0; i < rawPoints.Length; i++)
            {
                vertices[i] = new LineVertexData
                {
                    position = rawPoints[i],
                    color = LineColor,
                    thickness = LineThickness,
                    userData = Vector4.zero,
                };
            }
            LineRenderer.DrawPolyline(vertices, UVMode.ByDistance);
        }

        // ── 坐标映射 ──────────────────────────────────────────────

        /// <summary>将可见范围内的第 visibleIndex 个数据点映射为图表局部坐标</summary>
        private static Vector2 MapVisiblePoint(int visibleIndex,
            ChartVisibleIndexer<double> xIdx, ChartVisibleIndexer<double> yIdx,
            ChartViewRange xRange, ChartViewRange yRange,
            Vector2 plotSize)
        {
            float px, py;

            if (xRange.Mode == ViewRangeMode.Index)
            {
                // X 轴为索引模式 → 按索引比例均分宽度
                int count = Mathf.Max(xIdx.Count, 1);
                px = (float)visibleIndex / (count - 1) * plotSize.x;
            }
            else
            {
                // X 轴为值模式 → 按数据值映射
                double dataX = xIdx[visibleIndex];
                px = xRange.ValueSpan > 0f
                    ? (float)((dataX - xRange.ValueMin) / xRange.ValueSpan * plotSize.x)
                    : 0f;
            }

            if (yRange.Mode == ViewRangeMode.Index)
            {
                // Y 轴为索引模式 → Y 数据值映射到高度
                double dataY = yIdx[visibleIndex];
                py = yRange.ValueSpan > 0f
                    ? (float)((dataY - yRange.ValueMin) / yRange.ValueSpan * plotSize.y)
                    : 0f;
            }
            else
            {
                // Y 轴为值模式
                double dataY = yIdx[visibleIndex];
                py = yRange.ValueSpan > 0f
                    ? (float)((dataY - yRange.ValueMin) / yRange.ValueSpan * plotSize.y)
                    : 0f;
            }

            return new Vector2(px, py);
        }

        // ── 平滑插值 ──────────────────────────────────────────────

        private static Vector2[] GenerateSmoothPoints(Vector2[] points, int segs)
        {
            int n = points.Length;
            if (n < 3) return points;

            var result = new Vector2[(n - 1) * segs + 1];

            int idx = 0;
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = i > 0 ? points[i - 1] : points[i];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = i < n - 2 ? points[i + 2] : points[i + 1];

                for (int j = 0; j < segs; j++)
                {
                    float t = j / (float)segs;
                    result[idx++] = CatmullRom(p0, p1, p2, p3, t);
                }
            }
            result[idx] = points[n - 1];

            return result;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            float a = -0.5f * t3 + 1.0f * t2 - 0.5f * t;
            float b = 1.5f * t3 - 2.5f * t2 + 1.0f;
            float c = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
            float d = 0.5f * t3 - 0.5f * t2;
            return a * p0 + b * p1 + c * p2 + d * p3;
        }

        // ── 清理 ──────────────────────────────────────────────────

        protected override void OnDispose()
        {
            LineRenderer = null;
        }
    }
}
