using System;
using UnityEngine;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 线段端点数据（所有字段需为 blittable 类型以支持 NativeArray/Job）
    /// </summary>
    [Serializable]
    public struct LineVertexData
    {
        public Vector2 position;  // 端点位置（局部坐标）
        public Color32 color;     // 端点颜色
        public float thickness;   // 端点宽度
        public Vector4 userData;  // 自定义扩展数据 → 映射到 UV2
    }

    /// <summary>
    /// UV1 均分模式（每条线段独立）
    /// </summary>
    public enum UVMode : byte
    {
        /// <summary>按实际线路距离均分</summary>
        ByDistance,
        /// <summary>按线段数量均分</summary>
        BySegment,
    }

    /// <summary>
    /// 线段范围记录
    /// </summary>
    [Serializable]
    public struct LineSegmentRange
    {
        public int startVertexIndex;  // 在 vertexData 中的起始索引
        public int vertexCount;       // 该线段端点数量（>= 2）
        public UVMode uvMode;         // UV1 均分模式
    }

    /// <summary>
    /// Job 输入：单段线段的顶点生成参数
    /// </summary>
    [Serializable]
    public struct SegmentJobInput
    {
        public LineVertexData start;
        public LineVertexData end;
        public LineVertexData prevEnd;    // 上个线段终点（仅 hasPrev=1 时有效）
        public LineVertexData nextStart;  // 下个线段起点（仅 hasNext=1 时有效）
        public byte hasPrev;              // 0=无效, 1=有效
        public byte hasNext;              // 0=无效, 1=有效
        public int segmentIndex;
        public int totalSegmentCount;
        public float startLineUV;
        public float endLineUV;
        public UVMode uvMode;
    }
}
