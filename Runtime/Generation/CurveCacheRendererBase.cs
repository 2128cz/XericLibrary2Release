using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 曲线缓存渲染器基类
    /// 管理 CurveCacheEntry 列表 + 扁平控制点/宽度/箭头缓存 + 脏标记增量重建
    /// LateUpdate 中只重建脏标记范围内的曲线
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class CurveCacheRendererBase : MaskableGraphic
    {
        #region 默认材质

        private static Material s_DefaultMaterial;
        private const string DefaultShaderPath = "XericLibrary/UIGraph/XericUICurve";

        public override Material defaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    var shader = Shader.Find(DefaultShaderPath);
                    if (shader != null)
                        s_DefaultMaterial = new Material(shader);
                    else
                        s_DefaultMaterial = base.defaultMaterial;
                }
                return s_DefaultMaterial;
            }
        }

        #endregion

        #region 数据缓存

        /// <summary>曲线条目列表</summary>
        protected List<CurveCacheEntry> m_Curves = new List<CurveCacheEntry>();

        /// <summary>所有曲线的控制点扁平存储</summary>
        protected List<Vector2> m_ControlPoints = new List<Vector2>();

        /// <summary>所有曲线的宽度扁平存储（与控制点一一对应）</summary>
        protected List<float> m_Widths = new List<float>();

        /// <summary>所有曲线的箭头扁平存储</summary>
        protected List<ArrowHeadData> m_Arrows = new List<ArrowHeadData>();

        /// <summary>模型顶点缓存</summary>
        protected List<UIVertex> m_MeshVertexCache = new List<UIVertex>();

        /// <summary>三角形索引缓存</summary>
        protected List<int> m_MeshIndexCache = new List<int>();

        /// <summary>脏标记范围起点（曲线索引），-1 表示无脏标记</summary>
        protected int m_DirtyStartIndex = -1;

        /// <summary>脏标记范围终点（曲线索引）</summary>
        protected int m_DirtyEndIndex = -1;

        #endregion

        #region API

        /// <summary>
        /// 添加一条曲线
        /// </summary>
        /// <param name="entry">曲线缓存条目（控制点计数和起始索引需预先设置）</param>
        /// <param name="controlPoints">控制点数组</param>
        /// <param name="widths">宽度数组（长度需与控制点一致）</param>
        /// <param name="arrows">箭头数组（可选）</param>
        /// <returns>曲线索引</returns>
        public int AddCurve(CurveCacheEntry entry, Vector2[] controlPoints, float[] widths, ArrowHeadData[] arrows = null)
        {
            int index = m_Curves.Count;

            // 填充起始索引
            entry.controlPointStartIndex = m_ControlPoints.Count;
            entry.widthStartIndex = m_Widths.Count;
            entry.arrowStartIndex = (arrows != null && arrows.Length > 0) ? m_Arrows.Count : -1;
            entry.arrowCount = arrows?.Length ?? 0;
            entry.controlPointCount = controlPoints.Length;

            m_Curves.Add(entry);
            m_ControlPoints.AddRange(controlPoints);
            m_Widths.AddRange(widths);
            if (arrows != null)
                m_Arrows.AddRange(arrows);

            MarkDirty(index);
            return index;
        }

        /// <summary>
        /// 移除指定曲线
        /// </summary>
        public void RemoveCurve(int index)
        {
            if (index < 0 || index >= m_Curves.Count) return;
            m_Curves.RemoveAt(index);
            RebuildAll();
        }

        /// <summary>
        /// 标记指定曲线为脏
        /// </summary>
        public void SetCurveDirty(int index)
        {
            MarkDirty(index);
        }

        /// <summary>
        /// 清空所有曲线
        /// </summary>
        public void ClearAll()
        {
            m_Curves.Clear();
            m_ControlPoints.Clear();
            m_Widths.Clear();
            m_Arrows.Clear();
            m_MeshVertexCache.Clear();
            m_MeshIndexCache.Clear();
            m_DirtyStartIndex = -1;
            m_DirtyEndIndex = -1;
            SetVerticesDirty();
        }

        #endregion

        #region 脏标记

        protected void MarkDirty(int index)
        {
            if (m_DirtyStartIndex < 0 || index < m_DirtyStartIndex)
                m_DirtyStartIndex = index;
            if (m_DirtyEndIndex < 0 || index > m_DirtyEndIndex)
                m_DirtyEndIndex = index;
        }

        /// <summary>
        /// 标记所有曲线为脏（下次 LateUpdate 全量重绘）
        /// 适用于：更新控制点/宽度后，不改变元素数量的重绘
        /// </summary>
        public void RebuildAll()
        {
            m_DirtyStartIndex = 0;
            m_DirtyEndIndex = m_Curves.Count - 1;
            if (m_DirtyEndIndex < 0)
                m_DirtyStartIndex = -1;
        }

        #endregion

        #region LateUpdate

        protected virtual void LateUpdate()
        {
            if (m_DirtyStartIndex < 0) return;

            int start = m_DirtyStartIndex;

            // 统计脏范围前的顶点/索引数（这些无需重建）
            int preVertexCount = 0;
            int preIndexCount = 0;
            for (int i = 0; i < start; i++)
            {
                preVertexCount += m_Curves[i].vertexCount;
                preIndexCount += m_Curves[i].triangleCount * 3;
            }

            // 从 start 开始重建所有曲线
            var tempVerts = new List<UIVertex>();
            var tempIndices = new List<int>();
            int vAccum = preVertexCount;
            int iAccum = preIndexCount;
            for (int i = start; i < m_Curves.Count; i++)
            {
                var entry = m_Curves[i];
                int vertBefore = tempVerts.Count;
                int idxBefore = tempIndices.Count;
                CurveMeshGenerator.GenerateCurve(
                    entry, m_ControlPoints, m_Widths, m_Arrows,
                    tempVerts, tempIndices);
                var updated = entry;
                updated.startVertexIndex = vAccum;
                updated.vertexCount = tempVerts.Count - vertBefore;
                updated.startIndexIndex = iAccum;
                updated.triangleCount = (tempIndices.Count - idxBefore) / 3;
                m_Curves[i] = updated;
                vAccum += updated.vertexCount;
                iAccum += updated.triangleCount * 3;
            }

            // 合并到主缓存
            int totalVertices = vAccum;
            int totalIndices = iAccum;

            while (m_MeshVertexCache.Count < totalVertices)
                m_MeshVertexCache.Add(new UIVertex());
            while (m_MeshIndexCache.Count < totalIndices)
                m_MeshIndexCache.Add(0);

            for (int i = 0; i < tempVerts.Count; i++)
                m_MeshVertexCache[preVertexCount + i] = tempVerts[i];

            for (int i = 0; i < tempIndices.Count; i++)
                m_MeshIndexCache[preIndexCount + i] = tempIndices[i] + preVertexCount;

            SetVerticesDirty();
            m_DirtyStartIndex = -1;
            m_DirtyEndIndex = -1;
        }

        #endregion

        #region OnPopulateMesh

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (m_MeshVertexCache.Count == 0 || m_MeshIndexCache.Count == 0) return;
            vh.AddUIVertexStream(m_MeshVertexCache, m_MeshIndexCache);
        }

        #endregion

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            RebuildAll();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion
    }
}
