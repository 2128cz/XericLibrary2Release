using System;
using System.Collections.Generic;
using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>Chunk renderer 后端基类：仅消费 Graph.ChunkState 的提交，不计算归属或可见性。</summary>
	public abstract class BlueprintCanvasRenderTool<TChildRenderer> : BlueprintTool where TChildRenderer : MonoBehaviour
	{
		protected int MaxPoolSize = 100;
		protected readonly Dictionary<ulong, TChildRenderer> ActiveChunks = new Dictionary<ulong, TChildRenderer>();
		protected readonly Stack<TChildRenderer> ChunkPool = new Stack<TChildRenderer>();
		protected Transform ChunksParent;
		protected RectTransform ViewportRect;
		private Vector2 _prevPan;
		private float _prevZoom;

		protected abstract TChildRenderer BuildChunkRenderer(GameObject chunkGo);
		/// <summary>当前模板方法提交；子类不得重新计算 chunk 本地偏移。</summary>
		protected BlueprintElementRenderSubmission CurrentElementSubmission { get; private set; }
		protected abstract void AppendElementData(TChildRenderer renderer, IBlueprintElement element, ulong chunkID);
		protected abstract void ClearChunkData(TChildRenderer renderer);
		protected abstract void SetChunkDirty(TChildRenderer renderer);
		protected abstract bool AcceptsElement(IBlueprintElement element);
		/// <summary>每个非退出 Chunk 提交的开始钩子。</summary>
		protected virtual void BeginChunkSubmission(ulong chunkID, TChildRenderer renderer) { }
		/// <summary>每个非退出 Chunk 提交的结束钩子。</summary>
		protected virtual void EndChunkSubmission(ulong chunkID, TChildRenderer renderer) { }
		protected virtual void OnChunkRecycled(ulong chunkID, TChildRenderer renderer) { }

		public override void OnInitialize() => EnsureViewport();
		public override void OnConfigChanged() => Graph?.MarkAllElementsDirty();
		public override void OnRender()
		{
			if (Graph?.Canvas == null || Graph.ChunkState == null) return;
			EnsureViewport();
			if (ViewportRect == null) return;
			SyncViewportTransform();
			foreach (var submission in Graph.ChunkState.Submissions)
			{
				if (submission.Kind == ChunkSubmissionKind.Exited) { RecycleChunk(submission.ChunkId); continue; }
				var renderer = GetOrCreateChunk(submission.ChunkId);
				if (renderer == null) continue;
				ClearChunkData(renderer);
				BeginChunkSubmission(submission.ChunkId, renderer);
				var session = new RenderSession(Graph, submission);
				if (submission.Elements != null)
					foreach (var element in submission.Elements)
						if (AcceptsElement(element)) { CurrentElementSubmission = session.CreateElementSubmission(element); AppendElementData(renderer, element, submission.ChunkId); }
				CurrentElementSubmission = null;
				EndChunkSubmission(submission.ChunkId, renderer);
				SetChunkDirty(renderer);
			}
		}

		public override void OnDestroy()
		{
			DestroyAllChunks(); ViewportRect = null; ChunksParent = null;
		}

		protected virtual void EnsureViewport()
		{
			if (ViewportRect != null || Graph?.Canvas == null) return;
			var root = Graph.Canvas.GetRootTransform();
			if (root == null) return;
			ViewportRect = root.Find("__Bp_Viewport") as RectTransform;
			if (ViewportRect == null)
			{
				var go = new GameObject("__Bp_Viewport", typeof(RectTransform)); go.hideFlags = HideFlags.HideAndDontSave;
				ViewportRect = go.GetComponent<RectTransform>(); ViewportRect.SetParent(root, false);
			}
			var rootRect = root as RectTransform;
			var pivot = rootRect != null ? rootRect.pivot : new Vector2(.5f, .5f);
			ViewportRect.anchorMin = pivot; ViewportRect.anchorMax = pivot; ViewportRect.pivot = pivot;
			ViewportRect.anchoredPosition = Vector2.zero; ViewportRect.sizeDelta = rootRect != null ? rootRect.rect.size : Vector2.zero;
			ChunksParent = ViewportRect; _prevPan = Graph.Canvas.PanOffset; _prevZoom = Graph.Canvas.ZoomLevel;
			ViewportRect.anchoredPosition = _prevPan; ViewportRect.localScale = Vector3.one * _prevZoom;
		}

		protected virtual void SyncViewportTransform()
		{
			var pan = Graph.Canvas.PanOffset; var zoom = Graph.Canvas.ZoomLevel;
			if (_prevPan != pan) { ViewportRect.anchoredPosition = pan; _prevPan = pan; }
			if (!Mathf.Approximately(_prevZoom, zoom)) { ViewportRect.localScale = Vector3.one * zoom; _prevZoom = zoom; }
		}

		protected Vector2 ChunkOrigin(ulong id) => Graph.ChunkState.GetChunkOrigin(id);

		/// <summary>一次 chunk 提交的标准化坐标、LOD 与版本上下文。</summary>
		protected sealed class RenderSession
		{
			private readonly BlueprintGraph _graph; private readonly ChunkSubmission _submission; private readonly Vector2 _origin; private readonly Rect _bounds;
			public RenderSession(BlueprintGraph graph, ChunkSubmission submission) { _graph=graph; _submission=submission; _origin=graph.ChunkState.GetChunkOrigin(submission.ChunkId); _bounds=graph.ChunkState.GetChunkRect(submission.ChunkId); }
			public BlueprintElementRenderSubmission CreateElementSubmission(IBlueprintElement element) => new BlueprintElementRenderSubmission(element, element.BoundingBox.center, element.BoundingBox, _origin, _bounds, _graph.CurrentLod, _submission.ContentRevision, _submission.LodRevision);
		}

		public sealed class BlueprintElementRenderSubmission
		{
			public readonly IBlueprintElement Element; public readonly Vector2 CanvasPosition, ChunkOrigin, ChunkLocalPosition; public readonly Rect CanvasBounds, ChunkLocalBounds; public readonly BlueprintLodLevel LOD; public readonly int ContentRevision, LodRevision;
			public BlueprintElementRenderSubmission(IBlueprintElement element, Vector2 canvasPosition, Rect canvasBounds, Vector2 chunkOrigin, Rect chunkBounds, BlueprintLodLevel lod, int contentRevision, int lodRevision)
			{ Element=element; CanvasPosition=canvasPosition; CanvasBounds=canvasBounds; ChunkOrigin=chunkOrigin; ChunkLocalPosition=canvasPosition-chunkOrigin; ChunkLocalBounds=new Rect(canvasBounds.position-chunkOrigin, canvasBounds.size); LOD=lod; ContentRevision=contentRevision; LodRevision=lodRevision; }
			/// <summary>将提交元素相关的 Canvas 空间数据转换为当前 Chunk 局部空间。</summary>
			public Vector2 ToChunkLocal(Vector2 canvasPosition) => canvasPosition - ChunkOrigin;
			public Rect ToChunkLocal(Rect canvasBounds) => new Rect(canvasBounds.position - ChunkOrigin, canvasBounds.size);
		}
		protected TChildRenderer GetOrCreateChunk(ulong id)
		{
			if (ActiveChunks.TryGetValue(id, out var renderer) && renderer != null) return renderer;
			BlueprintChunkState.DecodeCoord(id, out int x, out int y);
			GameObject go;
			if (ChunkPool.Count > 0) { go = ChunkPool.Pop().gameObject; go.SetActive(true); }
			else { go = new GameObject("Chunk_" + x + "_" + y, typeof(RectTransform)); go.hideFlags = HideFlags.HideAndDontSave; go.transform.SetParent(ChunksParent, false); }
			var rt = go.GetComponent<RectTransform>(); var pivot = ViewportRect.pivot;
			rt.anchorMin = pivot; rt.anchorMax = pivot; rt.pivot = pivot; rt.anchoredPosition = ChunkOrigin(id);
			rt.sizeDelta = new Vector2(Graph.ChunkState.ChunkSize, Graph.ChunkState.ChunkSize);
			renderer = go.GetComponent<TChildRenderer>() ?? BuildChunkRenderer(go); ActiveChunks[id] = renderer; return renderer;
		}

		private void RecycleChunk(ulong id)
		{
			if (!ActiveChunks.TryGetValue(id, out var renderer) || renderer == null) { ActiveChunks.Remove(id); return; }
			OnChunkRecycled(id, renderer);
			if (ChunkPool.Count < MaxPoolSize) { renderer.gameObject.SetActive(false); ChunkPool.Push(renderer); }
			else UnityEngine.Object.Destroy(renderer.gameObject);
			ActiveChunks.Remove(id);
		}
		protected virtual void DestroyAllChunks()
		{
			foreach (var pair in ActiveChunks) if (pair.Value != null) UnityEngine.Object.Destroy(pair.Value.gameObject);
			ActiveChunks.Clear();
			while (ChunkPool.Count > 0) { var item = ChunkPool.Pop(); if (item != null) UnityEngine.Object.Destroy(item.gameObject); }
		}
	}
}