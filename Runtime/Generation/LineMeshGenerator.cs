using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_BURST
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
#endif

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 线段→模型顶点生成器
    /// 核心算法：每段线生成 5 个模型顶点 + 3 个三角形
    /// </summary>
    public static class LineMeshGenerator
    {
        /// <summary>
        /// 每段线的模型顶点数
        /// </summary>
        public const int VerticesPerSegment = 5;

        /// <summary>
        /// 每段线的三角形索引数（3 个三角形 × 3 索引）
        /// </summary>
        public const int IndicesPerSegment = 9;

        /// <summary>
        /// 计算单段线段对应的 5 个模型顶点（串行 &amp; Job 共用）
        /// </summary>
        /// <param name="input">线段生成参数</param>
        /// <param name="outputVertices">输出顶点数组，须预留 VerticesPerSegment 个位置</param>
        /// <param name="outputStart">写入 outputVertices 的起始偏移</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GenerateSingleSegment(in SegmentJobInput input,
            UIVertex[] outputVertices, int outputStart = 0)
        {
            Vector2 startPos = input.start.position;
            Vector2 endPos = input.end.position;
            Vector2 dir = endPos - startPos;
            float length = dir.magnitude;
            if (length < float.Epsilon)
            {
                // 退化为零长度线段，生成空顶点
                for (int i = 0; i < VerticesPerSegment; i++)
                {
                    outputVertices[outputStart + i] = new UIVertex
                    {
                        position = startPos,
                        color = Color.clear,
                        uv0 = Vector2.zero,
                        uv1 = Vector2.zero,
                        uv2 = Vector4.zero,
                        uv3 = Vector4.zero
                    };
                }
                return;
            }

            Vector2 normal = dir / length;
            Vector2 subTangent = Vector2.Perpendicular(normal);

            // 起点和终点的实际宽度（两端可不同）
            float halfW0 = input.start.thickness * 0.5f;
            float halfW1 = input.end.thickness * 0.5f;

            // 5 顶点布局
            // V0 = start + normal * -halfW    (左下)
            // V1 = start + normal + halfW     (左上)
            // V2 = end   + normal * -halfW    (右下)
            // V3 = end   + normal + halfW     (右上)
            // V4 = end                        (中心 → 转角连接)
            Vector3 v0 = startPos - subTangent * halfW0;
            Vector3 v1 = startPos + subTangent * halfW0;
            Vector3 v2 = endPos - subTangent * halfW1;
            Vector3 v3 = endPos + subTangent * halfW1;
            Vector3 v4 = endPos;

            Color32 c0 = Color32.Lerp(input.start.color, input.end.color, 0f);
            Color32 c1 = c0;
            Color32 c2 = Color32.Lerp(input.start.color, input.end.color, 1f);
            Color32 c3 = c2;
            Color32 c4 = c2;

            float uvSX = 0f;
            float uvEX = 1f;
            float uvSY = input.startLineUV;
            float uvEY = input.endLineUV;

            UIVertex vert = new UIVertex
            {
                normal = Vector3.back,
                tangent = new Vector4(1f, 0f, 0f, -1f)
            };

            Vector4 userDataStart = input.start.userData;
            Vector4 userDataEnd = input.end.userData;

            // V0
            vert.position = v0;
            vert.color = c0;
            vert.uv0 = new Vector2(uvSX, uvSY);
            vert.uv1 = new Vector2(0f, uvSY);     // 整线UV (u=0 表示线的起点侧)
            vert.uv2 = userDataStart;
            vert.uv3 = Vector4.zero;
            outputVertices[outputStart + 0] = vert;

            // V1
            vert.position = v1;
            vert.color = c1;
            vert.uv0 = new Vector2(uvEX, uvSY);
            vert.uv1 = new Vector2(1f, uvSY);     // 整线UV (u=1 表示线的终点侧)
            vert.uv2 = userDataStart;
            vert.uv3 = Vector4.zero;
            outputVertices[outputStart + 1] = vert;

            // V2
            vert.position = v2;
            vert.color = c2;
            vert.uv0 = new Vector2(uvSX, uvEY);
            vert.uv1 = new Vector2(0f, uvEY);
            vert.uv2 = userDataEnd;
            vert.uv3 = Vector4.zero;
            outputVertices[outputStart + 2] = vert;

            // V3
            vert.position = v3;
            vert.color = c3;
            vert.uv0 = new Vector2(uvEX, uvEY);
            vert.uv1 = new Vector2(1f, uvEY);
            vert.uv2 = userDataEnd;
            vert.uv3 = Vector4.zero;
            outputVertices[outputStart + 3] = vert;

            // V4
            vert.position = v4;
            vert.color = c4;
            vert.uv0 = new Vector2(0.5f, uvEY);
            vert.uv1 = new Vector2(0.5f, uvEY);
            vert.uv2 = userDataEnd;
            vert.uv3 = Vector4.zero;
            outputVertices[outputStart + 4] = vert;
        }

        /// <summary>
        /// 生成单段线段的三角形索引（写入 indexList）
        /// </summary>
        /// <param name="segmentIndex">子段全局索引</param>
        /// <param name="hasPrevInLine">上一子段是否属于同一条线（控制转角连接三角形）</param>
        /// <param name="indexList">输出的三角形索引列表</param>
        public static void GenerateSegmentIndices(int segmentIndex, bool hasPrevInLine, List<int> indexList)
        {
            int baseVert = segmentIndex * VerticesPerSegment;

            // 三角形 1: V0-V1-V3 (矩形上半)
            indexList.Add(baseVert + 0);
            indexList.Add(baseVert + 1);
            indexList.Add(baseVert + 3);

            // 三角形 2: V3-V2-V0 (矩形下半)
            indexList.Add(baseVert + 3);
            indexList.Add(baseVert + 2);
            indexList.Add(baseVert + 0);

            // 三角形 3: 转角连接三角形
            // 仅在「同一折线上的相邻子段」之间绘制
            // 连接上一段的 V4（上一段终点中心）到当前段的 V0, V1
            if (hasPrevInLine)
            {
                int prevV4 = (segmentIndex - 1) * VerticesPerSegment + 4;
                indexList.Add(prevV4);
                indexList.Add(baseVert + 0);
                indexList.Add(baseVert + 1);
            }
        }

        #region Jobs 模式

#if ENABLE_BURST
        /// <summary>
        /// Burst 编译的 IJobParallelFor：并行生成各段线的模型顶点
        /// </summary>
        [BurstCompile]
        public struct GenerateVerticesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<SegmentJobInput> Inputs;
            [WriteOnly] public NativeArray<UIVertex> OutputVertices;

            public void Execute(int index)
            {
                int start = index * VerticesPerSegment;
                var input = Inputs[index];
                Vector2 startPos = input.start.position;
                Vector2 endPos = input.end.position;
                Vector2 dir = endPos - startPos;
                float length = dir.magnitude;
                if (length < float.Epsilon)
                {
                    for (int i = 0; i < VerticesPerSegment; i++)
                        OutputVertices[start + i] = new UIVertex();
                    return;
                }

                Vector2 normal = dir / length;
                Vector2 subTangent = new Vector2(-normal.y, normal.x);

                float halfW0 = input.start.thickness * 0.5f;
                float halfW1 = input.end.thickness * 0.5f;

                Vector3 v0 = startPos - subTangent * halfW0;
                Vector3 v1 = startPos + subTangent * halfW0;
                Vector3 v2 = endPos - subTangent * halfW1;
                Vector3 v3 = endPos + subTangent * halfW1;
                Vector3 v4 = endPos;

                Color32 c0 = Color32.Lerp(input.start.color, input.end.color, 0f);
                Color32 c2 = Color32.Lerp(input.start.color, input.end.color, 1f);

                float uvSY = input.startLineUV;
                float uvEY = input.endLineUV;
                Vector4 userDataStart = input.start.userData;
                Vector4 userDataEnd = input.end.userData;

                UIVertex vert = new UIVertex
                {
                    normal = Vector3.back,
                    tangent = new Vector4(1f, 0f, 0f, -1f)
                };

                vert.position = v0; vert.color = c0;
                vert.uv0 = new Vector2(0f, uvSY); vert.uv1 = new Vector2(0f, uvSY); vert.uv2 = userDataStart;
                OutputVertices[start + 0] = vert;

                vert.position = v1; vert.color = c0;
                vert.uv0 = new Vector2(1f, uvSY); vert.uv1 = new Vector2(1f, uvSY); vert.uv2 = userDataStart;
                OutputVertices[start + 1] = vert;

                vert.position = v2; vert.color = c2;
                vert.uv0 = new Vector2(0f, uvEY); vert.uv1 = new Vector2(0f, uvEY); vert.uv2 = userDataEnd;
                OutputVertices[start + 2] = vert;

                vert.position = v3; vert.color = c2;
                vert.uv0 = new Vector2(1f, uvEY); vert.uv1 = new Vector2(1f, uvEY); vert.uv2 = userDataEnd;
                OutputVertices[start + 3] = vert;

                vert.position = v4; vert.color = c2;
                vert.uv0 = new Vector2(0.5f, uvEY); vert.uv1 = new Vector2(0.5f, uvEY); vert.uv2 = userDataEnd;
                OutputVertices[start + 4] = vert;
            }
        }

        /// <summary>
        /// 使用 Jobs 并行生成线段顶点
        /// </summary>
        public static JobHandle GenerateSegmentsJobs(
            NativeArray<SegmentJobInput> inputs,
            NativeArray<UIVertex> outputVertices,
            JobHandle dependsOn = default)
        {
            var job = new GenerateVerticesJob
            {
                Inputs = inputs,
                OutputVertices = outputVertices
            };
            return job.Schedule(inputs.Length, 1, dependsOn);
        }
#endif

        #endregion
    }

}
