using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.MacroLibrary;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 曲线网格生成器
    /// 将贝塞尔曲线细分为线段带状三角片 + 生成箭头装饰
    ///
    /// 核心算法：
    ///   1. 在 t∈[0,1] 上均匀采样 N+1 个点
    ///   2. 每个采样点计算位置、切线、宽度、颜色
    ///   3. 切线转 90° 得切法线，沿切法线位移 halfWidth 得左右边顶点
    ///   4. 顺序连接左右边顶点形成 ribbon 带状：
    ///      左[i]──右[i]──左[i+1]──右[i+1]──...
    ///      每个四边形拆两个三角形
    /// </summary>
    public static class CurveMeshGenerator
    {
        /// <summary>圆圈箭头多边形边数</summary>
        private const int CircleArrowSides = 8;

        /// <summary>计算切线时的 epsilon 采样偏移</summary>
        private const float TangentEpsilon = 0.001f;

        /// <summary>
        /// 生成单条曲线的网格数据
        /// </summary>
        public static void GenerateCurve(
            in CurveCacheEntry entry,
            List<Vector2> controlPointsPool,
            List<float> widthsPool,
            List<ArrowHeadData> arrowsPool,
            List<UIVertex> vertexList,
            List<int> indexList)
        {
            int segCount = entry.tessellationSegments;
            if (segCount < 1) segCount = 4;
            int sampleCount = segCount + 1; // N+1 个采样点

            int cpStart = entry.controlPointStartIndex;
            int cpCount = entry.controlPointCount;
            int wStart = entry.widthStartIndex;

            var ctrlPts = new Vector2[cpCount];
            var wPts = new float[cpCount];
            for (int i = 0; i < cpCount; i++)
            {
                ctrlPts[i] = controlPointsPool[cpStart + i];
                wPts[i] = widthsPool[wStart + i];
            }

            int baseVertex = vertexList.Count;
            UIVertex vert = new UIVertex
            {
                normal = Vector3.back,
                tangent = new Vector4(1f, 0f, 0f, -1f),
                uv1 = new Vector2(1f, 0f),
            };

            // 1. 采样 + 生成左右边顶点
            for (int s = 0; s < sampleCount; s++)
            {
                float t = (float)s / segCount;

                Vector2 pos = MacroCurve.BezierCurve(ctrlPts, t);                // 曲线上点
                float width = MacroCurve.BezierCurve(wPts, t);                   // 当前宽度
                Vector2 tangent = ComputeTangent(ctrlPts, t);         // 切线方向
                Vector2 normal = new Vector2(-tangent.y, tangent.x);  // 切法线（左转90°）

                MacroCurve.BezierCurve1(entry.startColor, entry.endColor, t, out Color col);
                Color32 color = col;

                float halfW = width * 0.5f;
                float uv = (float)s / segCount;

                // 左边顶点
                vert.position = pos - normal * halfW;
                vert.color = color;
                vert.uv0 = new Vector2(0f, uv);
                vertexList.Add(vert);

                // 右边顶点
                vert.position = pos + normal * halfW;
                vert.color = color;
                vert.uv0 = new Vector2(1f, uv);
                vertexList.Add(vert);
            }

            // 2. 连接带状三角形
            // 左[i]=v[i*2], 右[i]=v[i*2+1]
            // 四边形 i→i+1: (左i, 右i, 左i+1) + (右i, 右i+1, 左i+1)
            for (int s = 0; s < segCount; s++)
            {
                int bv = baseVertex + s * 2;

                indexList.Add(bv + 0); // 左i
                indexList.Add(bv + 1); // 右i
                indexList.Add(bv + 2); // 左i+1

                indexList.Add(bv + 1); // 右i
                indexList.Add(bv + 3); // 右i+1
                indexList.Add(bv + 2); // 左i+1
            }

            // 3. 生成箭头
            if (entry.arrowCount > 0 && entry.arrowStartIndex >= 0)
            {
                for (int a = 0; a < entry.arrowCount; a++)
                {
                    var arrow = arrowsPool[entry.arrowStartIndex + a];
                    GenerateArrow(arrow, ctrlPts, vertexList, indexList);
                }
            }
        }

        /// <summary>
        /// 计算曲线上 t 处的切线方向（差分法）
        /// </summary>
        private static Vector2 ComputeTangent(Vector2[] ctrlPts, float t)
        {
            float t0 = Mathf.Max(0f, t - TangentEpsilon);
            float t1 = Mathf.Min(1f, t + TangentEpsilon);
            Vector2 tangent = MacroCurve.BezierCurve(ctrlPts, t1) - MacroCurve.BezierCurve(ctrlPts, t0);
            if (tangent.sqrMagnitude < float.Epsilon)
                tangent = Vector2.up;
            else
                tangent.Normalize();
            return tangent;
        }

        #region 箭头

        private static void GenerateArrow(
            ArrowHeadData arrow,
            Vector2[] controlPoints,
            List<UIVertex> vertexList,
            List<int> indexList)
        {
            float t = Mathf.Clamp01(arrow.progress);
            Vector2 pos = MacroCurve.BezierCurve(controlPoints, t);
            Vector2 tangent = ComputeTangent(controlPoints, t);

            if (arrow.reversed)
                tangent = -tangent;

            // 深度补偿：半长偏移（箭头中心在本地x=0，尖端在+width*0.5，尾部在-width*0.5）
            pos += tangent * arrow.width * 0.5f * arrow.depthCompensation;

            int baseVert = vertexList.Count;
            GenerateArrowShape(arrow.shape, pos, tangent, arrow.width, arrow.height, arrow.color,
                vertexList, indexList, baseVert, t);
        }

        private static void GenerateArrowShape(
            ArrowShape shape,
            Vector2 position,
            Vector2 tangent,
            float width,
            float height,
            Color32 color,
            List<UIVertex> vertexList,
            List<int> indexList,
            int baseVertex,
            float progressT)
        {
            float cos = tangent.x;
            float sin = tangent.y;

            Vector2 TransformPoint(float ux, float uy)
            {
                float x = ux * width;
                float y = uy * height;
                float rx = x * cos - y * sin;
                float ry = x * sin + y * cos;
                return position + new Vector2(rx, ry);
            }

            UIVertex vert = new UIVertex
            {
                color = color,
                normal = Vector3.back,
                tangent = new Vector4(1f, 0f, 0f, -1f),
                uv0 = new Vector2(0.5f, progressT),
                uv1 = new Vector2(0f, 1f)
            };

            switch (shape)
            {
                case ArrowShape.Triangle:
                {
                    vert.position = TransformPoint(0.5f, 0f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-0.5f, -0.3f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-0.5f, 0.3f);
                    vertexList.Add(vert);

                    indexList.Add(baseVertex + 0);
                    indexList.Add(baseVertex + 1);
                    indexList.Add(baseVertex + 2);
                    break;
                }

                case ArrowShape.HollowTriangle:
                {
                    float tailOff = -0.1f;

                    // 外三角
                    vert.position = TransformPoint(0.5f, 0f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-0.5f, -0.3f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-0.5f, 0.3f);
                    vertexList.Add(vert);

                    // 内三角（缩小 + 向尾部偏移）
                    vert.position = TransformPoint(0.5f * 0.5f + tailOff, 0f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-0.5f * 0.5f + tailOff, -0.3f * 0.5f * 0.6f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-0.5f * 0.5f + tailOff, 0.3f * 0.5f * 0.6f);
                    vertexList.Add(vert);

                    indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 4); indexList.Add(baseVertex + 5);
                    indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 5); indexList.Add(baseVertex + 2);
                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 4);
                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 4); indexList.Add(baseVertex + 3);
                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 3); indexList.Add(baseVertex + 5);
                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 5); indexList.Add(baseVertex + 2);
                    break;
                }

                case ArrowShape.Arrow:
                {
                    float headBaseU = 0.5f - 0.35f;  // 头部基座 x=0.15
                    float shaftEndU = -0.8f;         // 杆尾

                    vert.position = TransformPoint(0.5f, 0f);
                    vertexList.Add(vert);              // 0: 尖端
                    vert.position = TransformPoint(headBaseU, -0.25f);
                    vertexList.Add(vert);              // 1: 左翼
                    vert.position = TransformPoint(headBaseU, 0.25f);
                    vertexList.Add(vert);              // 2: 右翼
                    vert.position = TransformPoint(headBaseU, -0.06f);
                    vertexList.Add(vert);              // 3: 杆起点下
                    vert.position = TransformPoint(headBaseU, 0.06f);
                    vertexList.Add(vert);              // 4: 杆起点上
                    vert.position = TransformPoint(shaftEndU, -0.06f);
                    vertexList.Add(vert);              // 5: 杆尾下
                    vert.position = TransformPoint(shaftEndU, 0.06f);
                    vertexList.Add(vert);              // 6: 杆尾上

                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 2);
                    indexList.Add(baseVertex + 2); indexList.Add(baseVertex + 4); indexList.Add(baseVertex + 6);
                    indexList.Add(baseVertex + 2); indexList.Add(baseVertex + 6); indexList.Add(baseVertex + 1);
                    indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 6); indexList.Add(baseVertex + 5);
                    indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 5); indexList.Add(baseVertex + 3);
                    break;
                }

                case ArrowShape.Circle:
                {
                    float r = 0.4f;
                    vert.position = TransformPoint(0f, 0f);
                    vertexList.Add(vert);
                    for (int i = 0; i < CircleArrowSides; i++)
                    {
                        float angle = i * Mathf.PI * 2f / CircleArrowSides;
                        vert.position = TransformPoint(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                        vertexList.Add(vert);
                    }
                    for (int i = 0; i < CircleArrowSides; i++)
                    {
                        int next = (i + 1) % CircleArrowSides;
                        indexList.Add(baseVertex + 0);
                        indexList.Add(baseVertex + 1 + i);
                        indexList.Add(baseVertex + 1 + next);
                    }
                    break;
                }

                case ArrowShape.Square:
                {
                    float hs = 0.35f;
                    vert.position = TransformPoint(-hs, -hs);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(hs, -hs);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(hs, hs);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(-hs, hs);
                    vertexList.Add(vert);
                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 2);
                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 2); indexList.Add(baseVertex + 3);
                    break;
                }

                case ArrowShape.ReverseArrow:
                {
                    float rHeadBaseU = 0.5f - 0.35f;
                    float rShaftEndU = -0.8f;

                    vert.position = TransformPoint(0.5f, 0f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(rHeadBaseU, -0.25f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(rHeadBaseU, 0.25f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(rHeadBaseU, -0.06f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(rHeadBaseU, 0.06f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(rShaftEndU, -0.06f);
                    vertexList.Add(vert);
                    vert.position = TransformPoint(rShaftEndU, 0.06f);
                    vertexList.Add(vert);

                    indexList.Add(baseVertex + 0); indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 2);
                    indexList.Add(baseVertex + 2); indexList.Add(baseVertex + 4); indexList.Add(baseVertex + 6);
                    indexList.Add(baseVertex + 2); indexList.Add(baseVertex + 6); indexList.Add(baseVertex + 1);
                    indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 6); indexList.Add(baseVertex + 5);
                    indexList.Add(baseVertex + 1); indexList.Add(baseVertex + 5); indexList.Add(baseVertex + 3);
                    break;
                }
            }
        }

        #endregion
    }
}
