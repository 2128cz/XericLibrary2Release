using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using XericLibrary.Runtime.MacroLibrary;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// UILineRenderer 的工具类，提供静态方法用于线条渲染相关的操作
    /// </summary>
    internal static class XericRendererUtils
    {
        #region 数学计算

        /// <summary>
        /// 计算线路的总长度
        /// </summary>
        public static float CalculateTotalLength(this Vector2[] points, bool cycleLoop = false)
        {
            if (points.Length < 2)
                return 0f;

            var totalLength = 0f;
            for (int i = 0; i < points.Length - 1; i++)
                totalLength += Vector2.Distance(points[i], points[i + 1]);

            if (cycleLoop)
                totalLength += Vector2.Distance(points[0], points[^1]);

            return totalLength;
        }

        /// <summary>
        /// 计算一个点指向另一个点的角度（以度为单位）
        /// </summary>
        public static float RotatePointTowards(Vector2 vertex, Vector2 target)
        {
            return (Mathf.Atan2(target.y - vertex.y, target.x - vertex.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 如果需要居中显示，则偏移到组件中心
        /// </summary>
        public static Vector2 SwitchCenterOffset(bool center, Vector2 rectSize) =>
            center ? Vector2.zero : -(rectSize / 2);

        private static bool GetNormalizedVector(Vector2 startPoint, Vector2 endPoint,
            out Vector2 vector, out float distance, out Vector2 normal, out Vector2 subTangent,
            bool reverseVector = false)
        {
            vector = reverseVector ? startPoint - endPoint : endPoint - startPoint;
            distance = vector.magnitude;
            normal = Vector2.zero;
            subTangent = Vector2.zero;

            if (distance <= float.Epsilon)
                return false;

            normal = vector / distance;
            subTangent = Vector2.Perpendicular(normal);
            return true;
        }

        #endregion

        #region uv计算

        private static void GetCurrentLengthUVY(float startLength, float currentLength, float totalLength,
            out float startUVY, out float endUVY)
        {
            startUVY = totalLength > 0 ? startLength / totalLength : 0f;
            endUVY = totalLength > 0 ? (startLength + currentLength) / totalLength : 0f;
        }

        #endregion

        #region 简易线路绘制 DSL5

        public static List<UIVertex> GetSingleLineSegmentVerts_DSL5(List<UIVertex> vertices, Vector2 startPoint,
            Vector2 endPoint,
            float thickness, Color color, Vector2 normal, Vector2 subTangent, float startUVY, float endUVY,
            Vector3 offset)
        {
            if (vertices == null)
                throw new Exception("没有指定线段片段顶点容器列表");
            vertices.Clear();
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            var width = thickness / 2;

            vertex.position = (startPoint + subTangent * width);
            vertex.position += offset;
            vertex.uv0 = new Vector2(0f, startUVY);
            vertices.Add(vertex);

            vertex.position = (startPoint + subTangent * -width);
            vertex.position += offset;
            vertex.uv0 = new Vector2(1f, startUVY);
            vertices.Add(vertex);

            vertex.position = (endPoint + subTangent * width);
            vertex.position += offset;
            vertex.uv0 = new Vector2(0f, endUVY);
            vertices.Add(vertex);

            vertex.position = (endPoint + subTangent * -width);
            vertex.position += offset;
            vertex.uv0 = new Vector2(1f, endUVY);
            vertices.Add(vertex);

            vertex.position = endPoint;
            vertex.position += offset;
            vertex.uv0 = new Vector2(0.5f, endUVY);
            vertices.Add(vertex);
            return vertices;
        }

        public static void AddSingleLineSegmentVertsAndTriangle_DSL5(this VertexHelper vh, int segmentPartIndex,
            IEnumerable<UIVertex> verts)
        {
            foreach (var vert in verts)
                vh.AddVert(vert);
            int index = segmentPartIndex * 5;

            vh.AddTriangle(index, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index);

            if (segmentPartIndex != 0)
            {
                vh.AddTriangle(index, index - 1, index - 3);
                vh.AddTriangle(index + 1, index - 1, index - 2);
            }
        }

        public static void PopulateLineByPointsArray_DSL5(this VertexHelper vh, Vector2[] points,
            float thickness, bool cycleLoop, Color lineColor, Vector3 offset,
            bool enableArrow,
            Color arrowColor, Vector2 arrowSize, float pointProgress, float arrowPointProgress,
            bool absProgressPoint = true, bool reverseDir = false)
        {
            if (points.Length < 2)
                return;

            var totalLength = points.CalculateTotalLength(cycleLoop);
            var currentLength = 0f;
            var lineVertices = ListPool<UIVertex>.Get();
            var arrowVertices = ListPool<UIVertex>.Get();
            var arrowindices = ListPool<int>.Get();
            for (int i = 0; i < points.Length - (cycleLoop ? 0 : 1); i++)
            {
                var thisPoint = points[i];
                var nextPoint = points[(i + 1) % points.Length];

                if (!GetNormalizedVector(thisPoint, nextPoint,
                        out var vector, out var distance, out var normal, out var subTangent))
                    continue;
                GetCurrentLengthUVY(currentLength, distance, totalLength, out var startUvy, out var endUvy);

                vh.AddSingleLineSegmentVertsAndTriangle_DSL5(i,
                    GetSingleLineSegmentVerts_DSL5(lineVertices, thisPoint, nextPoint, thickness, lineColor, normal,
                        subTangent, startUvy, endUvy, offset));

                if (enableArrow)
                {
                    PopulateTriangleArrowByPointArray(arrowVertices, thisPoint, nextPoint, arrowColor, arrowSize.y,
                        arrowSize.x, pointProgress, arrowPointProgress, absProgressPoint, reverseDir);
                    arrowindices.AddFaceByConsecutiveVertices(arrowVertices, i * 3, 3, i * 3);
                }
                currentLength += distance;
            }

            if (enableArrow)
            {
                vh.AddUIVertexStream(arrowVertices, arrowindices);
            }
            ListPool<UIVertex>.Release(lineVertices);
            ListPool<UIVertex>.Release(arrowVertices);
            ListPool<int>.Release(arrowindices);
        }

        #endregion

        #region 圆弧过度的线路绘制 CFAL

        public static void AddSingleLineSegmentVertsAndTriangle_CFAL(this VertexHelper vh, int segmentPartIndex,
            IEnumerable<UIVertex> verts)
        {
            foreach (var vert in verts)
                vh.AddVert(vert);
            int index = segmentPartIndex * 5;

            vh.AddTriangle(index, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index);

            if (segmentPartIndex != 0)
            {
                vh.AddTriangle(index, index - 1, index - 3);
                vh.AddTriangle(index + 1, index - 1, index - 2);
            }
        }

        public static List<UIVertex> GetSingleLineSegmentVerts_CFAL(List<UIVertex> vertices, Vector2 startPoint,
            Vector2 endPoint, Vector2 nextPoint, Vector2 prevPoint,
            float thickness, Color color, float startLength, out float distance, float totalLength, Vector3 offset,
            int chamferSegments = 0, float innerRadius = 0f)
        {
            if (vertices == null)
                throw new Exception("没有指定线段片段顶点容器列表");
            vertices.Clear();

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            if (!GetNormalizedVector(startPoint, endPoint,
                    out var vector, out distance, out var normal, out var subTangent))
                return vertices;

            var width = thickness / 2;

            GetCurrentLengthUVY(startLength, distance, totalLength, out var startUVY, out var endUVY);

            float startAngle = 0f;
            float endAngle = 0f;

            if (prevPoint != startPoint)
            {
                var prevVector = startPoint - prevPoint;
                var prevNormal = prevVector.normalized;
                startAngle = Vector2.Angle(normal, -prevNormal);
            }

            if (nextPoint != endPoint)
            {
                var nextVector = nextPoint - endPoint;
                var nextNormal = nextVector.normalized;
                endAngle = Vector2.Angle(-normal, nextNormal);
            }

            float startOffset = 0f;
            float endOffset = 0f;

            if (Mathf.Abs(startAngle) > 0.1f)
            {
                if (innerRadius > 0f)
                {
                    startOffset = (width + innerRadius) / Mathf.Sin(startAngle * Mathf.Deg2Rad / 2) - innerRadius;
                }
                else
                {
                    startOffset = width / Mathf.Sin(startAngle * Mathf.Deg2Rad / 2);
                }
            }

            if (Mathf.Abs(endAngle) > 0.1f)
            {
                if (innerRadius > 0f)
                {
                    endOffset = (width + innerRadius) / Mathf.Sin(endAngle * Mathf.Deg2Rad / 2) - innerRadius;
                }
                else
                {
                    endOffset = width / Mathf.Sin(endAngle * Mathf.Deg2Rad / 2);
                }
            }

            vertex.position = (startPoint + subTangent * width + normal * startOffset);
            vertex.position += offset;
            vertex.uv0 = new Vector2(0f, startUVY);
            vertices.Add(vertex);

            vertex.position = (startPoint + subTangent * -width + normal * startOffset);
            vertex.position += offset;
            vertex.uv0 = new Vector2(1f, startUVY);
            vertices.Add(vertex);

            vertex.position = (endPoint + subTangent * width + normal * -endOffset);
            vertex.position += offset;
            vertex.uv0 = new Vector2(0f, endUVY);
            vertices.Add(vertex);

            vertex.position = (endPoint + subTangent * -width + normal * -endOffset);
            vertex.position += offset;
            vertex.uv0 = new Vector2(1f, endUVY);
            vertices.Add(vertex);

            vertex.position = (endPoint + normal * -endOffset);
            vertex.position += offset;
            vertex.uv0 = new Vector2(0.5f, endUVY);
            vertices.Add(vertex);

            return vertices;
        }

        public static void PopulateLineByPointsArray_CFAL(this VertexHelper vh, Vector2[] points, float thickness,
            bool cycleLoop, Color color, Vector3 offset,
            int chamferSegments = 0, float innerRadius = 0f)
        {
            if (points.Length < 2)
                return;

            var totalLength = points.CalculateTotalLength(cycleLoop);
            var currentLength = 0f;
            var vertices = ListPool<UIVertex>.Get();

            for (int i = 0; i < points.Length - (cycleLoop ? 0 : 1); i++)
            {
                var startPoint = points[i];
                var endPoint = points[(i + 1) % points.Length];
                var prevPoint = i > 0 ? points[i - 1] : cycleLoop ? points[^1] : startPoint;
                var nextPoint = points[(i + 2) % points.Length];

                vh.AddSingleLineSegmentVertsAndTriangle_CFAL(i,
                    GetSingleLineSegmentVerts_CFAL(vertices, startPoint, endPoint, nextPoint, prevPoint,
                        thickness, color, currentLength, out var distance, totalLength, offset,
                        chamferSegments, innerRadius));
                currentLength += distance;
            }

            ListPool<UIVertex>.Release(vertices);
        }

        public static void CreateLineSegment_CFAL(this VertexHelper vh, Vector2 startPoint, Vector2 endPoint,
            float angle0, float angle3, float thickness, bool center, Vector2 rectSize, Color color, float startLength,
            float totalLength)
        {
            Vector2 offset = center ? (rectSize / 2) : Vector2.zero;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            if (!GetNormalizedVector(startPoint, endPoint,
                    out var vector, out var distance, out var normal, out var subTangent))
                return;

            var width = thickness / 2;
            var pointOffset1 = Mathf.Abs(angle0) <= 0.1f
                ? 0
                : width / Mathf.Sin(angle0 * Mathf.Deg2Rad) + (angle0 > 90 ? width : 0);
            var pointOffset2 = Mathf.Abs(angle3) <= 0.1f
                ? 0
                : width / Mathf.Sin(angle3 * Mathf.Deg2Rad) + (angle3 > 90 ? width : 0);
            float startUVY = totalLength > 0 ? startLength / totalLength : 0f;
            float endUVY = totalLength > 0 ? (startLength + distance) / totalLength : 0f;

            vertex.position = (Vector3)(startPoint + subTangent * width + normal * pointOffset1) - (Vector3)offset;
            vertex.uv0 = new Vector2(0f, startUVY);
            vh.AddVert(vertex);

            vertex.position = (Vector3)(startPoint + subTangent * -width + normal * pointOffset1) - (Vector3)offset;
            vertex.uv0 = new Vector2(1f, startUVY);
            vh.AddVert(vertex);

            vertex.position = (Vector3)(endPoint + subTangent * width + normal * -pointOffset2) - (Vector3)offset;
            vertex.uv0 = new Vector2(0f, endUVY);
            vh.AddVert(vertex);

            vertex.position = (Vector3)(endPoint + subTangent * -width + normal * -pointOffset2) - (Vector3)offset;
            vertex.uv0 = new Vector2(1f, endUVY);
            vh.AddVert(vertex);

            vertex.position = (Vector3)(endPoint + normal * -pointOffset2) - (Vector3)offset;
            vertex.uv0 = new Vector2(0.5f, endUVY);
            vh.AddVert(vertex);
        }

        #endregion

        #region 箭头绘制工具

        public static List<UIVertex> PopulateTriangleArrowByPointArray(List<UIVertex> vertices, Vector2 start,
            Vector2 end, Color color, float sizeL, float sizeW, float pointProgress, float arrowPointProgress,
            bool absProgressPoint = true, bool reverseDir = false)
        {
            GetNormalizedVector(start, end, out var vector, out var length, out var normal, out var tangent,
                reverseDir);

            var progressPosition = absProgressPoint
                ? pointProgress * normal
                : pointProgress * vector;
            progressPosition += reverseDir ? end : start;
            progressPosition += arrowPointProgress * sizeL * normal;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = progressPosition;
            vertices.Add(vertex);

            vertex.position = progressPosition + -sizeL * normal + sizeW * 0.5f * tangent;
            vertices.Add(vertex);
            vertex.position += (Vector3)(-sizeW * tangent);
            vertices.Add(vertex);

            return vertices;
        }

        #endregion

        #region 顶点工具集

        /// <summary>
        /// 获取点在矩形中的UV坐标
        /// </summary>
        public static void GetUVAtRect(this Vector2 pos, Rect rect, out float u, out float v)
        {
            u = (pos.x - rect.xMin) / rect.width;
            v = (pos.y - rect.yMin) / rect.height;
        }

        public static void Get(this IList<UIVertex> vertexList, Vector4 uv2)
        {
        }

        #endregion

        #region 顶点列表工具

        private static readonly Color32 s_DefaultColor =
            new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        private static readonly Vector4 s_DefaultTangent = new Vector4(1f, 0.0f, 0.0f, -1f);

        /// <summary>
        /// 添加一个顶点信息
        /// </summary>
        public static void AddVert(this ICollection<UIVertex> vertexList, Vector3 position, Vector4 uv, Color32 color)
        {
            vertexList.Add(new UIVertex()
            {
                position = position,
                normal = Vector3.back,
                tangent = s_DefaultTangent,
                color = color,
                uv0 = uv,
                uv1 = uv,
                uv2 = uv,
                uv3 = uv
            });
        }

        /// <summary>
        /// 添加一个三角面索引
        /// </summary>
        public static void AddTriangle(this ICollection<int> indices, int index0, int index1, int index2)
        {
            indices.Add(index0);
            indices.Add(index1);
            indices.Add(index2);
        }

        /// <summary>
        /// 向顶点管理器添加顶点列表中的顶点构成的面
        /// </summary>
        public static void AddFaceByConsecutiveVertices(this ICollection<int> indices, IList<UIVertex> vertexList,
            int startVertexIndex, int faceVertexRange, int centerVertexIndex)
        {
            if (faceVertexRange < 3)
                throw new IndexOutOfRangeException("无法创建小于3个点的面");
            var length = startVertexIndex + faceVertexRange - 1;
            for (int i = startVertexIndex + 1; i < length; i++)
            {
                if (i < 0 || i > vertexList.Count)
                    throw new IndexOutOfRangeException(
                        $"在向顶点集添加顶点时，从{startVertexIndex}开始的{faceVertexRange}个顶点(以及{centerVertexIndex})可能超出{vertexList.Count}的范围(预定范围{length})，导致构建超出预期");
                var next = startVertexIndex + 1;

                if (next >= vertexList.Count)
                    return;
                indices.AddTriangle(centerVertexIndex, startVertexIndex, startVertexIndex + 1);
            }
        }

        #endregion
    }
}
