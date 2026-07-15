using UnityEngine;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图 UGUI 输入工具静态辅助类。
	/// 提供跨新旧输入系统的统一鼠标位置和按钮状态查询。
	/// </summary>
	public static class BlueprintUGUIInputManager
	{
		/// <summary>当前帧鼠标/指针在屏幕空间的位置，每帧由 BlueprintInputTool_UGUI 更新。</summary>
		public static Vector2 MousePosition { get; internal set; }

		/// <summary>
		/// 更新静态鼠标位置。
		/// 由 <see cref="BlueprintInputTool_UGUI"/> 每帧在 OnUpdate 中调用。
		/// </summary>
		public static void UpdateMousePosition()
		{
#if ENABLE_INPUT_SYSTEM
			MousePosition = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero;
#else
			MousePosition = (Vector2)Input.mousePosition;
#endif
		}

		/// <summary>
		/// 返回当前是否按住指定鼠标按钮。
		/// </summary>
		/// <param name="button">0=左键, 1=右键, 2=中键</param>
		public static bool IsButtonPressed(int button)
		{
#if ENABLE_INPUT_SYSTEM
			var mouse = UnityEngine.InputSystem.Mouse.current;
			if (mouse == null) return false;
			return button switch
			{
				0 => mouse.leftButton.isPressed,
				1 => mouse.rightButton.isPressed,
				2 => mouse.middleButton.isPressed,
				_ => false,
			};
#else
			return Input.GetMouseButton(button);
#endif
		}
	}
}
