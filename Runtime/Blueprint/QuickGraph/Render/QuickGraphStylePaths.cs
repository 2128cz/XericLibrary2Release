namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	/// <summary>
	/// QuickGraph XSSS 路径的唯一声明处。
	/// 命名空间决定主题，路径仅描述视觉/交互要素。
	/// </summary>
	public static class QuickGraphStylePaths
	{
		public const string NodeRoot = "/quickgraph/node/";
		public const string NodeWidth = NodeRoot + "width";
		public const string NodeHeight = NodeRoot + "height";
		public const string NodeBorder = NodeRoot + "border";
		public const string NodeChamfer = NodeRoot + "chamfer";
		public const string NodeChamferSegments = NodeRoot + "chamferSegments";
		public const string NodeTitleFontSize = NodeRoot + "titleFontSize";
		public const string NodeTitleFont = NodeRoot + "titleFont";
		public const string NodeTitleAlignment = NodeRoot + "titleAlignment";
		public const string NodeShowTitleFull = NodeRoot + "showTitle/full";
		public const string NodeOuterColor = NodeRoot + "outerColor";
		public const string NodeAxisScaleX = NodeRoot + "axisScaleX";
		public const string NodeAxisScaleY = NodeRoot + "axisScaleY";
		public const string NodeSideCount = NodeRoot + "sideCount";
		public const string NodeFillColor = NodeRoot + "fillColor";
		public const string NodeBorderColor = NodeRoot + "borderColor";
		public const string NodeTitleColor = NodeRoot + "titleColor";

		public const string PortRoot = "/quickgraph/port/";
		public const string PortInputSide = PortRoot + "inputSide";
		public const string PortOutputSide = PortRoot + "outputSide";
		public const string PortTopRatio = PortRoot + "topRatio";
		public const string PortBottomRatio = PortRoot + "bottomRatio";

		public const string WireRoot = "/quickgraph/wire/";
		public const string WireWidth = WireRoot + "width";
		public const string WireHitRadius = WireRoot + "hitRadius";
		public const string WireSourceHandle = WireRoot + "sourceHandle";
		public const string WireTargetHandle = WireRoot + "targetHandle";
		public const string WireArrowWidth = WireRoot + "arrowWidth";
		public const string WireArrowHeight = WireRoot + "arrowHeight";
		public const string WireArrowProgress = WireRoot + "arrowProgress";
		public const string WireArrowDepth = WireRoot + "arrowDepth";
		public const string WireTessellation = WireRoot + "tessellation";
		public const string WireMinTessellation = WireRoot + "minTessellation";
		public const string WireArrowShape = WireRoot + "arrowShape";
		public const string WireArrowReversed = WireRoot + "arrowReversed";
		public const string WireUseNodeColorGradient = WireRoot + "useNodeColorGradient";
		public const string WireColor = WireRoot + "color";

		public const string GridRoot = "/quickgraph/grid/";
		public const string GridMaterial = GridRoot + "material";
		public const string GridShader = GridRoot + "shader";
		public const string GridSize = GridRoot + "size";
		public const string GridOverlayPower = GridRoot + "overlayPower";
		public const string GridThreshold = GridRoot + "threshold";
		public const string GridExp = GridRoot + "exp";
		public const string GridColor = GridRoot + "color";
		public const string GridBackground = GridRoot + "background";
		public const string GridRaycastTarget = GridRoot + "raycastTarget";
		public const string GridMaskable = GridRoot + "maskable";
		public const string GridLodTransitionThreshold = GridRoot + "lodTransitionThreshold";
		public const string GridLodTransitionStart = GridRoot + "lodTransitionStart";
		public const string GridLodTransitionEnd = GridRoot + "lodTransitionEnd";
		public const string GridCellCenter = GridRoot + "cellCenter";
		public const string GridLineShapeScale = GridRoot + "lineShapeScale";

		public const string InputRoot = "/quickgraph/input/";
		public const string InputZoomDivisor = InputRoot + "zoomDivisor";
		public const string InputDragPanSpeed = InputRoot + "dragPanSpeed";
		public const string InputMinZoom = InputRoot + "minZoom";
		public const string InputMaxZoom = InputRoot + "maxZoom";
		public const string InputDefaultZoom = InputRoot + "defaultZoom";
		public const string InputFocusZoom = InputRoot + "focusZoom";
		public const string InputFocusPadding = InputRoot + "focusPadding";
		public const string InputKeyboardPanSpeed = InputRoot + "keyboardPanSpeed";
		public const string InputKeyboardZoomSpeed = InputRoot + "keyboardZoomSpeed";
		public const string InputPanMouseButton = InputRoot + "panMouseButton";

		public const string SelectionRoot = "/quickgraph/selection/";
		public const string SelectionDragThreshold = SelectionRoot + "dragThreshold";
		public const string SelectionFillColor = SelectionRoot + "fillColor";
		public const string SelectionOutlineColor = SelectionRoot + "outlineColor";
		public const string SelectionOutlineOffset = SelectionRoot + "outlineOffset";
		public const string SelectionRaycastTarget = SelectionRoot + "raycastTarget";

		public static string Lod(string level, string property)
		{
			return "/quickgraph/lod/" + level + "/" + property;
		}
	}
}
