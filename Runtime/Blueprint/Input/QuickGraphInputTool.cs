using UnityEngine;
using XericLibrary.Runtime.Blueprint.Render;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// QuickGraph 输入交互工具 —— 处理画布平移、缩放和节点选择。
	/// <para>中键拖拽 = 平移；左键点击 = 选择；滚轮 = 缩放。</para>
	/// <para>快捷键（新输入系统）：方向键 = 平移，Ctrl+± = 缩放，Ctrl+H = 归位。</para>
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

		// ---------- 视图状态 ----------
		private Vector2 _targetPanOffset;
		private float   _targetZoomLevel = 1f;
		private bool    _viewInitialized;

		// 用于检测帧间变化
		private Vector2 _prevPanOffset;
		private float   _prevZoomLevel;

		// ---------- 拖拽状态 ----------
		private bool    _isDragging;
		private int     _activeDragButton = -1;
		private Vector2 _dragStartPan;

		// ---------- 选择状态 ----------
		private BlueprintElementBase _hoveredElement;
		private BlueprintElementBase _selectedElement;

		// ===== 更新（平滑插值 + 键盘轮询） =====

		public override void OnUpdate(float deltaTime)
		{
			if (Graph?.Canvas == null) return;

			var cfg = Config;
			var canvas = Graph.Canvas;

			// ── 同步缩放范围 ──
			canvas.MinZoom = cfg.MinZoom;
			canvas.MaxZoom = cfg.MaxZoom;

			if (!_viewInitialized)
			{
				_targetPanOffset = canvas.PanOffset;
				_targetZoomLevel = canvas.ZoomLevel;
				_prevPanOffset = _targetPanOffset;
				_prevZoomLevel = _targetZoomLevel;
				_viewInitialized = true;
			}

			// ── 键盘轮询（新输入系统 Pan 动作值） ──
			_targetPanOffset += BlueprintInputManager.LastPanDirection
				* cfg.KeyboardPanSpeed * deltaTime;

			// ── 键盘轮询（新输入系统 Zoom 动作值） ──
			// zoomInput 对键盘按键是 ±1（按下一下），对轴是 0~1 连续值。
			// 统一按一个 scroll notch = 120 缩放，scale = 120 / ZoomDivisor
			float zoomInput = BlueprintInputManager.LastZoomAxis;
			if (!Mathf.Approximately(zoomInput, 0f))
			{
				float oldZoom = _targetZoomLevel;
				_targetZoomLevel = Mathf.Clamp(
					_targetZoomLevel + zoomInput / cfg.ZoomDivisor,
					canvas.MinZoom, canvas.MaxZoom);
				// 以鼠标焦点为中心缩放（键盘无光标，从鼠标读取）
				ApplyZoomFocus(ref _targetPanOffset, _targetZoomLevel, oldZoom);
				// 归零，避免持续累加
				BlueprintInputManager.LastZoomAxis = 0f;
			}

			// ── 平滑插值到目标值 ──
			canvas.PanOffset = Vector2.Lerp(
				canvas.PanOffset, _targetPanOffset, cfg.PanLerpSpeed * deltaTime);
			canvas.ZoomLevel = Mathf.Lerp(
				canvas.ZoomLevel, _targetZoomLevel, cfg.ZoomLerpSpeed * deltaTime);

			// ── 标记脏 ──
			if (canvas.PanOffset != _prevPanOffset || !Mathf.Approximately(canvas.ZoomLevel, _prevZoomLevel))
			{
				Graph.MarkDirty();
				_prevPanOffset = canvas.PanOffset;
				_prevZoomLevel = canvas.ZoomLevel;
			}
		}

		// ===== 鼠标移动 =====

		public override void OnPointerMove(Vector2 canvasPoint, Vector2 delta)
		{
			if (Graph == null || _isDragging) return;

			var hit = Graph.HitTest(canvasPoint);
			_hoveredElement = hit as BlueprintElementBase;
		}

		// ===== 鼠标按下 =====

		public override void OnPointerDown(Vector2 canvasPoint, int mouseButton)
		{
			if (Graph == null) return;

			if (mouseButton == 2) // 中键 → 平移
			{
				_isDragging = true;
				_activeDragButton = 2;
				_dragStartPan = _targetPanOffset;
			}
			// 左键点击选择由元素层的 IBlueprintEventHandler.OnPointerDown 处理
		}

		// ===== 鼠标释放 =====

		public override void OnPointerUp(Vector2 canvasPoint, int mouseButton)
		{
			if (_isDragging && _activeDragButton == mouseButton)
			{
				_isDragging = false;
				_activeDragButton = -1;
			}
		}

		// ===== 拖拽 =====

		public override void OnPointerDrag(Vector2 canvasPoint, Vector2 delta, int mouseButton)
		{
			if (Graph?.Canvas == null) return;

			if (mouseButton == 2) // 中键拖拽 = 平移
			{
				_targetPanOffset += delta * Graph.Canvas.ZoomLevel;
				Graph.MarkDirty();
			}
		}

		// ===== 滚轮 =====

		public override void OnScroll(Vector2 canvasPoint, Vector2 scrollDelta)
		{
			if (Graph == null) return;

			float oldZoom = _targetZoomLevel;

			_targetZoomLevel = Mathf.Clamp(
				_targetZoomLevel + scrollDelta.y / Config.ZoomDivisor,
				Graph.Canvas.MinZoom, Graph.Canvas.MaxZoom);

			if (!Mathf.Approximately(oldZoom, _targetZoomLevel))
			{
				// 以鼠标光标为中心缩放
				_targetPanOffset += canvasPoint * (oldZoom - _targetZoomLevel);
				Graph.MarkDirty();
			}
		}

		// ===== 快捷键（仅新输入系统） =====

		public override void OnAction(string actionName)
		{
			if (Graph?.Canvas == null) return;

			switch (actionName)
			{
				case BlueprintInputConstants.FocusHome:
					ResetView();
					break;
				case BlueprintInputConstants.Undo:
					break; // TODO
				case BlueprintInputConstants.Redo:
					break; // TODO
				case BlueprintInputConstants.NavigateParent:
					break; // TODO
			}
		}

		// ===== 辅助 =====

		public void ResetView()
		{
			_targetPanOffset = Vector2.zero;
			_targetZoomLevel = 1f;
			Graph?.MarkDirty();
		}

		private void SelectElement(BlueprintElementBase element)
		{
			_selectedElement = element;
		}

		/// <summary>当前悬停的元素（可能为 null）</summary>
		public BlueprintElementBase HoveredElement => _hoveredElement;

		/// <summary>当前选中的元素（可能为 null）</summary>
		public BlueprintElementBase SelectedElement => _selectedElement;

		// ===== 以鼠标焦点为中心缩放（键盘调用 — 自行读取鼠标位置） =====

		private void ApplyZoomFocus(ref Vector2 panOffset, float newZoom, float oldZoom)
		{
			if (Graph?.Canvas == null) return;

			// 通过 BlueprintUGUIInputManager 获取统一鼠标位置（已被 BlueprintInputReceiver 每帧刷新）
			Vector2 canvasCursor = Graph.Canvas.ScreenToCanvas(BlueprintUGUIInputManager.MousePosition);
			panOffset += canvasCursor * (oldZoom - newZoom);
		}

		// =====（旧 ZoomFocusAdjust 已删除 — 替代为 ApplyZoomFocus，OnScroll 直接内联 canvasPoint 计算）=====
	}
}
