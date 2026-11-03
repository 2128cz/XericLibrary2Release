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

        /// <summary>报表用：这一帧真正提交的顶点数（＝缓存收缩后的大小，不是历史最高水位）</summary>
        public int LiveVertexCount { get; private set; }

        /// <summary>
        /// 每块顶点上限：UGUI 的 65535 是**每个 Graphic 各自**的上限，而图元是按数据条数长的
        /// （散点一个点一个图元）⇒ 必须按预算切块，否则条数一多就撞。
        /// </summary>
        public const int ChunkVertexBudget = 16000;

        /// <summary>一块：只画图元 [FirstPrimitive, LastPrimitive)，顶点数受预算约束</summary>
        [RequireComponent(typeof(CanvasRenderer))]
        private sealed class PrimitiveChunk : MaskableGraphic
        {
            public PrimitiveCacheRendererBase Owner;
            public int FirstPrimitive;
            public int LastPrimitive;

            /// <summary>贴图跟宿主走：宿主换贴图，块也得跟着换</summary>
            public override Texture mainTexture
                => Owner != null ? Owner.mainTexture : base.mainTexture;

            protected override void OnEnable()
            {
                base.OnEnable();
                raycastTarget = false;
                SyncOwnerVisual();
            }

            /// <summary>
            /// 块是**独立的 Graphic**：宿主身上的材质不会自动继承。
            /// 注意 Graphic.material 的 getter 在没有用户材质时返回的是 defaultMaterial，
            /// 所以这里读到/写下的都是“宿主实际会用的那一份”（曲线基类那份就是 XericUICurve）。
            /// </summary>
            public void SyncOwnerVisual()
            {
                if (Owner == null) return;
                if (material != Owner.material) material = Owner.material;
            }

            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                if (Owner != null) Owner.PopulateSlice(vh, FirstPrimitive, LastPrimitive);
            }
        }

        private readonly List<PrimitiveChunk> m_Chunks = new List<PrimitiveChunk>();
        private int[] m_ChunkStarts = new int[8];

        /// <summary>报表用：切了几块 / 最大的那块有多少顶点</summary>
        public int ChunkCount => m_Chunks.Count;
        public int MaxChunkVertices { get; private set; }

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
            DisableChunks();
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

            // 跟着当前内容**收缩**：缓存只增不减时，OnPopulateMesh 会把整个列表交给 VertexHelper，
            // 提交的顶点数就变成"历史最高水位"——一旦超过 UGUI 的 65535 就永久报错（数据降回来也不回落）。
            if (m_MeshVertexCache.Count > totalVertices)
                m_MeshVertexCache.RemoveRange(totalVertices, m_MeshVertexCache.Count - totalVertices);
            if (m_MeshIndexCache.Count > totalIndices)
                m_MeshIndexCache.RemoveRange(totalIndices, m_MeshIndexCache.Count - totalIndices);
            LiveVertexCount = totalVertices;
            RebuildChunks();

            SetVerticesDirty();
            m_DirtyStartIndex = -1;
            m_DirtyEndIndex = -1;
        }

        #endregion

        #region OnPopulateMesh

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // 本体不画：所有图元都交给受顶点预算约束的子块（每块一个 Graphic，各自远低于 65535）
            vh.Clear();
        }

        /// <summary>
        /// 按顶点预算把图元切成若干块（顺序不变：块按提交顺序排、块内也按顺序，画家序不受影响）。
        /// </summary>
        private void RebuildChunks()
        {
            int count = m_Primitives.Count;
            if (m_ChunkStarts.Length < count + 1) m_ChunkStarts = new int[count + 1];

            int chunks = 0;
            int start = 0;
            int used = 0;
            for (int i = 0; i < count; i++)
            {
                int vertices = Mathf.Max(1, m_Primitives[i].vertexCount);
                if (used > 0 && used + vertices > ChunkVertexBudget)
                {
                    m_ChunkStarts[chunks++] = start;
                    start = i;
                    used = 0;
                }
                used += vertices;
            }
            if (count > 0) m_ChunkStarts[chunks++] = start;

            MaxChunkVertices = 0;
            for (int c = 0; c < chunks; c++)
            {
                int end = (c + 1 < chunks) ? m_ChunkStarts[c + 1] : count;
                int sum = 0;
                for (int i = m_ChunkStarts[c]; i < end; i++) sum += Mathf.Max(1, m_Primitives[i].vertexCount);
                if (sum > MaxChunkVertices) MaxChunkVertices = sum;
            }

            EnsureChunkCount(chunks);
            for (int c = 0; c < chunks; c++)
            {
                m_Chunks[c].FirstPrimitive = m_ChunkStarts[c];
                m_Chunks[c].LastPrimitive = (c + 1 < chunks) ? m_ChunkStarts[c + 1] : count;
                m_Chunks[c].enabled = true;
                m_Chunks[c].SyncOwnerVisual();
                m_Chunks[c].SetVerticesDirty();
            }
            for (int c = chunks; c < m_Chunks.Count; c++) m_Chunks[c].enabled = false;
        }

        private void EnsureChunkCount(int count)
        {
            while (m_Chunks.Count < count)
            {
                // Graphic 的 CanvasRenderer 是**懒创建**的（只在属性被访问时补），而 UGUI 的 Graphic
                // 自己并不要求它 —— 三个基类都是显式贴了 RequireComponent 才有的。块这里直接建出来，
                // 不依赖"哪天 Rebuild 恰好被调到"（没有它 Rebuild 第一句就 return，静默不画）。
                var chunkGo = new GameObject("PrimitiveChunk" + m_Chunks.Count,
                    typeof(RectTransform), typeof(CanvasRenderer));
                // 先挂父级再 AddComponent：OnEnable 里的 CacheCanvas 与注册看的是**当时**的父级
                var rect = (RectTransform)chunkGo.transform;
                rect.SetParent(transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                // 块是运行时建的，不标 DontSave 的话 Play 里存一下就会把它们写进场景/预制体
                chunkGo.hideFlags = HideFlags.DontSave;
                var chunk = chunkGo.AddComponent<PrimitiveChunk>();
                chunk.Owner = this;
                chunk.raycastTarget = false;
                m_Chunks.Add(chunk);
            }
        }

        private void DisableChunks()
        {
            for (int c = 0; c < m_Chunks.Count; c++)
            {
                if (m_Chunks[c] != null) m_Chunks[c].enabled = false;
            }
        }

        /// <summary>把图元 [first, last) 的顶点与三角形写进这个块的 VertexHelper（索引按块内起点重基）</summary>
        internal void PopulateSlice(VertexHelper vh, int firstPrimitive, int lastPrimitive)
        {
            if (firstPrimitive < 0) firstPrimitive = 0;
            if (lastPrimitive > m_Primitives.Count) lastPrimitive = m_Primitives.Count;
            if (firstPrimitive >= lastPrimitive) return;

            int vertexStart = 0;
            int indexStart = 0;
            for (int i = 0; i < firstPrimitive; i++)
            {
                vertexStart += m_Primitives[i].vertexCount;
                indexStart += m_Primitives[i].triangleCount * 3;
            }

            int vertexEnd = vertexStart;
            int indexEnd = indexStart;
            for (int i = firstPrimitive; i < lastPrimitive; i++)
            {
                vertexEnd += m_Primitives[i].vertexCount;
                indexEnd += m_Primitives[i].triangleCount * 3;
            }

            if (vertexEnd > m_MeshVertexCache.Count) vertexEnd = m_MeshVertexCache.Count;
            if (indexEnd > m_MeshIndexCache.Count) indexEnd = m_MeshIndexCache.Count;

            for (int v = vertexStart; v < vertexEnd; v++) vh.AddVert(m_MeshVertexCache[v]);
            for (int i = indexStart; i + 2 < indexEnd; i += 3)
            {
                vh.AddTriangle(m_MeshIndexCache[i] - vertexStart,
                    m_MeshIndexCache[i + 1] - vertexStart,
                    m_MeshIndexCache[i + 2] - vertexStart);
            }
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
            // 几何长在子块上，宿主关掉不等于它们停了：Graphic 停用只影响自己那份 VertexHelper
            DisableChunks();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            DestroyChunks();
            base.OnDestroy();
        }

        /// <summary>
        /// 块是宿主运行时建的子物体：只删组件不删它们，会留下一堆孤儿块，
        /// 组件再挂回来时 EnsureChunkCount 又建一组同名的 ⇒ 两组块叠着画。
        /// </summary>
        private void DestroyChunks()
        {
            for (int c = 0; c < m_Chunks.Count; c++)
            {
                var chunk = m_Chunks[c];
                if (chunk == null) continue;
                var chunkGo = chunk.gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(chunkGo); else Destroy(chunkGo);
#else
                Destroy(chunkGo);
#endif
            }
            m_Chunks.Clear();
        }

        #endregion
    }
}
