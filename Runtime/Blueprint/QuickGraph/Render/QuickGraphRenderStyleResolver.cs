using System;
using UnityEngine;
using XericLibrary.Runtime.Blueprint;
using XericLibrary.Runtime.SuperStyleSheet;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	[Flags] public enum QuickGraphRenderStyleChange { None = 0, NodeGeometry = 1, NodeAppearance = 2, WireGeometry = 4, WireAppearance = 8, GridAppearance = 16, All = NodeGeometry | NodeAppearance | WireGeometry | WireAppearance | GridAppearance }
	public struct QuickGraphNodeRenderStyle { public float Width, Height, Border, Chamfer; public int ChamferSegments; public float TitleFontSize; }
	public struct QuickGraphWireRenderStyle { public float Width, HitRadius, SourceHandle, TargetHandle, ArrowWidth, ArrowHeight, ArrowProgress, ArrowDepth; public int Tessellation; public ArrowShape ArrowShape; public bool ArrowReversed; }
	public struct QuickGraphGridRenderStyle { public Material Material; public string ShaderName; public float Size, OverlayPower, Threshold, Exp; public Color Color, Background; }
	public sealed class QuickGraphRenderStyleSnapshot { public int Revision; public QuickGraphRenderStyleChange Changes; public QuickGraphNodeRenderStyle Node; public QuickGraphWireRenderStyle Wire; public QuickGraphGridRenderStyle Grid; }

	/// <summary>每次样式变更只解析一次；渲染热路径只消费强类型快照。</summary>
	public sealed class QuickGraphRenderStyleResolver : IDisposable
	{
		private readonly BlueprintGraph _graph;
		private readonly Func<QuickGraphNodeRenderStyle> _nodeFallback;
		private readonly Func<QuickGraphWireRenderStyle> _wireFallback;
		private readonly Func<QuickGraphGridRenderStyle> _gridFallback;
		public QuickGraphRenderStyleSnapshot Snapshot { get; private set; }
		public event Action<QuickGraphRenderStyleSnapshot> Changed;

		public QuickGraphRenderStyleResolver(BlueprintGraph graph, Func<QuickGraphNodeRenderStyle> nodeFallback = null, Func<QuickGraphWireRenderStyle> wireFallback = null, Func<QuickGraphGridRenderStyle> gridFallback = null)
		{
			_graph = graph;
			_nodeFallback = nodeFallback ?? DefaultNode;
			_wireFallback = wireFallback ?? DefaultWire;
			_gridFallback = gridFallback ?? DefaultGrid;
			_graph.RenderStyleChanged += Refresh;
			StyleManager.OnStylesReloaded += Refresh;
			Refresh();
		}

		private static QuickGraphNodeRenderStyle DefaultNode() => new QuickGraphNodeRenderStyle { Width = 90f, Height = 130f, Border = 3f, Chamfer = .2f, ChamferSegments = 4, TitleFontSize = 14f };
		private static QuickGraphWireRenderStyle DefaultWire() => new QuickGraphWireRenderStyle { Width = 5f, HitRadius = 8f, SourceHandle = 80f, TargetHandle = 80f, ArrowWidth = 12f, ArrowHeight = 10f, ArrowProgress = 1f, ArrowDepth = -1f, Tessellation = 32, ArrowShape = ArrowShape.Triangle };
		private static QuickGraphGridRenderStyle DefaultGrid() => new QuickGraphGridRenderStyle { ShaderName = "XericLibrary/BluePrint/BlueprintBackgound_GridLine", Size = 100f, OverlayPower = 5f, Threshold = .99f, Exp = .001f, Color = Color.white, Background = Color.black };

		// 样式项存在即优先使用；只有项缺失时才使用旧 ToolConfig 回退值。
		private T Value<T>(string path, T fallback) { var value = _graph.GetRenderStyle(path); return value == null ? fallback : value.GetValueAs<T>(); }
		public void Refresh()
		{
			var node = _nodeFallback();
			node = new QuickGraphNodeRenderStyle { Width = Value("/quickgraph/node/width", node.Width), Height = Value("/quickgraph/node/height", node.Height), Border = Value("/quickgraph/node/border", node.Border), Chamfer = Value("/quickgraph/node/chamfer", node.Chamfer), ChamferSegments = Value("/quickgraph/node/chamferSegments", node.ChamferSegments), TitleFontSize = Value("/quickgraph/node/titleFontSize", node.TitleFontSize) };
			var wire = _wireFallback();
			wire = new QuickGraphWireRenderStyle { Width = Value("/quickgraph/wire/width", wire.Width), HitRadius = Value("/quickgraph/wire/hitRadius", wire.HitRadius), SourceHandle = Value("/quickgraph/wire/sourceHandle", wire.SourceHandle), TargetHandle = Value("/quickgraph/wire/targetHandle", wire.TargetHandle), ArrowWidth = Value("/quickgraph/wire/arrowWidth", wire.ArrowWidth), ArrowHeight = Value("/quickgraph/wire/arrowHeight", wire.ArrowHeight), ArrowProgress = Value("/quickgraph/wire/arrowProgress", wire.ArrowProgress), ArrowDepth = Value("/quickgraph/wire/arrowDepth", wire.ArrowDepth), Tessellation = Value("/quickgraph/wire/tessellation", wire.Tessellation), ArrowShape = Value("/quickgraph/wire/arrowShape", wire.ArrowShape), ArrowReversed = Value("/quickgraph/wire/arrowReversed", wire.ArrowReversed) };
			var grid = _gridFallback();
			grid = new QuickGraphGridRenderStyle { Material = Value("/quickgraph/grid/material", grid.Material), ShaderName = Value("/quickgraph/grid/shader", grid.ShaderName), Size = Value("/quickgraph/grid/size", grid.Size), OverlayPower = Value("/quickgraph/grid/overlayPower", grid.OverlayPower), Threshold = Value("/quickgraph/grid/threshold", grid.Threshold), Exp = Value("/quickgraph/grid/exp", grid.Exp), Color = Value("/quickgraph/grid/color", grid.Color), Background = Value("/quickgraph/grid/background", grid.Background) };

			var old = Snapshot;
			var changes = old == null ? QuickGraphRenderStyleChange.All : Classify(old, node, wire, grid);
			Snapshot = new QuickGraphRenderStyleSnapshot { Revision = old == null ? 1 : old.Revision + 1, Changes = changes, Node = node, Wire = wire, Grid = grid };
			if (changes != QuickGraphRenderStyleChange.None) Changed?.Invoke(Snapshot);
		}

		private static QuickGraphRenderStyleChange Classify(QuickGraphRenderStyleSnapshot old, QuickGraphNodeRenderStyle node, QuickGraphWireRenderStyle wire, QuickGraphGridRenderStyle grid)
		{
			QuickGraphRenderStyleChange changes = QuickGraphRenderStyleChange.None;
			if (old.Node.Width != node.Width || old.Node.Height != node.Height) changes |= QuickGraphRenderStyleChange.NodeGeometry;
			if (old.Node.Border != node.Border || old.Node.Chamfer != node.Chamfer || old.Node.ChamferSegments != node.ChamferSegments || old.Node.TitleFontSize != node.TitleFontSize) changes |= QuickGraphRenderStyleChange.NodeAppearance;
			if (old.Wire.Width != wire.Width || old.Wire.HitRadius != wire.HitRadius || old.Wire.SourceHandle != wire.SourceHandle || old.Wire.TargetHandle != wire.TargetHandle) changes |= QuickGraphRenderStyleChange.WireGeometry;
			if (old.Wire.ArrowWidth != wire.ArrowWidth || old.Wire.ArrowHeight != wire.ArrowHeight || old.Wire.ArrowProgress != wire.ArrowProgress || old.Wire.ArrowDepth != wire.ArrowDepth || old.Wire.Tessellation != wire.Tessellation || old.Wire.ArrowShape != wire.ArrowShape || old.Wire.ArrowReversed != wire.ArrowReversed) changes |= QuickGraphRenderStyleChange.WireAppearance;
			if (old.Grid.Material != grid.Material || old.Grid.ShaderName != grid.ShaderName || old.Grid.Size != grid.Size || old.Grid.OverlayPower != grid.OverlayPower || old.Grid.Threshold != grid.Threshold || old.Grid.Exp != grid.Exp || old.Grid.Color != grid.Color || old.Grid.Background != grid.Background) changes |= QuickGraphRenderStyleChange.GridAppearance;
			return changes;
		}
		public void Dispose() { _graph.RenderStyleChanged -= Refresh; StyleManager.OnStylesReloaded -= Refresh; Changed = null; }
	}
}
