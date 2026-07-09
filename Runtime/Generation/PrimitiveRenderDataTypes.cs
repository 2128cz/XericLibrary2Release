using UnityEngine;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 图元类型
    /// </summary>
    public enum PrimitiveType : byte
    {
        /// <summary>正多边形（内切圆/外切圆）</summary>
        Polygon,
        /// <summary>矩形（内切椭圆/外切圆比例）</summary>
        Rectangle,
    }

    /// <summary>
    /// 尺寸模式
    /// </summary>
    public enum SizeMode : byte
    {
        /// <summary>内切圆 — 半径延伸到各边中点</summary>
        InscribedCircle,
        /// <summary>外切圆 — 半径延伸到各顶点</summary>
        CircumscribedCircle,
        /// <summary>内切椭圆 — XY各自独立的内切半径</summary>
        InscribedEllipse,
        /// <summary>外切圆比例 — 外切圆半径 + XY比例</summary>
        CircumscribedRatio,
    }

    /// <summary>
    /// 图元参数
    /// 顶点通道分配：
    ///   uv0: 面片内归一化坐标 (0~1)
    ///   uv1: (centerDist, isBorder)
    ///   normal: (bg.r, bg.g, bg.b) — 0~1
    ///   tangent: (border.r, border.g, border.b, border.a) — 0~1
    /// </summary>
    public struct PrimitiveParams
    {
        public float axisScaleX;
        public float axisScaleY;
        public float sideCount;
        public float chamferSize;
        /// <summary>倒角细分段数（仅网格生成使用）</summary>
        public int chamferSegments;
        public Color32 bgColor;
        public Color32 centerColor;
        public Color32 borderColor;
        public float borderThickness;
    }

    /// <summary>
    /// 图元缓存条目
    /// </summary>
    public struct PrimitiveCacheEntry
    {
        /// <summary>在网格顶点缓存中的起始索引</summary>
        public int startVertexIndex;
        /// <summary>该图元占用的顶点数</summary>
        public int vertexCount;
        /// <summary>在网格索引缓存中的起始索引</summary>
        public int startIndexIndex;
        /// <summary>三角形数量</summary>
        public int triangleCount;

        public PrimitiveType type;
        public SizeMode sizeMode;

        /// <summary>图元中心（局部坐标）</summary>
        public Vector2 center;
        /// <summary>图元尺寸</summary>
        public Vector2 size;
        /// <summary>旋转角度（度数，0 = 正上方）</summary>
        public float angle;

        /// <summary>图元参数</summary>
        public PrimitiveParams @params;
    }
}
