using UnityEngine;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图视口控制器 —— 管理视图状态（Pan/Zoom/范围），提供外部 API 供工具和外部代码读写。
	/// <para>
	/// 在 <c>ToolPhase.PreUpdate, order: 40</c> 执行，每帧直接将目标值写入 <see cref="IBlueprintCanvas"/>，
	/// 不做插值平滑或消费端缩放钳制。输入工具通过写入 <see cref="TargetPanOffset"/> / <see cref="TargetZoomLevel"/>
	/// 来驱动视图，外部代码可通过 <c>BlueprintToolProvider.GetTool&lt;BlueprintViewportController&gt;(graph)</c>
	/// 获取实例直接控制视图。
	/// </para>
	/// <para>
	/// 此工具为通用基础工具（无主题限定），对所有蓝图主题可见。
	/// </para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 40)]
	public class BlueprintViewportController : BlueprintTool
	{
		// ── 目标值（由输入工具或外部代码写入） ──

		/// <summary>目标平移偏移量。外部代码直接设置此值即可控制视图平移。</summary>
		public Vector2 TargetPanOffset = Vector2.zero;

		/// <summary>目标缩放级别。外部代码直接设置此值即可控制视图缩放。</summary>
		public float TargetZoomLevel;

		// ── 帧间脏检测 ──

		private Vector2 _prevPanOffset;
		private float _prevZoomLevel;
		private bool _initialized;

		public override void OnUpdate(float deltaTime)
		{
			if (Graph?.Canvas == null) return;

			var canvas = Graph.Canvas;

			// 首次同步使用样式默认缩放。
			if (!_initialized)
			{
				TargetPanOffset = canvas.PanOffset;
				TargetZoomLevel = Graph.GetRenderStyleValue<float>("/quickgraph/input/defaultZoom");
				_prevPanOffset = TargetPanOffset;
				_prevZoomLevel = TargetZoomLevel;
				_initialized = true;
			}

			// 缩放范围由样式写入 Canvas，由 Canvas 负责约束。
			canvas.MinZoom = Graph.GetRenderStyleValue<float>("/quickgraph/input/minZoom");
			canvas.MaxZoom = Graph.GetRenderStyleValue<float>("/quickgraph/input/maxZoom");

			// 直接写入 Canvas（无插值）
			canvas.PanOffset = TargetPanOffset;
			canvas.ZoomLevel = TargetZoomLevel;

			// 标记脏
			if (TargetPanOffset != _prevPanOffset || !Mathf.Approximately(TargetZoomLevel, _prevZoomLevel))
			{
				Graph.MarkDirty();
				_prevPanOffset = TargetPanOffset;
				_prevZoomLevel = TargetZoomLevel;
			}
		}

		// ====================================================================
		//  公共 API
		// ====================================================================

		/// <summary>重置视图到原点和样式默认缩放。</summary>
		public void ResetView()
		{
			TargetPanOffset = Vector2.zero;
			TargetZoomLevel = Graph.GetRenderStyleValue<float>("/quickgraph/input/defaultZoom");
			Graph?.MarkDirty();
		}

		/// <summary>聚焦到某个画布坐标点；省略缩放时使用样式聚焦缩放。</summary>
		public void FocusOn(Vector2 canvasCenter, float? zoom = null)
		{
			TargetZoomLevel = zoom ?? Graph.GetRenderStyleValue<float>("/quickgraph/input/focusZoom");
			if (Graph?.Canvas != null)
			{
				// local = canvas * zoom + pan；令目标画布点落在实际 viewport 的 RenderRoot local 中心。
				TargetPanOffset = Graph.Canvas.GetViewportLocalRect().center - canvasCenter * TargetZoomLevel;
			}
			else TargetPanOffset = -canvasCenter * TargetZoomLevel;
			Graph?.MarkDirty();
		}

		/// <summary>聚焦到一组元素的包围盒区域，使用样式聚焦边距。</summary>
		public void FocusOnElements(System.Collections.Generic.IReadOnlyList<IBlueprintElement> elements)
		{
			if (elements == null || elements.Count == 0) return;
			if (Graph?.Canvas == null) return;

			float padding = Graph.GetRenderStyleValue<float>("/quickgraph/input/focusPadding");

			// 计算所有元素的包围盒
			Rect bounds = elements[0].BoundingBox;
			for (int i = 1; i < elements.Count; i++)
			{
				var b = elements[i].BoundingBox;
				bounds.xMin = Mathf.Min(bounds.xMin, b.xMin);
				bounds.yMin = Mathf.Min(bounds.yMin, b.yMin);
				bounds.xMax = Mathf.Max(bounds.xMax, b.xMax);
				bounds.yMax = Mathf.Max(bounds.yMax, b.yMax);
			}

			bounds.xMin -= padding;
			bounds.yMin -= padding;
			bounds.xMax += padding;
			bounds.yMax += padding;

			// 以实际 viewport 的 RenderRoot local 尺寸适配；再复用 FocusOn 的统一公式。
			var viewportLocal = Graph.Canvas.GetViewportLocalRect();
			float zoomX = bounds.width > 0.0001f ? viewportLocal.width / bounds.width : Graph.GetRenderStyleValue<float>("/quickgraph/input/maxZoom");
			float zoomY = bounds.height > 0.0001f ? viewportLocal.height / bounds.height : Graph.GetRenderStyleValue<float>("/quickgraph/input/maxZoom");
			FocusOn(bounds.center, Mathf.Min(zoomX, zoomY));
		}
	}
}
