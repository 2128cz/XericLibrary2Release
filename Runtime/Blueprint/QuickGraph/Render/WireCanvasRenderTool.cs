using System.Collections.Generic;
using UnityEngine;
using XericLibrary.Runtime.Blueprint.Render;
using XericLibrary.Runtime.UIGraph;
namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	[BlueprintTool(phase: ToolPhase.Render, order: 110)] [BlueprintTheme("QuickGraph")]
	public class WireCanvasRenderTool : BlueprintCanvasRenderTool<BlueprintCurveRenderer>
	{
		private QuickGraphRenderStyleResolver _styles; private int _revision = -1;
		private readonly HashSet<string> _invalidWireWarningLogged = new HashSet<string>();
		public override void OnInitialize() { base.OnInitialize(); _styles = new QuickGraphRenderStyleResolver(Graph); _styles.Changed += OnStyleChanged; ApplyGeometry(_styles.Snapshot.Wire, true); }
		public override void OnConfigChanged() => _styles?.Refresh();
		private void OnStyleChanged(QuickGraphRenderStyleSnapshot snapshot) { ApplyGeometry(snapshot.Wire, true); Graph?.MarkAllElementsDirty(); }
		private void ApplyGeometry(QuickGraphWireRenderStyle style, bool force) { if (Graph == null || (!force && _revision == _styles.Snapshot.Revision)) return; Graph.SetWireGeometrySettings(new BlueprintWireGeometrySettings { SourceHandleLength = style.SourceHandle, TargetHandleLength = style.TargetHandle, WireWidth = style.Width, HitTestRadius = style.HitRadius }); foreach (var wire in Graph.Wires) Graph.UpdateWireGeometry(wire); _revision = _styles.Snapshot.Revision; }
		protected override BlueprintCurveRenderer BuildChunkRenderer(GameObject go) => go.AddComponent<BlueprintCurveRenderer>(); protected override void ClearChunkData(BlueprintCurveRenderer renderer) => renderer.ClearAll(); protected override void SetChunkDirty(BlueprintCurveRenderer renderer) => renderer.RebuildAll(); protected override bool AcceptsElement(IBlueprintElement element) => element is BlueprintWire;
		protected override void AppendElementData(BlueprintCurveRenderer renderer, IBlueprintElement element, ulong chunkID) { if (!(element is BlueprintWire wire)) return; if (wire.SourcePort == null || wire.TargetPort == null || wire.SourcePort.OwnerNode == null || wire.TargetPort.OwnerNode == null) { ReportInvalidWire(wire); return; } var style = _styles.Snapshot.Wire; wire.GetControlPoints(out var a, out var b, out var c, out var d); var submit = CurrentElementSubmission; var points = new[] { submit.ToChunkLocal(a), submit.ToChunkLocal(b), submit.ToChunkLocal(c), submit.ToChunkLocal(d) }; Color32 src = style.UseNodeColorGradient ? wire.SourcePort.OwnerNode.NodeColor : style.Color; Color32 dst = style.UseNodeColorGradient ? wire.TargetPort.OwnerNode.NodeColor : style.Color; var arrow = new ArrowHeadData { shape = style.ArrowShape, reversed = style.ArrowReversed, progress = style.ArrowProgress, width = style.ArrowWidth, height = style.ArrowHeight, depthCompensation = style.ArrowDepth, color = dst }; renderer.AddCurve(new CurveCacheEntry { startColor = src, endColor = dst, tessellationSegments = Mathf.RoundToInt(style.Tessellation * Graph.CurrentLodSettings.WireTessellationMultiplier) }, points, new[] { style.Width, style.Width, style.Width, style.Width }, new[] { arrow }); }
		private void ReportInvalidWire(BlueprintWire wire) { if (!_invalidWireWarningLogged.Add(wire.ElementId)) return; var missing = new List<string>(); if (wire.SourcePort == null) missing.Add("SourcePort"); else if (wire.SourcePort.OwnerNode == null) missing.Add("SourcePort.OwnerNode"); if (wire.TargetPort == null) missing.Add("TargetPort"); else if (wire.TargetPort.OwnerNode == null) missing.Add("TargetPort.OwnerNode"); Debug.LogWarning($"[WireCanvasRenderTool] 跳过无效 wire: GraphId={Graph.GraphId}, WireId={wire.ElementId}, missing={string.Join(", ", missing)}。"); }
		public override void OnDestroy() { if (_styles != null) _styles.Changed -= OnStyleChanged; _styles?.Dispose(); _styles = null; _invalidWireWarningLogged.Clear(); base.OnDestroy(); }
	}
}
