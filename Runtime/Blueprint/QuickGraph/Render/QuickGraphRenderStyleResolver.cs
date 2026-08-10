using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using XericLibrary.Runtime.Blueprint;
using XericLibrary.Runtime.SuperStyleSheet;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	[Flags] public enum QuickGraphRenderStyleChange { None = 0, NodeGeometry = 1, NodeAppearance = 2, PortLayout = 4, WireGeometry = 8, WireAppearance = 16, GridAppearance = 32, Input = 64, Lod = 128, Selection = 256, All = NodeGeometry | NodeAppearance | PortLayout | WireGeometry | WireAppearance | GridAppearance | Input | Lod | Selection }
	public struct QuickGraphNodeRenderStyle { public float Width, Height, Border, Chamfer, TitleFontSize, AxisScaleX, AxisScaleY; public int ChamferSegments, SideCount; public TMP_FontAsset TitleFont; public TextAlignmentOptions TitleAlignment; public bool ShowTitleFull; public Color OuterColor; }
	public struct QuickGraphPortRenderStyle { public float InputSide, OutputSide, TopRatio, BottomRatio; }
	public struct QuickGraphWireRenderStyle { public float Width, HitRadius, SourceHandle, TargetHandle, ArrowWidth, ArrowHeight, ArrowProgress, ArrowDepth; public int Tessellation, MinTessellation; public ArrowShape ArrowShape; public bool ArrowReversed, UseNodeColorGradient; public Color Color; }
	public struct QuickGraphGridRenderStyle { public Material Material; public string ShaderName; public float Size, OverlayPower, Threshold, Exp, LodTransitionThreshold, LodTransitionStart, LodTransitionEnd, LineShapeScale; public Color Color, Background; public bool RaycastTarget, Maskable; public Vector2 CellCenter; }
	public struct QuickGraphInputRenderStyle { public float ZoomDivisor, DragPanSpeed, MinZoom, MaxZoom, DefaultZoom, FocusZoom, FocusPadding, KeyboardPanSpeed, KeyboardZoomSpeed; public int PanMouseButton; }
	public struct QuickGraphLodRenderStyle { public float EnterZoom, ExitZoom, WireTessellationMultiplier, NodeBorder; public bool UseNodeColorAsBackground, ShowTitle; }
	public struct QuickGraphSelectionRenderStyle { public float DragThreshold; public Color FillColor, OutlineColor; public Vector2 OutlineOffset; public bool RaycastTarget; }
	public sealed class QuickGraphRenderStyleSnapshot { public int Revision; public QuickGraphRenderStyleChange Changes; public QuickGraphNodeRenderStyle Node; public QuickGraphPortRenderStyle Port; public QuickGraphWireRenderStyle Wire; public QuickGraphGridRenderStyle Grid; public QuickGraphInputRenderStyle Input; public QuickGraphLodRenderStyle MinimalLod, SimplifiedLod, FullLod; public QuickGraphSelectionRenderStyle Selection; public QuickGraphLodRenderStyle GetLod(BlueprintLodLevel level) => level == BlueprintLodLevel.Minimal ? MinimalLod : level == BlueprintLodLevel.Simplified ? SimplifiedLod : FullLod; }

	public sealed class QuickGraphRenderStyleResolver : IDisposable
	{
		private readonly BlueprintGraph _graph;
		private static readonly ConditionalWeakTable<BlueprintGraph, ValidationState> _validationStates = new ConditionalWeakTable<BlueprintGraph, ValidationState>();
		private sealed class ValidationState { public string LastError; }
		public QuickGraphRenderStyleSnapshot Snapshot { get; private set; }
		public event Action<QuickGraphRenderStyleSnapshot> Changed;
		public QuickGraphRenderStyleResolver(BlueprintGraph graph) { _graph = graph; _graph.RenderStyleChanged += Refresh; StyleManager.OnStylesReloaded += Refresh; Refresh(); }
		private T Value<T>(string path) => _graph.GetRenderStyleValue<T>(path);
		private QuickGraphLodRenderStyle Lod(string name) => new QuickGraphLodRenderStyle { EnterZoom = Value<float>(QuickGraphStylePaths.Lod(name, "enterZoom")), ExitZoom = Value<float>(QuickGraphStylePaths.Lod(name, "exitZoom")), WireTessellationMultiplier = Value<float>(QuickGraphStylePaths.Lod(name, "wireTessellationMultiplier")), NodeBorder = Value<float>(QuickGraphStylePaths.Lod(name, "nodeBorder")), UseNodeColorAsBackground = Value<bool>(QuickGraphStylePaths.Lod(name, "useNodeColorAsBackground")), ShowTitle = Value<bool>(QuickGraphStylePaths.Lod(name, "showTitle")) };

		public void Refresh()
		{
			StyleManager.EnsureNamespaceLoaded(_graph.RenderStyleContext == null ? "default" : _graph.RenderStyleContext.BaseNamespace);
			var node = new QuickGraphNodeRenderStyle { Width = Value<float>(QuickGraphStylePaths.NodeWidth), Height = Value<float>(QuickGraphStylePaths.NodeHeight), Border = Value<float>(QuickGraphStylePaths.NodeBorder), Chamfer = Value<float>(QuickGraphStylePaths.NodeChamfer), ChamferSegments = Value<int>(QuickGraphStylePaths.NodeChamferSegments), TitleFontSize = Value<float>(QuickGraphStylePaths.NodeTitleFontSize), TitleFont = Value<TMP_FontAsset>(QuickGraphStylePaths.NodeTitleFont), TitleAlignment = Value<TextAlignmentOptions>(QuickGraphStylePaths.NodeTitleAlignment), ShowTitleFull = Value<bool>(QuickGraphStylePaths.NodeShowTitleFull), OuterColor = Value<Color>(QuickGraphStylePaths.NodeOuterColor), AxisScaleX = Value<float>(QuickGraphStylePaths.NodeAxisScaleX), AxisScaleY = Value<float>(QuickGraphStylePaths.NodeAxisScaleY), SideCount = Value<int>(QuickGraphStylePaths.NodeSideCount) };
			var port = new QuickGraphPortRenderStyle { InputSide = Value<float>(QuickGraphStylePaths.PortInputSide), OutputSide = Value<float>(QuickGraphStylePaths.PortOutputSide), TopRatio = Value<float>(QuickGraphStylePaths.PortTopRatio), BottomRatio = Value<float>(QuickGraphStylePaths.PortBottomRatio) };
			var wire = new QuickGraphWireRenderStyle { Width = Value<float>(QuickGraphStylePaths.WireWidth), HitRadius = Value<float>(QuickGraphStylePaths.WireHitRadius), SourceHandle = Value<float>(QuickGraphStylePaths.WireSourceHandle), TargetHandle = Value<float>(QuickGraphStylePaths.WireTargetHandle), ArrowWidth = Value<float>(QuickGraphStylePaths.WireArrowWidth), ArrowHeight = Value<float>(QuickGraphStylePaths.WireArrowHeight), ArrowProgress = Value<float>(QuickGraphStylePaths.WireArrowProgress), ArrowDepth = Value<float>(QuickGraphStylePaths.WireArrowDepth), Tessellation = Value<int>(QuickGraphStylePaths.WireTessellation), MinTessellation = Value<int>(QuickGraphStylePaths.WireMinTessellation), ArrowShape = Value<ArrowShape>(QuickGraphStylePaths.WireArrowShape), ArrowReversed = Value<bool>(QuickGraphStylePaths.WireArrowReversed), UseNodeColorGradient = Value<bool>(QuickGraphStylePaths.WireUseNodeColorGradient), Color = Value<Color>(QuickGraphStylePaths.WireColor) };
			var grid = new QuickGraphGridRenderStyle { Material = Value<Material>(QuickGraphStylePaths.GridMaterial), ShaderName = Value<string>(QuickGraphStylePaths.GridShader), Size = Value<float>(QuickGraphStylePaths.GridSize), OverlayPower = Value<float>(QuickGraphStylePaths.GridOverlayPower), Threshold = Value<float>(QuickGraphStylePaths.GridThreshold), Exp = Value<float>(QuickGraphStylePaths.GridExp), Color = Value<Color>(QuickGraphStylePaths.GridColor), Background = Value<Color>(QuickGraphStylePaths.GridBackground), RaycastTarget = Value<bool>(QuickGraphStylePaths.GridRaycastTarget), Maskable = Value<bool>(QuickGraphStylePaths.GridMaskable), LodTransitionThreshold = Value<float>(QuickGraphStylePaths.GridLodTransitionThreshold), LodTransitionStart = Value<float>(QuickGraphStylePaths.GridLodTransitionStart), LodTransitionEnd = Value<float>(QuickGraphStylePaths.GridLodTransitionEnd), CellCenter = Value<Vector2>(QuickGraphStylePaths.GridCellCenter), LineShapeScale = Value<float>(QuickGraphStylePaths.GridLineShapeScale) };
			var input = new QuickGraphInputRenderStyle { ZoomDivisor = Value<float>(QuickGraphStylePaths.InputZoomDivisor), DragPanSpeed = Value<float>(QuickGraphStylePaths.InputDragPanSpeed), MinZoom = Value<float>(QuickGraphStylePaths.InputMinZoom), MaxZoom = Value<float>(QuickGraphStylePaths.InputMaxZoom), DefaultZoom = Value<float>(QuickGraphStylePaths.InputDefaultZoom), FocusZoom = Value<float>(QuickGraphStylePaths.InputFocusZoom), FocusPadding = Value<float>(QuickGraphStylePaths.InputFocusPadding), KeyboardPanSpeed = Value<float>(QuickGraphStylePaths.InputKeyboardPanSpeed), KeyboardZoomSpeed = Value<float>(QuickGraphStylePaths.InputKeyboardZoomSpeed), PanMouseButton = Value<int>(QuickGraphStylePaths.InputPanMouseButton) };
			var selection = new QuickGraphSelectionRenderStyle { DragThreshold = Value<float>(QuickGraphStylePaths.SelectionDragThreshold), FillColor = Value<Color>(QuickGraphStylePaths.SelectionFillColor), OutlineColor = Value<Color>(QuickGraphStylePaths.SelectionOutlineColor), OutlineOffset = Value<Vector2>(QuickGraphStylePaths.SelectionOutlineOffset), RaycastTarget = Value<bool>(QuickGraphStylePaths.SelectionRaycastTarget) };
			var minimalLod = Lod("minimal"); var simplifiedLod = Lod("simplified"); var fullLod = Lod("full"); var old = Snapshot;
			var changes = old == null ? QuickGraphRenderStyleChange.All : Changes(old, node, port, wire, grid, input, minimalLod, simplifiedLod, fullLod, selection);
			Snapshot = new QuickGraphRenderStyleSnapshot { Revision = old == null ? 1 : old.Revision + 1, Changes = changes, Node = node, Port = port, Wire = wire, Grid = grid, Input = input, MinimalLod = minimalLod, SimplifiedLod = simplifiedLod, FullLod = fullLod, Selection = selection };
			Validate(node, wire, grid, input);
			Changed?.Invoke(Snapshot);
		}

		private void Validate(QuickGraphNodeRenderStyle node, QuickGraphWireRenderStyle wire, QuickGraphGridRenderStyle grid, QuickGraphInputRenderStyle input)
		{
			var errors = new List<string>();
			Require(errors, QuickGraphStylePaths.NodeWidth); Require(errors, QuickGraphStylePaths.NodeHeight); Require(errors, QuickGraphStylePaths.WireWidth); Require(errors, QuickGraphStylePaths.WireTessellation); Require(errors, QuickGraphStylePaths.WireMinTessellation); Require(errors, QuickGraphStylePaths.GridSize); Require(errors, QuickGraphStylePaths.InputMinZoom); Require(errors, QuickGraphStylePaths.InputMaxZoom); Require(errors, QuickGraphStylePaths.InputDefaultZoom);
			if (!Has(QuickGraphStylePaths.GridMaterial) && !Has(QuickGraphStylePaths.GridShader)) errors.Add($"缺少关键路径 path={QuickGraphStylePaths.GridMaterial} 或 path={QuickGraphStylePaths.GridShader} value=<missing> expected=至少提供一个可用的 Grid 材质或 Shader");
			if (node.Width <= 0f) errors.Add($"无效节点宽度 path={QuickGraphStylePaths.NodeWidth} value={node.Width} expected=> 0");
			if (node.Height <= 0f) errors.Add($"无效节点高度 path={QuickGraphStylePaths.NodeHeight} value={node.Height} expected=> 0");
			if (wire.Width <= 0f) errors.Add($"无效连线宽度 path={QuickGraphStylePaths.WireWidth} value={wire.Width} expected=> 0");
			if (wire.MinTessellation < 2 || wire.Tessellation < wire.MinTessellation) errors.Add($"无效连线细分 path={QuickGraphStylePaths.WireTessellation} value={wire.Tessellation}; path={QuickGraphStylePaths.WireMinTessellation} value={wire.MinTessellation} expected=tessellation >= minTessellation >= 2");
			if (grid.Size <= 0f) errors.Add($"无效网格尺寸 path={QuickGraphStylePaths.GridSize} value={grid.Size} expected=> 0");
			if (input.MinZoom <= 0f || input.MaxZoom <= input.MinZoom || input.DefaultZoom < input.MinZoom || input.DefaultZoom > input.MaxZoom) errors.Add($"无效缩放范围 path={QuickGraphStylePaths.InputMinZoom} value={input.MinZoom}; path={QuickGraphStylePaths.InputMaxZoom} value={input.MaxZoom}; path={QuickGraphStylePaths.InputDefaultZoom} value={input.DefaultZoom} expected=0 < minZoom <= defaultZoom <= maxZoom");
			bool materialUsable = grid.Material != null && grid.Material.shader != null;
			bool shaderUsable = !string.IsNullOrEmpty(grid.ShaderName) && Shader.Find(grid.ShaderName) != null;
			if (!materialUsable && !shaderUsable) errors.Add($"Grid 材质与 Shader 不可用 path={QuickGraphStylePaths.GridMaterial} value={grid.Material}; path={QuickGraphStylePaths.GridShader} value={grid.ShaderName ?? "<null>"} expected=有效 Material(含 shader) 或可由 Shader.Find 找到的 Shader 名称");
			string error = errors.Count == 0 ? null : $"[QuickGraph] 样式快照无效 GraphId={_graph.GraphId} ThemeName={_graph.ThemeName} Namespace={Namespace}:\n - " + string.Join("\n - ", errors);
			var state = _validationStates.GetOrCreateValue(_graph);
			if (error == state.LastError) return;
			state.LastError = error;
			if (error != null) Debug.LogError(error);
		}

		private string Namespace => _graph.RenderStyleContext == null || string.IsNullOrEmpty(_graph.RenderStyleContext.BaseNamespace) ? "default" : _graph.RenderStyleContext.BaseNamespace;
		private bool Has(string path) { return _graph.TryGetRenderStyle(path, out _); }
		private void Require(List<string> errors, string path) { if (!Has(path)) errors.Add($"缺少关键路径 path={path} value=<missing> expected=定义可解析的样式值"); }

		private static QuickGraphRenderStyleChange Changes(QuickGraphRenderStyleSnapshot old, QuickGraphNodeRenderStyle node, QuickGraphPortRenderStyle port, QuickGraphWireRenderStyle wire, QuickGraphGridRenderStyle grid, QuickGraphInputRenderStyle input, QuickGraphLodRenderStyle minimalLod, QuickGraphLodRenderStyle simplifiedLod, QuickGraphLodRenderStyle fullLod, QuickGraphSelectionRenderStyle selection)
		{
			var changes = QuickGraphRenderStyleChange.None;
			if (!Equals(old.Node, node)) changes |= QuickGraphRenderStyleChange.NodeGeometry | QuickGraphRenderStyleChange.NodeAppearance;
			if (!Equals(old.Port, port)) changes |= QuickGraphRenderStyleChange.PortLayout;
			if (!Equals(old.Wire, wire)) changes |= QuickGraphRenderStyleChange.WireGeometry | QuickGraphRenderStyleChange.WireAppearance;
			if (!Equals(old.Grid, grid)) changes |= QuickGraphRenderStyleChange.GridAppearance;
			if (!Equals(old.Input, input)) changes |= QuickGraphRenderStyleChange.Input;
			if (!Equals(old.MinimalLod, minimalLod) || !Equals(old.SimplifiedLod, simplifiedLod) || !Equals(old.FullLod, fullLod)) changes |= QuickGraphRenderStyleChange.Lod;
			if (!Equals(old.Selection, selection)) changes |= QuickGraphRenderStyleChange.Selection;
			return changes;
		}

		public void Dispose() { _graph.RenderStyleChanged -= Refresh; StyleManager.OnStylesReloaded -= Refresh; Changed = null; }
	}
}