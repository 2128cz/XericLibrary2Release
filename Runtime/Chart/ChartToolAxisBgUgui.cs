using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Chart
{
    /// <summary>
    /// 2D 轴线网格渲染工具。
    ///
    /// 创建 Image 子组件，加载 GridLine shader 实例化材质，
    /// 通过材质 _Transform 参数控制网格的缩放和平移。
    ///
    /// 可见范围由基类 VisibleRanges[4] 提供（每维度独立配置），
    /// 本工具在 OnRender 中读取基类提供的范围计算 UV 变换。
    ///
    /// 子节点结构：
    ///   [ChartTool_AxisBgUgui]
    ///     └─ BackgroundImage (Image)
    /// </summary>
    [AddComponentMenu("Xeric/Chart/2D Axis Tool")]
    public class ChartToolAxisBgUgui : ChartToolBase
    {
        // ── Shader 资源路径 ────────────────────────────────────────

        private const string k_GridShaderPath =
            "Material/Graph/UI/Chart/ChartAxisBackground-GridLine";

        private const string k_TransformProp = "_Transform";
        private const string k_GridColorProp = "_GridColor";
        private const string k_BgColorProp = "_GridBackgroundColor";

        // ── 外观配置 ──────────────────────────────────────────────

        /// <summary>网格线颜色</summary>
        public Color GridColor { get; set; } = Color.white;

        /// <summary>背景颜色</summary>
        public Color BackgroundColor { get; set; } = new Color(0.15f, 0.15f, 0.15f, 1f);

        /// <summary>网格每轴分割数（如 10 表示 10×10 网格）</summary>
        public float GridDivisions { get; set; } = 10f;

        // ── 内部组件 ──────────────────────────────────────────────

        /// <summary>Image 组件（挂载材质）</summary>
        public Image BackgroundImage { get; private set; }

        /// <summary>实例化的网格材质</summary>
        public Material GridMaterial { get; private set; }

        // ── 初始化 ────────────────────────────────────────────────

        protected override void OnInitialize()
        {
            // 1. 设置默认可见范围（对应 X 和 Y 维度）
            SetViewRange(0, ChartViewRange.ValueMode(-100f, 100f, 200)); // X
            SetViewRange(1, ChartViewRange.ValueMode(-100f, 100f, 200)); // Y

            // 2. 创建 Image 子组件
            BackgroundImage = CreateChildComponent<Image>("BackgroundImage");

            // 3. 加载 GridLine shader
            Shader gridShader = Resources.Load<Shader>(k_GridShaderPath);
            if (gridShader == null)
            {
                Debug.LogError("[charttool_axisugui] 未找到 GridLine shader: " + k_GridShaderPath);
                return;
            }

            // 4. 实例化材质
            GridMaterial = new Material(gridShader);
            GridMaterial.name = "GridMat_Instance";

            // 5. 材质挂在 Image 上
            if (BackgroundImage != null)
            {
                BackgroundImage.material = GridMaterial;
                BackgroundImage.raycastTarget = false;
                BackgroundImage.color = Color.white;

                var rt = BackgroundImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // 6. 初始材质参数
            ApplyMaterialSettings();

            SetVisible(true);
        }

        // ── 材质参数更新 ──────────────────────────────────────────

        /// <summary>
        /// 根据 Image 尺寸和基类可见范围计算 _Transform.xyzw，更新材质。
        /// </summary>
        private void ApplyMaterialSettings()
        {
            if (GridMaterial == null || BackgroundImage == null) return;

            GridMaterial.SetColor(k_GridColorProp, GridColor);
            GridMaterial.SetColor(k_BgColorProp, BackgroundColor);

            var rt = BackgroundImage.rectTransform;
            Vector2 imageSize = rt.rect.size;
            if (imageSize.x < 1f || imageSize.y < 1f)
                imageSize = new Vector2(800f, 600f);

            Vector4 transform = CalculateTransform(imageSize);
            GridMaterial.SetVector(k_TransformProp, transform);
        }

        /// <summary>
        /// 计算材质 _Transform 参数：
        ///   _Transform.xy = UV 缩放（网格密度）
        ///   _Transform.zw = UV 偏移（平移）
        ///
        /// 使用基类 VisibleRanges[0]（X 轴范围）和 VisibleRanges[1]（Y 轴范围）。
        /// </summary>
        private Vector4 CalculateTransform(Vector2 imageSize)
        {
            float scaleX = GridDivisions;
            float scaleY = GridDivisions;
            float offsetX = 0f;
            float offsetY = 0f;

            var xRange = GetViewRange(0);
            var yRange = GetViewRange(1);

            if (xRange.Mode == ViewRangeMode.Value && xRange.ValueSpan > 0f)
            {
                // X 轴：值范围模式
                float cellsX = GridDivisions;
                scaleX = cellsX;
                offsetX = -xRange.ValueMin / xRange.ValueSpan * cellsX;
            }
            else if (xRange.Mode == ViewRangeMode.Index && xRange.IndexCount > 0)
            {
                // X 轴：索引范围模式
                scaleX = xRange.IndexCount;
            }

            if (yRange.Mode == ViewRangeMode.Value && yRange.ValueSpan > 0f)
            {
                // Y 轴：值范围模式
                float cellsY = GridDivisions;
                scaleY = cellsY * (imageSize.y / imageSize.x);
                offsetY = -yRange.ValueMin / yRange.ValueSpan * scaleY;
            }
            else if (yRange.Mode == ViewRangeMode.Index && yRange.IndexCount > 0)
            {
                // Y 轴：索引范围模式
                scaleY = yRange.IndexCount * (imageSize.y / imageSize.x);
            }

            return new Vector4(scaleX, scaleY, offsetX, offsetY);
        }

        // ── 渲染 ──────────────────────────────────────────────────

        public override void OnRender()
        {
            base.OnRender();

            if (GridMaterial != null)
            {
                GridMaterial.SetColor(k_GridColorProp, GridColor);
                GridMaterial.SetColor(k_BgColorProp, BackgroundColor);

                if (BackgroundImage != null)
                {
                    var rt = BackgroundImage.rectTransform;
                    Vector2 imageSize = rt.rect.size;
                    if (imageSize.x > 0f && imageSize.y > 0f)
                    {
                        GridMaterial.SetVector(k_TransformProp, CalculateTransform(imageSize));
                    }
                }
            }
        }

        // ── 清理 ──────────────────────────────────────────────────

        protected override void OnDispose()
        {
            if (GridMaterial != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(GridMaterial);
                else
                    Object.DestroyImmediate(GridMaterial);
                GridMaterial = null;
            }
            BackgroundImage = null;
        }
    }
}
