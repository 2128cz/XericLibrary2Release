using UnityEngine;
using XericLibrary.Runtime.Blueprint.Render;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	/// <summary>按源节点 Chunk 提交连线的渲染工具；所有局部坐标由基类 submission 提供。</summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 110)]
	[BlueprintTheme("QuickGraph")]
	public class WireCanvasRenderTool : BlueprintCanvasRenderTool<BlueprintCurveRenderer>
	{
		private QuickGraphWireRenderConfig _defaultConfig;
		private QuickGraphRenderStyleResolver _styleResolver;
		private int _appliedStyleRevision = -1;
		private QuickGraphWireRenderConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphWireRenderConfig external) return external;
				if (_defaultConfig == null) _defaultConfig = ScriptableObject.CreateInstance<QuickGraphWireRenderConfig>();
				return _defaultConfig;
			}
		}

		public override void OnInitialize()
		{
			base.OnInitialize();
			_styleResolver = new QuickGraphRenderStyleResolver(Graph, wireFallback: ConfigStyle);
			_styleResolver.Changed += OnStyleChanged;
			ApplyWireGeometry(_styleResolver.Snapshot.Wire, true);
		}
		public override void OnConfigChanged()
		{
			_styleResolver?.Refresh();
			if (_styleResolver == null) ApplyWireGeometry(ConfigStyle(), true);
			base.OnConfigChanged();
		}
		private QuickGraphWireRenderStyle ConfigStyle()
		{
			var cfg = Config;
			return new QuickGraphWireRenderStyle { Width = cfg.WireWidth, HitRadius = cfg.HitTestRadius, SourceHandle = cfg.SourceHandleLength, TargetHandle = cfg.TargetHandleLength, ArrowShape = cfg.ArrowShape, ArrowReversed = cfg.ArrowReversed, ArrowProgress = cfg.ArrowProgress, ArrowWidth = cfg.ArrowWidth, ArrowHeight = cfg.ArrowHeight, ArrowDepth = cfg.ArrowDepthCompensation, Tessellation = cfg.TessellationSegments };
		}
		private void OnStyleChanged(QuickGraphRenderStyleSnapshot snapshot)
		{
			if ((snapshot.Changes & (QuickGraphRenderStyleChange.WireGeometry | QuickGraphRenderStyleChange.WireAppearance)) == 0) return;
			ApplyWireGeometry(snapshot.Wire, (snapshot.Changes & QuickGraphRenderStyleChange.WireGeometry) != 0);
			Graph?.MarkAllElementsDirty();
		}
		private void ApplyWireGeometry(QuickGraphWireRenderStyle style, bool force)
		{
			if (Graph == null || (!force && _appliedStyleRevision == _styleResolver.Snapshot.Revision)) return;
			Graph.SetWireGeometrySettings(new BlueprintWireGeometrySettings { SourceHandleLength = style.SourceHandle, TargetHandleLength = style.TargetHandle, WireWidth = style.Width, HitTestRadius = style.HitRadius });
			foreach (var wire in Graph.Wires) Graph.UpdateWireGeometry(wire);
			_appliedStyleRevision = _styleResolver != null ? _styleResolver.Snapshot.Revision : _appliedStyleRevision + 1;
		}

		protected override BlueprintCurveRenderer BuildChunkRenderer(GameObject chunkGo) => chunkGo.AddComponent<BlueprintCurveRenderer>();
		protected override void ClearChunkData(BlueprintCurveRenderer renderer) => renderer.ClearAll();
		protected override void SetChunkDirty(BlueprintCurveRenderer renderer) => renderer.RebuildAll();
		protected override bool AcceptsElement(IBlueprintElement element) => element is BlueprintWire;
		protected override void AppendElementData(BlueprintCurveRenderer renderer, IBlueprintElement element, ulong chunkID)
		{
			if (!(element is BlueprintWire wire) || wire.SourcePort == null || wire.TargetPort == null || wire.SourcePort.OwnerNode == null || wire.TargetPort.OwnerNode == null) return;
			var style = _styleResolver != null ? _styleResolver.Snapshot.Wire : ConfigStyle();
			wire.GetControlPoints(out var startCanvas, out var handle1Canvas, out var handle2Canvas, out var endCanvas);
			var submission = CurrentElementSubmission;
			var ctrlPts = new[] { submission.ToChunkLocal(startCanvas), submission.ToChunkLocal(handle1Canvas), submission.ToChunkLocal(handle2Canvas), submission.ToChunkLocal(endCanvas) };
			var widths = new[] { style.Width, style.Width, style.Width, style.Width };
			Color32 srcColor = wire.SourcePort.OwnerNode.NodeColor;
			Color32 tgtColor = wire.TargetPort.OwnerNode.NodeColor;
			var arrow = new ArrowHeadData { shape = style.ArrowShape, reversed = style.ArrowReversed, progress = style.ArrowProgress, width = style.ArrowWidth, height = style.ArrowHeight, depthCompensation = style.ArrowDepth, color = tgtColor };
			var entry = new CurveCacheEntry { startColor = srcColor, endColor = tgtColor, tessellationSegments = Mathf.Max(2, Mathf.RoundToInt(style.Tessellation * Graph.CurrentLodSettings.WireTessellationMultiplier)) };
			renderer.AddCurve(entry, ctrlPts, widths, new[] { arrow });
		}
		public override void OnDestroy()
		{
			if (_styleResolver != null) _styleResolver.Changed -= OnStyleChanged;
			_styleResolver?.Dispose(); _styleResolver = null;
			base.OnDestroy();
		}
	}
}
