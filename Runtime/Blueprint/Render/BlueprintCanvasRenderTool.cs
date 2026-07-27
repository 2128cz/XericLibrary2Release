using System;
using System.Collections.Generic;
using UnityEngine;
using XericLibrary.Runtime.MacroLibrary;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图画布渲染工具的抽象基类。
	/// <para>
	/// 职责：
	/// 1. 管理 Viewport Transform — 平移/缩放改为驱动 Transform，不触发 Mesh 重建。
	/// 2. 管理网格区块（Chunk）— 按画布坐标分块，每块一个渲染器，避开 65535 顶点限制。
	/// 3. 数据分组 — 脏元素的坐标 ÷ chunkSize → Spiral2DEncode → 按 Chunk 写入渲染器。
	/// </para>
	/// <para>
	/// 派生类只需实现三个抽象方法：
	/// <list type="bullet">
	///   <item><see cref="BuildChunkRenderer"/> — 在 Chunk GameObject 上创建渲染器组件</item>
	///   <item><see cref="AppendElementData"/> — 把元素数据写入该 Chunk 的渲染器</item>
	///   <item><see cref="ClearChunk"/> — 清空 Chunk 渲染器，准备下一帧重写</item>
	/// </list>
	/// </para>
	/// </summary>
	/// <typeparam name="TChildRenderer">Chunk 使用的渲染器组件类型</typeparam>
	public abstract class BlueprintCanvasRenderTool<TChildRenderer> : BlueprintTool
		where TChildRenderer : MonoBehaviour
	{
		// ====================================================================
		// 配置
		// ====================================================================

		/// <summary>网格区块大小（画布空间单位）。一个 Chunk 覆盖 chunkSize × chunkSize 区域。</summary>
		protected int ChunkSize = 400;

		/// <summary>Chunk 池的最大缓存数量</summary>
		protected int MaxPoolSize = 100;

		// ====================================================================
		// Chunk 数据
		// ====================================================================

		/// <summary>活跃区块：Spiral2DEncode ID → 渲染器实例</summary>
		protected Dictionary<ulong, TChildRenderer> ActiveChunks = new();

		/// <summary>缓存池，减少创建/销毁</summary>
		protected Stack<TChildRenderer> ChunkPool = new();

		/// <summary>Chunk 的父级 Transform（通常在 Viewport 下）</summary>
		protected Transform ChunksParent;

		// ====================================================================
		// Viewport 状态
		// ====================================================================

		/// <summary>当前同步到的 Viewport Transform</summary>
		protected RectTransform ViewportRect;

		private Vector2 _prevPan;
		private float _prevZoom;

		// ====================================================================
		// 抽象方法（派生类实现）
		// ====================================================================

		/// <summary>在 Chunk GameObject 上创建渲染器组件并返回。</summary>
		protected abstract TChildRenderer BuildChunkRenderer(GameObject chunkGo);

		/// <summary>将元素数据写入指定 Chunk 的渲染器。</summary>
		protected abstract void AppendElementData(TChildRenderer renderer, IBlueprintElement element);

		/// <summary>清空 Chunk 渲染器的数据，准备下一帧重写。</summary>
		protected abstract void ClearChunkData(TChildRenderer renderer);

		/// <summary>触发 Chunk 渲染器的脏标记，使其重建 Mesh。</summary>
		protected abstract void SetChunkDirty(TChildRenderer renderer);

		/// <summary>获取元素用于 Chunk 分组的坐标（通常是 Position 或中心点）。</summary>
		protected abstract Vector2 GetElementPosition(IBlueprintElement element);

		/// <summary>检查元素是否需要被剔除（例如不在视口内）。</summary>
		protected abstract bool IsElementCulled(IBlueprintElement element);

		// ====================================================================
		// 生命周期
		// ====================================================================

		public override void OnInitialize()
		{
			EnsureViewport();
		}

		public override void OnRender()
		{
			if (Graph?.Canvas == null) return;

			// 1. 确保层级存在
			EnsureViewport();

			// 2. 同步 Viewport Transform（O(1)，不重建 Mesh）
			SyncViewportTransform();

			// 3. 检查是否有脏元素
			if (Graph.DirtyFlags == null || !Graph.DirtyFlags.HasDirty)
				return;

			// 4. 收集并分组脏元素
			var groups = GroupDirtyElements();

			if (groups.Count == 0) return;

			// 5. 写入 Chunk 渲染器
			foreach (var kvp in groups)
			{
				var renderer = GetOrCreateChunk(kvp.Key);
				if (renderer == null) continue;

				ClearChunkData(renderer);
				foreach (var element in kvp.Value)
					AppendElementData(renderer, element);

				SetChunkDirty(renderer);
			}

			// 6. 清理空 Chunk
			CleanupInactiveChunks(groups.Keys);
		}

		public override void OnDestroy()
		{
			DestroyAllChunks();
			ViewportRect = null;
			ChunksParent = null;
		}

		// ====================================================================
		// Viewport
		// ====================================================================

		/// <summary>获取或创建 Viewport Transform。</summary>
		protected virtual void EnsureViewport()
		{
			if (ViewportRect != null) return;
			if (Graph?.Canvas == null) return;

			var root = Graph.Canvas.GetRootTransform();
			if (root == null) return;

			// 查找已有的 __Bp_Viewport
			var existing = root.Find("__Bp_Viewport");
			if (existing != null)
			{
				ViewportRect = existing.GetComponent<RectTransform>();
				ChunksParent = existing;
				return;
			}

			// 创建 Viewport
			var vpGo = new GameObject("__Bp_Viewport", typeof(RectTransform));
			vpGo.hideFlags = HideFlags.HideAndDontSave;
			vpGo.transform.SetParent(root, false);
			vpGo.transform.SetAsFirstSibling(); // 在 GridBackground 之下

			ViewportRect = vpGo.GetComponent<RectTransform>();
			ViewportRect.anchorMin = Vector2.zero;
			ViewportRect.anchorMax = Vector2.one;
			ViewportRect.offsetMin = Vector2.zero;
			ViewportRect.offsetMax = Vector2.zero;

			ChunksParent = ViewportRect;

			// 记录初始状态
			_prevPan = Graph.Canvas.PanOffset;
			_prevZoom = Graph.Canvas.ZoomLevel;
			ViewportRect.anchoredPosition = -_prevPan;
			ViewportRect.localScale = Vector3.one * _prevZoom;
		}

		/// <summary>将画布的 Pan/Zoom 同步到 Viewport Transform。</summary>
		protected virtual void SyncViewportTransform()
		{
			if (ViewportRect == null) return;

			var pan = Graph.Canvas.PanOffset;
			var zoom = Graph.Canvas.ZoomLevel;

			if (_prevPan != pan)
			{
				ViewportRect.anchoredPosition = -pan;
				_prevPan = pan;
			}

			if (!Mathf.Approximately(_prevZoom, zoom))
			{
				ViewportRect.localScale = Vector3.one * zoom;
				_prevZoom = zoom;
			}
		}

		// ====================================================================
		// Chunk 管理
		// ====================================================================

		/// <summary>将画布空间坐标转为 Chunk 坐标。</summary>
		protected (int cx, int cy) PositionToCoord(Vector2 canvasPos)
		{
			return (
				Mathf.FloorToInt(canvasPos.x / ChunkSize),
				Mathf.FloorToInt(canvasPos.y / ChunkSize)
			);
		}

		/// <summary>Chunk 坐标 → 螺旋曲线 ID。</summary>
		protected ulong CoordToID(int cx, int cy)
		{
			return MacroCurveMapping.Spiral2DEncode(cx, cy);
		}

		/// <summary>螺旋曲线 ID → Chunk 坐标。</summary>
		protected void IDToCoord(ulong id, out int cx, out int cy)
		{
			MacroCurveMapping.Spiral2DDecode(id, out cx, out cy);
		}

		/// <summary>获取或创建坐标对应的 Chunk 渲染器。</summary>
		protected TChildRenderer GetOrCreateChunk(ulong chunkID)
		{
			if (ActiveChunks.TryGetValue(chunkID, out var renderer) && renderer != null)
				return renderer;

			IDToCoord(chunkID, out int cx, out int cy);

			GameObject go;

			// 从池中取或新建
			if (ChunkPool.Count > 0)
			{
				go = ChunkPool.Pop().gameObject;
				go.SetActive(true);
			}
			else
			{
				go = new GameObject($"Chunk_{cx}_{cy}", typeof(RectTransform));
				go.hideFlags = HideFlags.HideAndDontSave;
				go.transform.SetParent(ChunksParent, false);
			}

			// 定位 Chunk
			var rt = go.GetComponent<RectTransform>();
			rt.pivot = Vector2.zero;                   // 左下角为原点
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.zero;               // 不拉伸，绝对定位
			rt.anchoredPosition = new Vector2(cx * ChunkSize, cy * ChunkSize);
			rt.sizeDelta = new Vector2(ChunkSize, ChunkSize);

			// 获取或创建渲染器
			renderer = go.GetComponent<TChildRenderer>();
			if (renderer == null)
				renderer = BuildChunkRenderer(go);

			ActiveChunks[chunkID] = renderer;
			return renderer;
		}

		/// <summary>收集脏元素并按 Chunk 分组。</summary>
		protected virtual Dictionary<ulong, List<IBlueprintElement>> GroupDirtyElements()
		{
			var groups = new Dictionary<ulong, List<IBlueprintElement>>();
			var dirtyList = Graph.DirtyFlags.FlushDirty();

			foreach (var element in dirtyList)
			{
				if (element == null) continue;

				// 剔除
				if (IsElementCulled(element)) continue;

				var pos = GetElementPosition(element);
				var (cx, cy) = PositionToCoord(pos);
				ulong id = CoordToID(cx, cy);

				if (!groups.TryGetValue(id, out var list))
				{
					list = new List<IBlueprintElement>();
					groups[id] = list;
				}
				list.Add(element);
			}

			return groups;
		}

		/// <summary>清理不再包含脏元素的 Chunk。</summary>
		protected virtual void CleanupInactiveChunks(Dictionary<ulong, List<IBlueprintElement>>.KeyCollection activeIDs)
		{
			var toRemove = new List<ulong>();
			foreach (var kvp in ActiveChunks)
			{
				if (!System.Linq.Enumerable.Contains(activeIDs, kvp.Key))
					toRemove.Add(kvp.Key);
			}

			foreach (var id in toRemove)
			{
				if (!ActiveChunks.TryGetValue(id, out var renderer) || renderer == null)
					continue;

				// 入池或销毁
				if (ChunkPool.Count < MaxPoolSize)
				{
					renderer.gameObject.SetActive(false);
					ChunkPool.Push(renderer);
				}
				else
				{
					UnityEngine.Object.Destroy(renderer.gameObject);
				}

				ActiveChunks.Remove(id);
			}
		}

		/// <summary>销毁所有 Chunk。</summary>
		protected virtual void DestroyAllChunks()
		{
			foreach (var kvp in ActiveChunks)
			{
				if (kvp.Value != null)
					UnityEngine.Object.Destroy(kvp.Value.gameObject);
			}
			ActiveChunks.Clear();

			while (ChunkPool.Count > 0)
			{
				var pooled = ChunkPool.Pop();
				if (pooled != null)
					UnityEngine.Object.Destroy(pooled.gameObject);
			}
		}
	}
}
