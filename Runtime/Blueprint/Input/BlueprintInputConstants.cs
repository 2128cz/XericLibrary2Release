using XericLibrary.Runtime.MacroLibrary;
using UnityEngine;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入系统的 Action Map / Action 名称常量与全局快捷键方案。
	/// <para>
	/// 快捷键通过 <see cref="MacroKey.SetShortcut(string, string)"/> 注册别名→组合键映射，
	/// 由具体输入工具（如 <c>BlueprintInputTool_InputManager</c>）在初始化时通过
	/// <see cref="ShortcutTree.RegisterAlias(string, Action)"/> 绑定实际动作后生效。
	/// </para>
	/// </summary>
	public static class BlueprintInputConstants
	{
		// ── Action Map ──
		public const string MapName = "Blueprint";

		// ── 鼠标 / 指针 Actions ──
		public const string Point = "Point";
		public const string ScrollWheel = "ScrollWheel";
		public const string LeftClick = "LeftClick";
		public const string RightClick = "RightClick";
		public const string MiddleClick = "MiddleClick";

		// ── 功能键 Actions ──
		public const string Shift = "Shift";
		public const string Ctrl = "Ctrl";
		public const string Alt = "Alt";

		// ── 快捷键组合 Actions ──
		public const string Pan = "Pan";
		public const string Zoom = "Zoom";
		public const string FocusHome = "FocusHome";
		public const string Undo = "Undo";
		public const string Redo = "Redo";
		public const string NavigateParent = "NavigateParent";
		public const string DeleteSelected = "DeleteSelected";
		public const string SelectAll = "SelectAll";
		public const string Duplicate = "Duplicate";
		public const string Copy = "Copy";
		public const string Paste = "Paste";
		public const string Cut = "Cut";
		public const string Save = "Save";

		// ===== 全局快捷键方案 =====
		//
		// 使用 MacroKey 的静态实例管理快捷键别名→组合键映射。
		// 具体工具初始化时通过 MacroKey.ShortcutTreeInstance.RegisterAlias / RebindAlias
		// 绑定实际动作委托。

		/// <summary>全局快捷键树（只读，操作请通过 MacroKey 静态方法）。</summary>
		public static ShortcutTree GlobalShortcuts => MacroKey.ShortcutTreeInstance;

		// 静态构造器：在类首次加载时自动注册标准快捷键方案
		static BlueprintInputConstants()
		{
			RegisterStandardShortcuts();
		}

		private static void RegisterStandardShortcuts()
		{
			MacroKey.SetShortcut(FocusHome,       MacroKey.Combo(false, false, false, (char)KeyCode.Home));
			MacroKey.SetShortcut(FocusHome,       MacroKey.Combo(false, false, false, (char)KeyCode.F));
			MacroKey.SetShortcut(Undo,            MacroKey.Combo(false, true,  false, 'Z'));
			MacroKey.SetShortcut(Redo,            MacroKey.Combo(true,  true,  false, 'Z'));
			MacroKey.SetShortcut(Redo,            MacroKey.Combo(false, true,  false, 'Y'));
			MacroKey.SetShortcut(DeleteSelected,  MacroKey.Combo(false, false, false, (char)KeyCode.Delete));
			MacroKey.SetShortcut(DeleteSelected,  MacroKey.Combo(false, false, false, (char)KeyCode.Backspace));
			MacroKey.SetShortcut(SelectAll,       MacroKey.Combo(false, true,  false, 'A'));
			MacroKey.SetShortcut(Duplicate,       MacroKey.Combo(false, true,  false, 'D'));
			MacroKey.SetShortcut(Copy,            MacroKey.Combo(false, true,  false, 'C'));
			MacroKey.SetShortcut(Paste,           MacroKey.Combo(false, true,  false, 'V'));
			MacroKey.SetShortcut(Cut,             MacroKey.Combo(false, true,  false, 'X'));
			MacroKey.SetShortcut(Save,            MacroKey.Combo(false, true,  false, 'S'));
		}

		public static readonly string[] ActionNames = new[]
		{
			FocusHome, Undo, Redo, NavigateParent,
			DeleteSelected, SelectAll, Duplicate, Copy, Paste, Cut, Save
		};
	}
}
