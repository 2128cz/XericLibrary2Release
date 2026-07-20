using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 图元绘制器组件
    /// 支持多边形/矩形的内切圆/外切圆/椭圆等尺寸模式
    /// 参数写入 UV2-UV7 通道
    /// </summary>
    [AddComponentMenu("Xeric Library/UI/UIPrimitiveRenderer", 15)]
    public class UIPrimitiveRenderer : PrimitiveCacheRendererBase
    {
        public PrimitiveType primitiveType = PrimitiveType.Polygon;

		public SizeMode sizeMode = SizeMode.InscribedCircle;
		public Vector2 size = new Vector2(100f, 100f);
		public Vector2 centerOffset = Vector2.zero;
		[Range(-180f, 180f)]
		public float angle = 0f;

        [Range(3, 64)]
        public int sideCount = 6;

        [Range(0f, 1f)]
        public float chamferSize = 0f;

        [Range(1, 16)]
        public int chamferSegments = 4;

        public Color bgColor = Color.gray;
        public Color centerColor = Color.white;
        public Color borderColor = Color.black;

        [Range(0f, 50f)]
        public float borderThickness = 2f;

        public bool autoRebuild = true;

        private PrimitiveCacheEntry? m_CurrentEntry;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (autoRebuild) RebuildPrimitive();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (sideCount < 3) sideCount = 3;
            if (autoRebuild && isActiveAndEnabled)
            {
                // 使用 EditorApplication.delayCall 避免 OnValidate 中的 SetVerticesDirty 警告
                EditorApplication.delayCall += () =>
                {
                    if (this != null && isActiveAndEnabled)
                        RebuildPrimitive();
                };
            }
        }
#endif

        /// <summary>
        /// 重建图元
        /// </summary>
        public void RebuildPrimitive()
        {
            // 清除旧的图元
            ClearAll();

            var entry = new PrimitiveCacheEntry
            {
                type = primitiveType,
                sizeMode = sizeMode,
                center = centerOffset,
                size = size,
                angle = angle,
                @params = new PrimitiveParams
                {
                    axisScaleX = 1f,
                    axisScaleY = 1f,
                    sideCount = sideCount,
                    chamferSize = chamferSize,
                    chamferSegments = chamferSegments,
                    bgColor = bgColor,
                    centerColor = centerColor,
                    borderColor = borderColor,
                    borderThickness = borderThickness,
                }
            };

            AddPrimitive(entry);
            m_CurrentEntry = entry;
        }

        /// <summary>
        /// 更新图元参数（不触发完全重建，只标记脏）
        /// </summary>
        public void UpdateParams(System.Action<PrimitiveCacheEntry> updater)
        {
            if (m_Primitives.Count == 0) return;
            var entry = m_Primitives[0];
            updater?.Invoke(entry);
            m_Primitives[0] = entry;
            SetPrimitiveDirty(0);
        }

        #region 便捷方法

        /// <summary>
        /// 快速设置为正多边形
        /// </summary>
        public void SetPolygon(int sides, float radius, Color? fill = null, Color? border = null)
        {
            primitiveType = PrimitiveType.Polygon;
            sizeMode = SizeMode.InscribedCircle;
            size = new Vector2(radius * 2f, radius * 2f);
            sideCount = Mathf.Max(3, sides);
            if (fill.HasValue) bgColor = fill.Value;
            if (border.HasValue) borderColor = border.Value;
            RebuildPrimitive();
        }

        /// <summary>
        /// 快速设置为矩形
        /// </summary>
        public void SetRectangle(float width, float height, Color? fill = null, Color? border = null)
        {
            primitiveType = PrimitiveType.Rectangle;
            sizeMode = SizeMode.InscribedEllipse;
            size = new Vector2(width, height);
            if (fill.HasValue) bgColor = fill.Value;
            if (border.HasValue) borderColor = border.Value;
            RebuildPrimitive();
        }

        #endregion
    }
}
