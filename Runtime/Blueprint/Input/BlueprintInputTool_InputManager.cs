#if !ENABLE_INPUT_SYSTEM

using XericLibrary.Runtime.MacroLibrary;
using UnityEngine;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入工具（旧输入系统）—— 自身完成全部鼠标/键盘/快捷键轮询，
	/// 通过 <c>KfsStyle</c> + <c>KeyFlippingStampSheet</c> 状态机驱动拖拽识别。
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 1)]
	[BlueprintTheme("QuickGraph")]
	public class BlueprintInputTool_InputManager : BlueprintInputTool_UGUI
	{
		// ===== 三键的 sheet 状态表 =====
		private readonly MacroKey.KeyFlippingStampSheet[] _sheets = new MacroKey.KeyFlippingStampSheet[3];
		private readonly Vector2[] _lastCanvasPos = new Vector2[3];

		// ===== 快捷键轮询常量 =====
		private static readonly KeyCode[] s_modifierKeys = new[]
		{
			KeyCode.LeftControl, KeyCode.RightControl,
			KeyCode.LeftShift, KeyCode.RightShift,
			KeyCode.LeftAlt, KeyCode.RightAlt,
		};

		// ===== 生命周期 =====

		public override void OnInitialize()
		{
			UseEventTriggerForMouse = false;
			base.OnInitialize();

			// 初始化三键的 sheet（button → KfsStyle → KeyFlippingStampSheet）
			for (int i = 0; i < 3; i++)
			{
				var style = i.AsKfsStyle();
				_sheets[i] = style.GetKeySheet();
			}

			RegisterShortcutHandlers();
		}

		public override void OnUpdate(float deltaTime)
		{
			// 基类：BeginFrame + 鼠标位置 + 光标上下文 + PointerMove
			base.OnUpdate(deltaTime);

			// 鼠标按键 / 滚轮 / 键盘 Pan/Zoom 轮询
			PollAll();

			// 快捷键轮询
			PollShortcuts();
		}

		// ===== 统一轮询 =====

		private void PollAll()
		{
			if (Graph?.Canvas == null) return;
			var canvas = Graph.Canvas;

			// 三键（由 sheet 状态机驱动）
			PollMouseButtonSheet(canvas, 0);
			PollMouseButtonSheet(canvas, 1);
			PollMouseButtonSheet(canvas, 2);

			// 滚轮
			float scrollY = Input.mouseScrollDelta.y;
			if (!Mathf.Approximately(scrollY, 0f))
			{
				BlueprintToolProvider.DispatchScroll(Graph, this,
					canvas.ScreenToCanvas(Input.mousePosition),
					new Vector2(0f, scrollY));
			}

			// 键盘 Pan / Zoom
			GraphInputTool.LastPanDirection = Vector2.zero;
			float x = 0f, y = 0f;
			if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))    y += 1f;
			if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))  y -= 1f;
			if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))  x -= 1f;
			if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
			GraphInputTool.LastPanDirection = new Vector2(x, y);
			GraphInputTool.LastZoomAxis = 0f;
			if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))    GraphInputTool.LastZoomAxis = 1f;
			if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))    GraphInputTool.LastZoomAxis = -1f;
		}

		/// <summary>
		/// 使用 sheet 状态机驱动单个鼠标按键的点击/拖拽/释放事件。
		/// 每帧调用 <c>AppendKeyState_PositionTriggle</c> 更新状态，
		/// 然后根据 <c>IsPressed / IsDragging / IsDropping / IsReleased</c> 分发到 Provider。
		/// </summary>
		private void PollMouseButtonSheet(IBlueprintCanvas canvas, int button)
		{
			var sheet = _sheets[button];
			bool pressed = Input.GetMouseButton(button);
			sheet.AppendKeyState_PositionTriggle(pressed, Input.mousePosition, false, 0.1f);

			// ── 按下 ──
			if (sheet.IsPressed)
			{
				var pt = canvas.ScreenToCanvas(Input.mousePosition);
				_lastCanvasPos[button] = pt;
				BlueprintToolProvider.DispatchPointerDown(Graph, this, pt, button);
			}

			// ── 拖拽（含首次拖拽帧） ──
			if (sheet.IsDragging)
			{
				var current = canvas.ScreenToCanvas(Input.mousePosition);
				var delta = current - _lastCanvasPos[button];
				_lastCanvasPos[button] = current;

				if (delta.sqrMagnitude > 0.0001f)
				{
					BlueprintToolProvider.DispatchPointerDrag(Graph, this, current, delta, button);
				}
			}

			// ── 释放 ──
			if (sheet.IsDropping || sheet.IsReleased)
			{
				var pt = canvas.ScreenToCanvas(Input.mousePosition);
				bool isEmptyClick = sheet.IsReleased && Graph?.HitTest(pt) == null;
				BlueprintToolProvider.DispatchPointerUp(Graph, this, pt, button, isEmptyClick);
			}
		}

		// ===== 快捷键动作注册 =====

		private void RegisterShortcutHandlers()
		{
			var tree = MacroKey.ShortcutTreeInstance;
			RegisterAction(tree, BlueprintInputConstants.FocusHome);
			RegisterAction(tree, BlueprintInputConstants.Undo);
			RegisterAction(tree, BlueprintInputConstants.Redo);
			RegisterAction(tree, BlueprintInputConstants.DeleteSelected);
			RegisterAction(tree, BlueprintInputConstants.SelectAll);
			RegisterAction(tree, BlueprintInputConstants.Duplicate);
			RegisterAction(tree, BlueprintInputConstants.Copy);
			RegisterAction(tree, BlueprintInputConstants.Paste);
			RegisterAction(tree, BlueprintInputConstants.Cut);
			RegisterAction(tree, BlueprintInputConstants.Save);
		}

		private void RegisterAction(ShortcutTree tree, string alias)
		{
			tree.RegisterAlias(alias, () => BlueprintToolProvider.DispatchAction(Graph, this, alias));
			tree.RebindAlias(alias, () => BlueprintToolProvider.DispatchAction(Graph, this, alias));
		}

		// ===== 快捷键轮询 =====

		private void PollShortcuts()
		{
			if (!Input.anyKeyDown) return;

			foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
			{
				if (IsModifier(key)) continue;
				if (!Input.GetKeyDown(key)) continue;

				bool ctrl  = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
				bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
				bool alt   = Input.GetKey(KeyCode.LeftAlt)   || Input.GetKey(KeyCode.RightAlt);
				char keyChar = KeyCodeToChar(key);

				MacroKey.ShortcutTreeInstance.CallShortcut(shift, ctrl, alt, keyChar);
				break;
			}
		}

		private static bool IsModifier(KeyCode key)
		{
			foreach (var mod in s_modifierKeys)
				if (key == mod) return true;
			return false;
		}

		private static char KeyCodeToChar(KeyCode key)
		{
			if (key >= KeyCode.A && key <= KeyCode.Z)
				return (char)('A' + (key - KeyCode.A));
			if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
				return (char)('0' + (key - KeyCode.Alpha0));
			if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9)
				return (char)('0' + (key - KeyCode.Keypad0));
			return (char)key;
		}
	}
}

#endif
