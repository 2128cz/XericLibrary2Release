using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.MacroLibrary;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 图元网格生成器
    /// 为多边形/矩形生成 triangle-fan 网格，支持倒角
    /// </summary>
    public static class PrimitiveMeshGenerator
    {
        /// <summary>
        /// 生成单个图元的网格顶点和索引
        /// </summary>
        public static void GeneratePrimitive(
            in PrimitiveCacheEntry entry,
            List<UIVertex> vertexList,
            List<int> indexList)
        {
            // 将归一化 chamferSize [0,1] 映射为实际像素倒角尺寸
            float minDim = Mathf.Min(entry.size.x, entry.size.y);
            float scaledChamfer = entry.@params.chamferSize * minDim * 0.5f;

            PrimitiveParamHelper helper = new PrimitiveParamHelper
            {
                center = entry.center,
                size = entry.size,
                sizeMode = entry.sizeMode,
                type = entry.type,
                sideCount = (int)entry.@params.sideCount,
                chamferSize = scaledChamfer,
                chamferSegments = entry.@params.chamferSegments,
                faceCount = 0
            };

            // 1. 计算外围顶点
            var corners = ComputeCorners(helper);
            if (corners.Count < 3) return;

            // 应用角度旋转
            if (Mathf.Abs(entry.angle) > 0.001f)
            {
                float rad = entry.angle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);
                for (int i = 0; i < corners.Count; i++)
                {
                    Vector2 rel = corners[i] - entry.center;
                    corners[i] = entry.center + new Vector2(
                        rel.x * cos - rel.y * sin,
                        rel.x * sin + rel.y * cos);
                }
            }

            int baseVertex = vertexList.Count;
            int typeIndex = (int)entry.type;

            // 颜色打包到法线/副法线
            //   normal:  (bg.r/255, bg.g/255, bg.b/255)
            //   tangent: (border.r/255, border.g/255, border.b/255, border.a/255)
            Color32 bg = entry.@params.bgColor;
            Vector3 normalCol = new Vector3(bg.r / 255f, bg.g / 255f, bg.b / 255f);
            Color32 bc = entry.@params.borderColor;
            Vector4 tangentCol = new Vector4(bc.r / 255f, bc.g / 255f, bc.b / 255f, bc.a / 255f);

            // 2. 添加中心点
            UIVertex centerVert = new UIVertex
            {
                position = (Vector3)entry.center,
                color = entry.@params.centerColor,
                normal = normalCol,
                tangent = tangentCol,
                uv0 = new Vector2(0.5f, 0.5f),
                uv1 = new Vector2(0f, 0f),
            };
            vertexList.Add(centerVert);

            // 3. 添加外围顶点 + 创建三角形扇
            for (int i = 0; i < corners.Count; i++)
            {
                Vector2 pos = corners[i];
                float dist = Vector2.Distance(pos, entry.center);
                float maxDist = Mathf.Max(entry.size.x, entry.size.y) * 0.5f;
                float normalizedDist = maxDist > 0.001f ? Mathf.Clamp01(dist / maxDist) : 0f;

                UIVertex vert = new UIVertex
                {
                    position = (Vector3)pos,
                    color = entry.@params.centerColor,
                    normal = normalCol,
                    tangent = tangentCol,
                    uv0 = new Vector2(
                        (pos.x - entry.center.x) / entry.size.x + 0.5f,
                        (pos.y - entry.center.y) / entry.size.y + 0.5f),
                    uv1 = new Vector2(normalizedDist, 0f),
                };
                vertexList.Add(vert);

                // 三角形扇: (中心, i, i+1)
                if (i > 0)
                {
                    indexList.Add(baseVertex);           // 中心
                    indexList.Add(baseVertex + i);       // 当前角
                    indexList.Add(baseVertex + i + 1);   // 下一个角
                }
            }

            // 闭合：最后一个角 → 第一个角
            if (corners.Count > 2)
            {
                indexList.Add(baseVertex);
                indexList.Add(baseVertex + corners.Count);
                indexList.Add(baseVertex + 1);
            }

            // 4. 生成物理边框 — 复制外围顶点并向外偏移一圈，条带拼接
            float borderThickness = entry.@params.borderThickness;
            if (borderThickness > 0.001f && corners.Count >= 3)
            {
                int n = corners.Count;
                int borderBase = vertexList.Count;

                // 计算每个顶点的向外偏移方向（从中心指向顶点的方向）
                var outDirs = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 dir = corners[i] - entry.center;
                    outDirs[i] = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.up;
                }

                // 添加内圈（原始位置）+ 外圈（偏移位置），isBorder=1
                for (int i = 0; i < n; i++)
                {
                    Vector2 pos = corners[i];
                    Vector2 outPos = pos + outDirs[i] * borderThickness;

                    // 内圈顶点 (borderProgress=0)
                    UIVertex inner = new UIVertex
                    {
                        position = (Vector3)pos,
                        color = entry.@params.borderColor,
                        normal = normalCol,
                        tangent = tangentCol,
                        uv0 = new Vector2(
                            (pos.x - entry.center.x) / entry.size.x + 0.5f,
                            (pos.y - entry.center.y) / entry.size.y + 0.5f),
                        uv1 = new Vector2(0f, 1f),
                    };
                    vertexList.Add(inner);

                    // 外圈顶点 (borderProgress=1)
                    UIVertex outer = new UIVertex
                    {
                        position = (Vector3)outPos,
                        color = entry.@params.borderColor,
                        normal = normalCol,
                        tangent = tangentCol,
                        uv0 = new Vector2(
                            (outPos.x - entry.center.x) / entry.size.x + 0.5f,
                            (outPos.y - entry.center.y) / entry.size.y + 0.5f),
                        uv1 = new Vector2(1f, 1f),
                    };
                    vertexList.Add(outer);
                }

                // 连接条带三角形
                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;
                    int iI = borderBase + i * 2;       // 内圈 i
                    int oI = borderBase + i * 2 + 1;   // 外圈 i
                    int iN = borderBase + next * 2;     // 内圈 next
                    int oN = borderBase + next * 2 + 1; // 外圈 next

                    indexList.Add(iI);
                    indexList.Add(oI);
                    indexList.Add(iN);

                    indexList.Add(oI);
                    indexList.Add(oN);
                    indexList.Add(iN);
                }
            }
        }

        /// <summary>
        /// 计算图元的外围顶点列表
        /// </summary>
        private static List<Vector2> ComputeCorners(in PrimitiveParamHelper helper)
        {
            if (helper.type == PrimitiveType.Polygon)
                return ComputePolygonCorners(helper);
            else
                return ComputeRectangleCorners(helper);
        }

        /// <summary>
        /// 计算正多边形的外围顶点
        /// </summary>
        private static List<Vector2> ComputePolygonCorners(in PrimitiveParamHelper helper)
        {
            int sides = Mathf.Max(3, helper.sideCount);
            float angleStep = 360f / sides;
            float startAngle = 90f;

            // 计算半径
            float minSize = Mathf.Min(helper.size.x, helper.size.y);
            float radius = minSize * 0.5f;

            if (helper.sizeMode == SizeMode.CircumscribedCircle)
            {
                // 外切圆：半径延伸到各顶点
                // 外切圆半径 = minSize/2
                // 实际半径需要让多边形的边与外切圆相切
                // 对于外切圆，多边形的顶点到中心的距离 = r / cos(π/N)
                float apothemAngle = Mathf.PI / sides;
                radius = radius / Mathf.Cos(apothemAngle);
            }
            // InscribedCircle 保持 radius = minSize/2

            // 计算基本多边形顶点
            var corners = new List<Vector2>(sides);
            for (int i = 0; i < sides; i++)
            {
                float angleDeg = startAngle + i * angleStep;
                float angle = angleDeg * Mathf.Deg2Rad;
                corners.Add(new Vector2(
                    helper.center.x + Mathf.Cos(angle) * radius,
                    helper.center.y + Mathf.Sin(angle) * radius));
            }

            // 应用 XY 缩放
            for (int i = 0; i < corners.Count; i++)
            {
                Vector2 dir = corners[i] - helper.center;
                corners[i] = helper.center + new Vector2(
                    dir.x * helper.size.x / minSize,
                    dir.y * helper.size.y / minSize);
            }

            // 处理倒角
            if (helper.chamferSize > 0.001f)
                corners = ApplyChamfer(corners, helper.chamferSize, helper.chamferSegments);

            return corners;
        }

        /// <summary>
        /// 计算矩形的四个角
        /// </summary>
        private static List<Vector2> ComputeRectangleCorners(in PrimitiveParamHelper helper)
        {
            float halfX, halfY;

            switch (helper.sizeMode)
            {
                case SizeMode.InscribedCircle:
                {
                    float minSize = Mathf.Min(helper.size.x, helper.size.y);
                    halfX = minSize * 0.5f;
                    halfY = minSize * 0.5f;
                    break;
                }
                case SizeMode.CircumscribedCircle:
                {
                    float minSize = Mathf.Min(helper.size.x, helper.size.y);
                    float radius = minSize * 0.5f;
                    halfX = radius;
                    halfY = radius;
                    break;
                }
                case SizeMode.InscribedEllipse:
                {
                    halfX = helper.size.x * 0.5f;
                    halfY = helper.size.y * 0.5f;
                    break;
                }
                case SizeMode.CircumscribedRatio:
                {
                    float minSize = Mathf.Min(helper.size.x, helper.size.y);
                    float radius = minSize * 0.5f;
                    halfX = radius * (helper.size.x / minSize);
                    halfY = radius * (helper.size.y / minSize);
                    break;
                }
                default:
                    halfX = helper.size.x * 0.5f;
                    halfY = helper.size.y * 0.5f;
                    break;
            }

            var corners = new List<Vector2>(4)
            {
                helper.center + new Vector2(-halfX, -halfY),
                helper.center + new Vector2( halfX, -halfY),
                helper.center + new Vector2( halfX,  halfY),
                helper.center + new Vector2(-halfX,  halfY),
            };

            if (helper.chamferSize > 0.001f)
                corners = ApplyChamfer(corners, helper.chamferSize, helper.chamferSegments);

            return corners;
        }

        /// <summary>
        /// 对多边形角应用倒角（Bezier 曲线圆角）
        /// </summary>
        private static List<Vector2> ApplyChamfer(
            List<Vector2> corners, float chamferSize, int segments)
        {
            int n = corners.Count;
            // 计算每条边的长度，取最短边的一半作为倒角上限
            float minEdgeHalf = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                float edgeLen = Vector2.Distance(corners[i], corners[next]);
                minEdgeHalf = Mathf.Min(minEdgeHalf, edgeLen * 0.5f);
            }
            float clampedChamfer = Mathf.Min(chamferSize, minEdgeHalf);
            if (clampedChamfer < 0.001f) return corners;

            var result = new List<Vector2>();
            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                int curr = i;
                int next = (i + 1) % n;

                Vector2 pCurr = corners[curr];
                Vector2 pPrev = corners[prev];
                Vector2 pNext = corners[next];

                float edgeLenIn = Vector2.Distance(pPrev, pCurr);
                float edgeLenOut = Vector2.Distance(pCurr, pNext);
                float clampIn = Mathf.Min(clampedChamfer, edgeLenIn * 0.5f);
                float clampOut = Mathf.Min(clampedChamfer, edgeLenOut * 0.5f);

                // p0 在入边 (prev→curr) 上，距角 clampIn
                // p3 在出边 (curr→next) 上，距角 clampOut
                Vector2 dirFromCurrToPrev = (pPrev - pCurr).normalized;
                Vector2 dirFromCurrToNext = (pNext - pCurr).normalized;

                Vector2 p0 = pCurr + dirFromCurrToPrev * clampIn;
                Vector2 p3 = pCurr + dirFromCurrToNext * clampOut;

                // 三次 Bezier 控制点：向角的方向内收，使曲线从 p0 平滑过渡到 p3
                // 控制点位于 p0→pCorner 和 p3→pCorner 之间
                float c = 0.5522847498f; // 四分之一圆的 Bezier 近似常数
                float bezierR = Mathf.Min(clampIn, clampOut);
                Vector2 p1 = p0 + (pCurr - p0).normalized * bezierR * c;
                Vector2 p2 = p3 + (pCurr - p3).normalized * bezierR * c;

                // 添加 p0（每条边的起点）
                result.Add(p0);

                // 插值 Bezier 曲线上的中间点
                for (int j = 1; j < segments; j++)
                {
                    float t = (float)j / segments;
                    MacroCurve.BezierCurve3(p0, p1, p2, p3, t, out Vector2 pos);
                    result.Add(pos);
                }

                // 添加 p3（每条边的终点，也是下条边的起点）
                result.Add(p3);
            }

            return result;
        }

        /// <summary>
        /// 内部辅助结构（避免在 ComputeCorners 中传递过多参数）
        /// </summary>
        private struct PrimitiveParamHelper
        {
            public Vector2 center;
            public Vector2 size;
            public SizeMode sizeMode;
            public PrimitiveType type;
            public int sideCount;
            public float chamferSize;
            public int chamferSegments;
            public int faceCount; // 输出：实际生成的三角形数
        }
    }
}
