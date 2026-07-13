namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入系统的 Action Map / Action 名称常量。
	/// 供 Editor 的 InputAction 模板和 Runtime 的输入处理工具共同引用，
	/// 避免字符串硬编码散布各处。
	/// </summary>
	public static class BlueprintInputConstants
	{
		// ── Action Map ──
		public const string MapName = "Blueprint";

		// ── 鼠标 / 指针 Actions（名称须与 UnityEngine.InputSystem.UI.InputSystemUIInputModule 官方预设一致）──
		/// <summary>画布内指针位置（Vector2）。InputSystemUIInputModule 预设名称。</summary>
		public const string Point = "Point";
		/// <summary>滚轮滚动（Vector2）。InputSystemUIInputModule 预设名称。</summary>
		public const string ScrollWheel = "ScrollWheel";
		/// <summary>左键点击（Button）。InputSystemUIInputModule 预设名称。</summary>
		public const string LeftClick = "LeftClick";
		/// <summary>右键点击（Button）。InputSystemUIInputModule 预设名称。</summary>
		public const string RightClick = "RightClick";
		/// <summary>中键点击（Button）。InputSystemUIInputModule 预设名称。</summary>
		public const string MiddleClick = "MiddleClick";

		// ── 功能键 Actions ──
		/// <summary>左 Shift（Button）</summary>
		public const string Shift = "Shift";
		/// <summary>左 Ctrl（Button）</summary>
		public const string Ctrl = "Ctrl";
		/// <summary>左 Alt（Button）</summary>
		public const string Alt = "Alt";

		// ── 快捷键组合 Actions（仅新输入系统）──
		/// <summary>平移画布（Vector2，4方向）</summary>
		public const string Pan = "Pan";
		/// <summary>缩放画布（float，滚轮 + Ctrl 组合）</summary>
		public const string Zoom = "Zoom";
		/// <summary>定位到核心节点</summary>
		public const string FocusHome = "FocusHome";
		/// <summary>撤销</summary>
		public const string Undo = "Undo";
		/// <summary>重做</summary>
		public const string Redo = "Redo";
		/// <summary>进入父级</summary>
		public const string NavigateParent = "NavigateParent";
	}
}
