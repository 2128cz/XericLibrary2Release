#if !ENABLE_INPUT_SYSTEM

using UnityEngine;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入管理器（旧输入系统）—— 通过旧输入系统轮询鼠标和键盘事件。
	/// <para>
	/// 提供与 <c>BlueprintInputManager.InputSystem.cs</c> 相同的静态 API 签名，
	/// 但底层使用 <see cref="Input.GetMouseButton"/>/<see cref="Input.GetKey"/> 等旧 API，
	/// 不依赖任何新输入系统类型。
	/// </para>
	/// <para>
	/// 鼠标点击、拖拽、右键等事件通过 <see cref="PollMouseEvents"/> 轮询后
	/// 分发到 <see cref="BlueprintToolProvider"/> 的 DispatchXxx 方法。
	/// 键盘 Pan/Zoom 值通过 <see cref="PollKeyboard"/> 轮询后由
	/// <see cref="QuickGraphInputTool"/> 在 OnUpdate 中读取。
	/// </para>
	/// </summary>
	public static class BlueprintInputManager
	{
		/// <summary>
		/// 当前焦点蓝图（快捷键分发目标）。
		/// 由 <see cref="SetFocusedGraph"/> 设置。
		/// </summary>
		private static BlueprintGraph s_focusedGraph;

		/// <summary>设置当前焦点蓝图。</summary>
		public static void SetFocusedGraph(BlueprintGraph graph)
		{
			s_focusedGraph = graph;
		}

		/// <summary>
		/// 注册 InputActionAsset（旧系统空操作）。
		/// 仅保留签名以兼容编译。
		/// </summary>
		public static void RegisterAsset(Object asset)
		{
			// 旧输入系统：不管理 InputActionAsset
		}

		/// <summary>
		/// 注销 InputActionAsset（旧系统空操作）。
		/// 仅保留签名以兼容编译。
		/// </summary>
		public static void UnregisterAsset(Object asset)
		{
			// 旧输入系统：不管理 InputActionAsset
		}

		// ===== 键盘轮询值（供 QuickGraphInputTool.OnUpdate 读取） =====

		/// <summary>
		/// 方向键 Pan 方向值（每帧由 <see cref="PollKeyboard"/> 更新）。
		/// </summary>
		public static Vector2 LastPanDirection;

		/// <summary>
		/// +/- 键 Zoom 轴值（每帧由 <see cref="PollKeyboard"/> 更新）。
		/// </summary>
		public static float LastZoomAxis;

		// ===== 事件轮询 =====

		/// <summary>
		/// 轮询鼠标按键事件。
		/// 每帧在 Update 中调用，检测鼠标按下/释放并分发到 <see cref="BlueprintToolProvider"/>。
		/// </summary>
		/// <param name="mouseButton">0=左键, 1=右键, 2=中键</param>
		public static void PollMouseEvents(int mouseButton)
		{
			if (s_focusedGraph?.Canvas == null) return;

			s_focusedGraph.BeginFrame();

			if (Input.GetMouseButtonDown(mouseButton))
			{
				var canvasPoint = s_focusedGraph.Canvas.ScreenToCanvas(Input.mousePosition);
				BlueprintToolProvider.DispatchPointerDown(s_focusedGraph, canvasPoint, mouseButton);
			}

			if (Input.GetMouseButtonUp(mouseButton))
			{
				var canvasPoint = s_focusedGraph.Canvas.ScreenToCanvas(Input.mousePosition);
				BlueprintToolProvider.DispatchPointerUp(s_focusedGraph, canvasPoint, mouseButton);
			}
		}

		/// <summary>
		/// 轮询鼠标滚轮事件。
		/// 每帧在 Update 中调用，检测滚轮增量并分发到 <see cref="BlueprintToolProvider"/>。
		/// </summary>
		public static void PollScrollWheel()
		{
			if (s_focusedGraph?.Canvas == null) return;

			s_focusedGraph.BeginFrame();

			float scrollY = Input.mouseScrollDelta.y;
			if (!Mathf.Approximately(scrollY, 0f))
			{
				var canvasPoint = s_focusedGraph.Canvas.ScreenToCanvas(Input.mousePosition);
				var scrollDelta = new Vector2(0f, scrollY);
				BlueprintToolProvider.DispatchScroll(s_focusedGraph, canvasPoint, scrollDelta);
			}
		}

		/// <summary>
		/// 轮询键盘快捷键（方向键 Pan、+/- Zoom）。
		/// 每帧在 Update 中调用，更新 <see cref="LastPanDirection"/> 和 <see cref="LastZoomAxis"/>。
		/// </summary>
		public static void PollKeyboard()
		{
			LastPanDirection = Vector2.zero;
			if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
				LastPanDirection.y += 1f;
			if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
				LastPanDirection.y -= 1f;
			if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
				LastPanDirection.x -= 1f;
			if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
				LastPanDirection.x += 1f;

			LastZoomAxis = 0f;
			if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
				LastZoomAxis = 1f;
			if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
				LastZoomAxis = -1f;
		}
	}
}

#endif
