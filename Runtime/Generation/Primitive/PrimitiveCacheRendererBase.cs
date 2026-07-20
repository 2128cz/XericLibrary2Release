using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.UIGraph
{
    /// <summary>
    /// 图元缓存渲染器基类
    /// 管理 PrimitiveCacheEntry 列表 + 脏标记范围更新
    /// LateUpdate 中只重建脏标记范围内的图元
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
	public abstract class PrimitiveCacheRendererBase : MaskableGraphic
	{
		#region 默认材质

		private static Material s_DefaultMaterial;
		private const string DefaultShaderPath = "XericLibrary/UIGraph/UIPattern";

		/// <summary>
		/// 获取图元渲染器的默认材质（基于 UIPattern.shader）
		/// </summary>
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

        /// <summary>
        /// 图元列表
        /// </summary>
        protected List<PrimitiveCacheEntry> m_Primitives = new List<PrimitiveCacheEntry>();

        /// <summary>
        /// 模型顶点缓存
        /// </summary>
        protected List<UIVertex> m_MeshVertexCache = new List<UIVertex>();

        /// <summary>
        /// 三角形索引缓存
        /// </summary>
        protected List<int> m_MeshIndexCache = new List<int>();

        /// <summary>
        /// 脏标记范围起点（图元索引），-1 表示无脏标记
        /// </summary>
        protected int m_DirtyStartIndex = -1;

        /// <summary>
        /// 脏标记范围终点（图元索引）
        /// </summary>
        protected int m_DirtyEndIndex = -1;

        #endregion

        #region API

        /// <summary>
        /// 添加一个图元
        /// </summary>
        /// <returns>图元索引</returns>
        public int AddPrimitive(PrimitiveCacheEntry entry)
        {
            int index = m_Primitives.Count;
            m_Primitives.Add(entry);
            MarkDirty(index);
            return index;
        }

        /// <summary>
        /// 移除指定图元
        /// </summary>
        public void RemovePrimitive(int index)
        {
            if (index < 0 || index >= m_Primitives.Count) return;
            m_Primitives.RemoveAt(index);
            RebuildAll();
        }

        /// <summary>
        /// 标记指定图元为脏
        /// </summary>
        public void SetPrimitiveDirty(int index)
        {
            MarkDirty(index);
        }

        /// <summary>
        /// 清空所有图元
        /// </summary>
        public void ClearAll()
        {
            m_Primitives.Clear();
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
        /// 标记所有图元为脏（下次 LateUpdate 全量重绘）
        /// 适用于：更新顶点坐标后，不改变元素数量的重绘
        /// </summary>
        public void RebuildAll()
        {
            m_DirtyStartIndex = 0;
            m_DirtyEndIndex = m_Primitives.Count - 1;
            if (m_DirtyEndIndex < 0)
                m_DirtyStartIndex = -1;
        }

        #endregion

        #region LateUpdate

        protected virtual void LateUpdate()
        {
            if (m_DirtyStartIndex < 0) return;

            // 全量重建策略：从脏范围起点开始，重绘该条目及后续所有条目
            // 保证所有条目 startVertexIndex/startIndexIndex 始终正确
            int start = m_DirtyStartIndex;

            // 统计脏范围前的顶点/索引数（这些条目无需重建）
            int preVertexCount = 0;
            int preIndexCount = 0;
            for (int i = 0; i < start; i++)
            {
                preVertexCount += m_Primitives[i].vertexCount;
                preIndexCount += m_Primitives[i].triangleCount * 3;
            }

            // 从 start 开始重建所有条目
            var tempVerts = new List<UIVertex>();
            var tempIndices = new List<int>();
            int vAccum = preVertexCount;
            int iAccum = preIndexCount;
            for (int i = start; i < m_Primitives.Count; i++)
            {
                var entry = m_Primitives[i];
                int vertBefore = tempVerts.Count;
                int idxBefore = tempIndices.Count;
                PrimitiveMeshGenerator.GeneratePrimitive(entry, tempVerts, tempIndices);
                var updated = entry;
                updated.startVertexIndex = vAccum;
                updated.vertexCount = tempVerts.Count - vertBefore;
                updated.startIndexIndex = iAccum;
                updated.triangleCount = (tempIndices.Count - idxBefore) / 3;
                m_Primitives[i] = updated;
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
