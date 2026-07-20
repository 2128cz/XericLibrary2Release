using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_BURST
using Unity.Jobs;
using Unity.Collections;
#endif

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 基于缓存结构的线段渲染器基类
    /// 数据由 NativeArray 管理，支持脏标记范围更新和 Jobs 并行生成
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class CacheLineRendererBase : MaskableGraphic
    {
        #region 数据缓存

        /// <summary>
        /// 所有线段端点的扁平缓存
        /// </summary>
        protected List<LineVertexData> m_VertexData = new List<LineVertexData>();

        /// <summary>
        /// 线段范围数组
        /// </summary>
        protected List<LineSegmentRange> m_SegmentRanges = new List<LineSegmentRange>();

        /// <summary>
        /// 待添加的端点（Begin/End API 模式用）
        /// </summary>
        protected List<LineVertexData> m_PendingVertices = new List<LineVertexData>();

        /// <summary>
        /// 当前 Begin/End 模式的 uvMode
        /// </summary>
        protected UVMode m_PendingUVMode = UVMode.ByDistance;

        /// <summary>
        /// 是否正在 Begin/End 会话中
        /// </summary>
        protected bool m_IsBuilding;

        /// <summary>
        /// 模型顶点缓存 (segmentCount × 5)
        /// </summary>
        protected List<UIVertex> m_MeshVertexCache = new List<UIVertex>();

        /// <summary>
        /// 三角形索引缓存
        /// </summary>
        protected List<int> m_MeshIndexCache = new List<int>();

        /// <summary>
        /// 脏标记范围起点（线段范围索引），-1 表示无脏标记
        /// </summary>
        protected int m_DirtyStartSegment = -1;

        /// <summary>
        /// 脏标记范围终点（线段范围索引）
        /// </summary>
        protected int m_DirtyEndSegment = -1;

        /// <summary>
        /// 是否使用 Jobs 模式
        /// </summary>
        protected static bool UseJobs =>
#if ENABLE_BURST
            SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null &&
            Application.platform != RuntimePlatform.WebGLPlayer;
#else
            false;
#endif

        #endregion

        #region 线段添加

        /// <summary>
        /// 批量添加一根完整线段
        /// </summary>
        /// <param name="vertices">线段端点列表（至少2个）</param>
        /// <param name="uvMode">UV1 均分模式</param>
        /// <returns>线段范围索引</returns>
        public int AddLine(LineVertexData[] vertices, UVMode uvMode = UVMode.ByDistance)
        {
            if (vertices == null || vertices.Length < 2)
                return -1;

            int startIndex = m_VertexData.Count;
            m_VertexData.AddRange(vertices);

            var range = new LineSegmentRange
            {
                startVertexIndex = startIndex,
                vertexCount = vertices.Length,
                uvMode = uvMode
            };

            int rangeIndex = m_SegmentRanges.Count;
            m_SegmentRanges.Add(range);

            MarkDirty(rangeIndex);
            return rangeIndex;
        }

        /// <summary>
        /// 开始画线（记录模式）
        /// </summary>
        public void BeginLine(UVMode uvMode = UVMode.ByDistance)
        {
            m_IsBuilding = true;
            m_PendingUVMode = uvMode;
            m_PendingVertices.Clear();
        }

        /// <summary>
        /// 添加画线节点到当前记录
        /// </summary>
        public void AddLineNode(Vector2 position, Color32 color, float thickness, Vector4 userData = default)
        {
            if (!m_IsBuilding) return;
            m_PendingVertices.Add(new LineVertexData
            {
                position = position,
                color = color,
                thickness = thickness,
                userData = userData
            });
        }

        /// <summary>
        /// 添加画线节点
        /// </summary>
        public void AddLineNode(LineVertexData node)
        {
            if (!m_IsBuilding) return;
            m_PendingVertices.Add(node);
        }

        /// <summary>
        /// 结束画线，将挂起列表写入缓存
        /// </summary>
        public void EndLine()
        {
            if (!m_IsBuilding) return;
            m_IsBuilding = false;

            if (m_PendingVertices.Count < 2)
            {
                m_PendingVertices.Clear();
                return;
            }

            int startIndex = m_VertexData.Count;
            m_VertexData.AddRange(m_PendingVertices);

            var range = new LineSegmentRange
            {
                startVertexIndex = startIndex,
                vertexCount = m_PendingVertices.Count,
                uvMode = m_PendingUVMode
            };

            int rangeIndex = m_SegmentRanges.Count;
            m_SegmentRanges.Add(range);
            m_PendingVertices.Clear();

            MarkDirty(rangeIndex);
        }

        #endregion

        #region 线段管理

        /// <summary>
        /// 移除指定线段
        /// </summary>
        public void RemoveLine(int segmentRangeIndex)
        {
            if (segmentRangeIndex < 0 || segmentRangeIndex >= m_SegmentRanges.Count)
                return;

            m_SegmentRanges.RemoveAt(segmentRangeIndex);
            // 移除后后续所有线段索引发生变化，需完全重建
            RebuildAll();
        }

        /// <summary>
        /// 清空所有线段
        /// </summary>
        public void ClearAll()
        {
            m_VertexData.Clear();
            m_SegmentRanges.Clear();
            m_MeshVertexCache.Clear();
            m_MeshIndexCache.Clear();
            m_DirtyStartSegment = -1;
            m_DirtyEndSegment = -1;
            SetVerticesDirty();
        }

        /// <summary>
        /// 标记指定线段为脏（需要重建）
        /// </summary>
        public void SetSegmentDirty(int segmentRangeIndex)
        {
            MarkDirty(segmentRangeIndex);
        }

        #endregion

        #region 脏标记缓存

        /// <summary>
        /// 标记脏范围（合并到现有范围）
        /// </summary>
        protected void MarkDirty(int segmentIndex)
        {
            if (m_DirtyStartSegment < 0 || segmentIndex < m_DirtyStartSegment)
                m_DirtyStartSegment = segmentIndex;
            if (m_DirtyEndSegment < 0 || segmentIndex > m_DirtyEndSegment)
                m_DirtyEndSegment = segmentIndex;
        }

        /// <summary>
        /// 标记所有线段为脏（下次 LateUpdate 全量重绘）
        /// 适用于：更新顶点数据后，不改变元素数量的重绘
        /// </summary>
        public void RebuildAll()
        {
            m_DirtyStartSegment = 0;
            m_DirtyEndSegment = m_SegmentRanges.Count - 1;
            if (m_DirtyEndSegment < 0)
                m_DirtyStartSegment = -1;
        }

        #endregion

        #region 顶点生成

        protected virtual void LateUpdate()
        {
            if (m_DirtyStartSegment < 0)
                return;

            // 1) 统计脏范围内所有子段数（一个 range 有 vertexCount-1 个子段）
            int totalDirtySegments = 0;
            for (int ri = m_DirtyStartSegment; ri <= m_DirtyEndSegment; ri++)
                totalDirtySegments += m_SegmentRanges[ri].vertexCount - 1;

            if (totalDirtySegments == 0)
            {
                m_DirtyStartSegment = -1;
                m_DirtyEndSegment = -1;
                return;
            }

            // 2) 确保 mesh 顶点缓存足够大
            int totalSegments = GetTotalSegmentCount();
            int neededVertices = totalSegments * LineMeshGenerator.VerticesPerSegment;
            if (m_MeshVertexCache.Count < neededVertices)
            {
                int growBy = neededVertices - m_MeshVertexCache.Count;
                for (int i = 0; i < growBy; i++)
                    m_MeshVertexCache.Add(new UIVertex());
            }

            m_MeshIndexCache.Clear();

            // 3) 按子段构建 SegmentJobInput（同时计算 ByDistance 的 totalDist 缓存）
            int globalSegBase = GetGlobalSegmentOffset(m_DirtyStartSegment);
            var inputs = new List<SegmentJobInput>(totalDirtySegments);
            int inSegIdx = 0;
            for (int ri = m_DirtyStartSegment; ri <= m_DirtyEndSegment; ri++)
            {
                var range = m_SegmentRanges[ri];
                float cachedTotalDist = (range.uvMode == UVMode.ByDistance)
                    ? CalculateRangeTotalDistance(range)
                    : -1f;
                int subSegCount = range.vertexCount - 1;
                for (int si = 0; si < subSegCount; si++)
                {
                    inputs.Add(BuildSegmentInput(range, si,
                        globalSegBase + inSegIdx, totalSegments, cachedTotalDist));
                    inSegIdx++;
                }
            }

            // 4) 生成顶点（串行或 Jobs）
            if (UseJobs)
            {
#if ENABLE_BURST
                GenerateMeshJobs(inputs, globalSegBase);
#endif
            }
            else
            {
                GenerateMeshSerial(inputs, globalSegBase);
            }

            // 5) 生成三角形索引
            GenerateIndicesForRange(m_DirtyStartSegment, m_DirtyEndSegment,
                globalSegBase);

            // 清除脏标记
            SetVerticesDirty();
            m_DirtyStartSegment = -1;
            m_DirtyEndSegment = -1;
        }

        /// <summary>
        /// 串行生成顶点
        /// </summary>
        private void GenerateMeshSerial(List<SegmentJobInput> inputs, int globalSegBase)
        {
            var tempVerts = new UIVertex[LineMeshGenerator.VerticesPerSegment];
            for (int i = 0; i < inputs.Count; i++)
            {
                int meshVertStart = (globalSegBase + i) * LineMeshGenerator.VerticesPerSegment;
                LineMeshGenerator.GenerateSingleSegment(inputs[i], tempVerts, 0);
                for (int v = 0; v < LineMeshGenerator.VerticesPerSegment; v++)
                {
                    int cacheIdx = meshVertStart + v;
                    if (cacheIdx < m_MeshVertexCache.Count)
                        m_MeshVertexCache[cacheIdx] = tempVerts[v];
                }
            }
        }

#if ENABLE_BURST
        /// <summary>
        /// Jobs 并行生成顶点
        /// </summary>
        private void GenerateMeshJobs(List<SegmentJobInput> inputs, int globalSegBase)
        {
            int count = inputs.Count;
            int outputSize = count * LineMeshGenerator.VerticesPerSegment;

            var nativeInputs = new NativeArray<SegmentJobInput>(count, Allocator.TempJob);
            var nativeOutputs = new NativeArray<UIVertex>(outputSize, Allocator.TempJob);

            for (int i = 0; i < count; i++)
                nativeInputs[i] = inputs[i];

            var handle = LineMeshGenerator.GenerateSegmentsJobs(nativeInputs, nativeOutputs);
            handle.Complete();

            for (int i = 0; i < count; i++)
            {
                int srcStart = i * LineMeshGenerator.VerticesPerSegment;
                int dstStart = (globalSegBase + i) * LineMeshGenerator.VerticesPerSegment;
                for (int v = 0; v < LineMeshGenerator.VerticesPerSegment; v++)
                {
                    if (dstStart + v < m_MeshVertexCache.Count)
                        m_MeshVertexCache[dstStart + v] = nativeOutputs[srcStart + v];
                }
            }

            nativeInputs.Dispose();
            nativeOutputs.Dispose();
        }
#endif

        /// <summary>
        /// 生成指定 range 范围中所有子段的三角形索引
        /// 每条线的首个子段不生成转角连接三角形
        /// </summary>
        private void GenerateIndicesForRange(int startRange, int endRange,
            int globalSegBase)
        {
            int segIdx = globalSegBase;
            for (int ri = startRange; ri <= endRange; ri++)
            {
                int subSegCount = m_SegmentRanges[ri].vertexCount - 1;
                for (int si = 0; si < subSegCount; si++)
                {
                    // 子段 si == 0 是该条线的首段 → 无前驱同线段 → hasPrevInLine = false
                    bool hasPrevInLine = (si > 0);
                    LineMeshGenerator.GenerateSegmentIndices(segIdx, hasPrevInLine, m_MeshIndexCache);
                    segIdx++;
                }
            }
        }

        /// <summary>
        /// 计算某条线段包含的段数（端点数量 - 1）
        /// </summary>
        protected static int GetSegmentCountForRange(in LineSegmentRange range) => range.vertexCount - 1;

        /// <summary>
        /// 获取总段数（所有线段累加）
        /// </summary>
        protected int GetTotalSegmentCount()
        {
            int count = 0;
            for (int i = 0; i < m_SegmentRanges.Count; i++)
                count += GetSegmentCountForRange(m_SegmentRanges[i]);
            return count;
        }

        /// <summary>
        /// 获取指定线段范围的全局段偏移
        /// </summary>
        protected int GetGlobalSegmentOffset(int rangeIndex)
        {
            int offset = 0;
            for (int i = 0; i < rangeIndex; i++)
                offset += GetSegmentCountForRange(m_SegmentRanges[i]);
            return offset;
        }

        /// <summary>
        /// 计算指定 range 的总距离（用于 ByDistance UV 模式）
        /// </summary>
        private float CalculateRangeTotalDistance(in LineSegmentRange range)
        {
            float total = 0f;
            int segCount = range.vertexCount - 1;
            for (int i = 0; i < segCount; i++)
            {
                int idx = range.startVertexIndex + i;
                total += Vector2.Distance(m_VertexData[idx].position,
                    m_VertexData[idx + 1].position);
            }
            return total;
        }

        /// <summary>
        /// 构建指定范围内第 inSegmentIndex 段的输入参数
        /// </summary>
        protected SegmentJobInput BuildSegmentInput(
            in LineSegmentRange range, int inSegmentIndex,
            int globalSegmentIndex, int totalSegments,
            float cachedTotalDist = -1f)
        {
            int startIdx = range.startVertexIndex + inSegmentIndex;
            var start = m_VertexData[startIdx];
            var end = m_VertexData[startIdx + 1];

            var input = new SegmentJobInput
            {
                start = start,
                end = end,
                segmentIndex = globalSegmentIndex,
                totalSegmentCount = totalSegments,
                uvMode = range.uvMode,
                hasPrev = inSegmentIndex > 0 ? (byte)1 : (byte)0,
                hasNext = (inSegmentIndex < range.vertexCount - 2) ? (byte)1 : (byte)0
            };

            if (input.hasPrev == 1)
                input.prevEnd = m_VertexData[startIdx - 1];
            if (input.hasNext == 1)
                input.nextStart = m_VertexData[startIdx + 2];

            // 计算 startLineUV / endLineUV
            int segCount = GetSegmentCountForRange(range);
            if (range.uvMode == UVMode.BySegment)
            {
                float t0 = (float)inSegmentIndex / segCount;
                float t1 = (float)(inSegmentIndex + 1) / segCount;
                input.startLineUV = t0;
                input.endLineUV = t1;
            }
            else // ByDistance
            {
                float totalDist = cachedTotalDist >= 0f
                    ? cachedTotalDist
                    : CalculateRangeTotalDistance(range);

                if (totalDist < float.Epsilon)
                {
                    input.startLineUV = (float)inSegmentIndex / segCount;
                    input.endLineUV = (float)(inSegmentIndex + 1) / segCount;
                }
                else
                {
                    float distSoFar = 0f;
                    for (int i = 0; i < inSegmentIndex; i++)
                    {
                        int idx = range.startVertexIndex + i;
                        distSoFar += Vector2.Distance(m_VertexData[idx].position,
                            m_VertexData[idx + 1].position);
                    }
                    float segDist = Vector2.Distance(start.position, end.position);
                    input.startLineUV = distSoFar / totalDist;
                    input.endLineUV = (distSoFar + segDist) / totalDist;
                }
            }

            return input;
        }

        #endregion

        #region OnPopulateMesh

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (m_MeshVertexCache.Count == 0 || m_MeshIndexCache.Count == 0)
                return;

            // 将缓存的顶点和索引写入 VertexHelper
            vh.AddUIVertexStream(m_MeshVertexCache, m_MeshIndexCache);
        }

        #endregion

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            RebuildAll();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion
    }
}
