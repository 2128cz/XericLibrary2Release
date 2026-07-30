using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	/// <summary>
	/// QuickGraph 输入交互工具 —— 处理画布平移、缩放。
	/// <para>中键拖拽 = 平移；滚轮 = 缩放。</para>
	/// <para>视图状态读写委托给 <see cref="BlueprintViewportController"/>，
	/// 输入参数从 <see cref="QuickGraphInputConfig"/> 读取。</para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 50)]
	[BlueprintTheme("QuickGraph")]
	public class QuickGraphInputTool : BlueprintTool
	{
		// ---------- 配置（从 ToolConfigAssets 读取） ----------
		private QuickGraphInputConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphInputConfig external)
					return external;
				if (_defaultConfig == null)
					_defaultConfig = ScriptableObject.CreateInstance<QuickGraphInputConfig>();
				return _defaultConfig;
			}
		}
		private static QuickGraphInputConfig _defaultConfig;

		// ---------- 缓存的 ViewportController 引用 ----------
		private BlueprintViewportController _viewport;
		private BlueprintViewportController Viewport
		{
			get
			{
				if (_viewport == null && Graph != null)
					_viewport = BlueprintToolProvider.GetToolByType<BlueprintViewportController>(Graph);
				return _viewport;
			}
		}

		// ---------- 拖拽状态 ----------
		private bool    _isDragging;
		private int     _activeDragButton = -1;

		private Vector2 senderPanDirection() => GraphInputTool.GetAdapter(Graph)?.PanDirection ?? Vector2.zero;
		private float senderZoomAxis() => GraphInputTool.GetAdapter(Graph)?.ZoomAxis ?? 0f;

		// ---------- 选择状态 ----------
		private BlueprintElementBase _hoveredElement;

		public override void OnPointerMove(GraphInputTool sender, Vector2 canvasPoint, Vector2 delta)
		{
			if (Graph == null || _isDragging) return;

			var hit = Graph.HitTest(canvasPoint);
			_hoveredElement = hit as BlueprintElementBase;
		}

		/// <summary>当前悬停的元素（可能为 null）</summary>
		public BlueprintElementBase HoveredElement => _hoveredElement;

		/// <summary>当前选中的元素（委托给 SelectorHandle）。</summary>
		public BlueprintElementBase SelectedElement =>
			BlueprintToolProvider.GetSelector(Graph).PrimarySelection as BlueprintElementBase;

		public override void OnConfigChanged()
		{
			var viewport = Viewport;
			if (viewport == null) return;

			var cfg = Config;
			viewport.MinZoom = cfg.MinZoom;
			viewport.MaxZoom = cfg.MaxZoom;
			viewport.TargetZoomLevel = Mathf.Clamp(viewport.TargetZoomLevel, cfg.MinZoom, cfg.MaxZoom);

			// ViewportController 是 Canvas Pan/Zoom 的唯一提交者；这里只更新 controller 的范围与目标。
			Graph?.MarkDirty();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (Graph?.Canvas == null) return;
			var viewport = Viewport;
			if (viewport == null) return;

			var cfg = Config;

			// ── 同步缩放范围 ──
			viewport.MinZoom = cfg.MinZoom;
			viewport.MaxZoom = cfg.MaxZoom;

			// ── 键盘 Pan ──
			viewport.TargetPanOffset += senderPanDirection() * cfg.KeyboardPanSpeed * deltaTime;

			// ── 键盘 Zoom ──
			float zoomInput = senderZoomAxis();
			if (!Mathf.Approximately(zoomInput, 0f))
			{
				float oldZoom = viewport.TargetZoomLevel;
				float zoomDelta = zoomInput / cfg.ZoomDivisor;
				viewport.TargetZoomLevel = Mathf.Clamp(
					viewport.TargetZoomLevel + zoomDelta,
					viewport.MinZoom, viewport.MaxZoom);
				ApplyZoomFocus(oldZoom);
			}
		}

		// ===== 鼠标按下 =====

		public override void OnPointerDown(GraphInputTool sender, Vector2 canvasPoint, int mouseButton)
		{
			if (Graph == null) return;

			if (mouseButton == 2)
			{
				_isDragging = true;
				_activeDragButton = 2;
			}
		}

		// ===== 鼠标释放 =====

		public override void OnPointerUp(GraphInputTool sender, Vector2 canvasPoint, int mouseButton)
		{
			if (_isDragging && _activeDragButton == mouseButton)
			{
				_isDragging = false;
				_activeDragButton = -1;
			}
		}

		// ===== 拖拽 =====

		public override void OnPointerDrag(GraphInputTool sender, Vector2 canvasPoint, Vector2 delta, int mouseButton)
		{
			var viewport = Viewport;
			if (viewport == null) return;

			if (mouseButton == 2)
			{
				// canvas delta 在本帧 Pan 提交后会反向跳变；仅使用 RenderRoot local 平面 delta。
				viewport.TargetPanOffset += (sender?.PointerLocalDelta ?? Vector2.zero) * Config.DragPanSpeed;
				Graph.MarkDirty();
			}
		}

		// ===== 滚轮 =====

		public override void OnScroll(GraphInputTool sender, Vector2 canvasPoint, Vector2 scrollDelta)
		{
			var viewport = Viewport;
			if (viewport == null) return;

			float oldZoom = viewport.TargetZoomLevel;

			viewport.TargetZoomLevel = Mathf.Clamp(
				viewport.TargetZoomLevel + scrollDelta.y / Config.ZoomDivisor,
				viewport.MinZoom, viewport.MaxZoom);

			if (!Mathf.Approximately(oldZoom, viewport.TargetZoomLevel))
			{
				viewport.TargetPanOffset += canvasPoint * (oldZoom - viewport.TargetZoomLevel);
				Graph.MarkDirty();
			}
		}

		// ===== 快捷键 =====

		public override void OnAction(GraphInputTool sender, string actionName)
		{
			if (Graph?.Canvas == null) return;

			switch (actionName)
			{
				case BlueprintInputConstants.FocusHome:
					Viewport?.ResetView();
					break;
				case BlueprintInputConstants.Undo:
					break;
				case BlueprintInputConstants.Redo:
					break;
				case BlueprintInputConstants.NavigateParent:
					break;
			}
		}

		// ===== 以鼠标焦点为中心缩放（键盘调用 — 自行读取鼠标位置） =====

		private void ApplyZoomFocus(float oldZoom)
		{
			var viewport = Viewport;
			if (viewport == null || Graph?.Canvas == null) return;

			Vector2 canvasCursor = Graph.Canvas.ScreenToCanvas(BlueprintUGUIInputManager.MousePosition);
			viewport.TargetPanOffset += canvasCursor * (oldZoom - viewport.TargetZoomLevel);
		}
	}
}
