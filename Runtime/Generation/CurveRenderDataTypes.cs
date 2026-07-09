using System;
using UnityEngine;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 箭头形状枚举
    /// </summary>
    public enum ArrowShape : byte
    {
        /// <summary>实心三角箭头</summary>
        Triangle,
        /// <summary>空心三角箭头</summary>
        HollowTriangle,
        /// <summary>箭矢箭头（翼+杆）</summary>
        Arrow,
        /// <summary>圆形头</summary>
        Circle,
        /// <summary>方形头</summary>
        Square,
        /// <summary>反向箭头（箭尾）</summary>
        ReverseArrow,
    }

    /// <summary>
    /// 箭头装饰数据
    /// </summary>
    [Serializable]
    public struct ArrowHeadData
    {
        /// <summary>箭头形状</summary>
        public ArrowShape shape;
        /// <summary>是否反向</summary>
        public bool reversed;
        /// <summary>箭头出现在曲线上的位置 (0~1)</summary>
        [Range(0f, 1f)]
        public float progress;
        /// <summary>箭头宽度（沿切线方向）</summary>
        public float width;
        /// <summary>箭头高度（垂直切线方向）</summary>
        public float height;
        /// <summary>深度补偿：-1~1，箭头沿切线偏移 width*0.5*depth 距离。
        /// -1：尾部对准曲线点（头部向前伸）
        ///  0：中心对准曲线点
        /// +1：头部对准曲线点（尾部向后拉）</summary>
        [Range(-1f, 1f)]
        public float depthCompensation;
        /// <summary>箭头颜色</summary>
        public Color32 color;
    }

    /// <summary>
    /// 曲线缓存条目
    /// 控制点、宽度、箭头数据统一存放在外部扁平列表中，
    /// 本结构体只记录起始索引和计数。
    /// </summary>
    [Serializable]
    public struct CurveCacheEntry
    {
        /// <summary>在网格顶点缓存中的起始索引（由 Base 类回填）</summary>
        public int startVertexIndex;
        /// <summary>该曲线占用的顶点数（由 Base 类回填）</summary>
        public int vertexCount;
        /// <summary>在网格索引缓存中的起始索引（由 Base 类回填）</summary>
        public int startIndexIndex;
        /// <summary>三角形数量（由 Base 类回填）</summary>
        public int triangleCount;

        /// <summary>控制点数量（≥4，默认三阶贝塞尔）</summary>
        public int controlPointCount;
        /// <summary>在统一控制点缓存中的起始索引</summary>
        public int controlPointStartIndex;
        /// <summary>在统一宽度缓存中的起始索引</summary>
        public int widthStartIndex;
        /// <summary>起始颜色</summary>
        public Color32 startColor;
        /// <summary>结束颜色</summary>
        public Color32 endColor;
        /// <summary>在统一箭头缓存中的起始索引（-1 表示无箭头）</summary>
        public int arrowStartIndex;
        /// <summary>箭头数量</summary>
        public int arrowCount;
        /// <summary>曲线细分段数</summary>
        public int tessellationSegments;
    }
}
