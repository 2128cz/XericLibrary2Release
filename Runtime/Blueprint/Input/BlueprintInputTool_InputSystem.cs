#if ENABLE_INPUT_SYSTEM

using XericLibrary.Runtime.MacroLibrary;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入工具（新输入系统）—— 自身管理 InputActionAsset 绑定和快捷键回调，
	/// 不再依赖 <c>BlueprintInputManager</c> 管理器。
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 1)]
	[BlueprintTheme("QuickGraph")]
	public class BlueprintInputTool_InputSystem : BlueprintInputTool_UGUI
	{
		private InputActionAsset _inputActionAsset;
		private InputActionMap _activeMap;

		// 缓存的动作回调实例
		private System.Action<InputAction.CallbackContext> _panHandler;
		private System.Action<InputAction.CallbackContext> _panCanceled;
		private System.Action<InputAction.CallbackContext> _zoomHandler;
		private System.Action<InputAction.CallbackContext> _zoomCanceled;
		private System.Action<InputAction.CallbackContext> _homeHandler;
		private System.Action<InputAction.CallbackContext> _undoHandler;
		private System.Action<InputAction.CallbackContext> _redoHandler;
		private System.Action<InputAction.CallbackContext> _parentHandler;

		// ===== InputActionAsset 属性 =====

		public InputActionAsset InputActionAsset
		{
			get => _inputActionAsset;
			set
			{
				if (_inputActionAsset == value) return;
				UnbindCurrent();
				_inputActionAsset = value;
				if (_inputActionAsset != null && Graph != null)
					BindCurrent();
			}
		}

		// ===== 生命周期 =====

		public override void OnInitialize()
		{
			// 创建回调委托（确保 Bind/Unbind 使用同一引用）
			_panHandler   = ctx => GraphInputTool.LastPanDirection = ctx.ReadValue<Vector2>();
			_panCanceled  = _   => GraphInputTool.LastPanDirection = Vector2.zero;
			_zoomHandler  = ctx => GraphInputTool.LastZoomAxis = ctx.ReadValue<float>();
			_zoomCanceled = _   => GraphInputTool.LastZoomAxis = 0f;
			_homeHandler  = _   => DispatchShortcut(BlueprintInputConstants.FocusHome);
			_undoHandler  = _   => DispatchShortcut(BlueprintInputConstants.Undo);
			_redoHandler  = _   => DispatchShortcut(BlueprintInputConstants.Redo);
			_parentHandler = _  => DispatchShortcut(BlueprintInputConstants.NavigateParent);

			base.OnInitialize();
			RegisterShortcutHandlers();
		}

		public override void OnDestroy()
		{
			UnbindCurrent();
			base.OnDestroy();
		}

		// ===== ActionMap 绑定/解绑 =====

		private void BindCurrent()
		{
			if (_inputActionAsset == null) return;
			var map = _inputActionAsset.FindActionMap(BlueprintInputConstants.MapName);
			if (map == null) return;

			map.Enable();
			Bind(map, BlueprintInputConstants.Pan,        _panHandler);
			BindCancel(map, BlueprintInputConstants.Pan,   _panCanceled);
			Bind(map, BlueprintInputConstants.Zoom,       _zoomHandler);
			BindCancel(map, BlueprintInputConstants.Zoom,  _zoomCanceled);
			Bind(map, BlueprintInputConstants.FocusHome,   _homeHandler);
			Bind(map, BlueprintInputConstants.Undo,        _undoHandler);
			Bind(map, BlueprintInputConstants.Redo,        _redoHandler);
			Bind(map, BlueprintInputConstants.NavigateParent, _parentHandler);
			_activeMap = map;
		}

		private void UnbindCurrent()
		{
			if (_activeMap == null) return;
			Unbind(_activeMap, BlueprintInputConstants.Pan,        _panHandler);
			UnbindCancel(_activeMap, BlueprintInputConstants.Pan,  _panCanceled);
			Unbind(_activeMap, BlueprintInputConstants.Zoom,       _zoomHandler);
			UnbindCancel(_activeMap, BlueprintInputConstants.Zoom, _zoomCanceled);
			Unbind(_activeMap, BlueprintInputConstants.FocusHome,   _homeHandler);
			Unbind(_activeMap, BlueprintInputConstants.Undo,        _undoHandler);
			Unbind(_activeMap, BlueprintInputConstants.Redo,        _redoHandler);
			Unbind(_activeMap, BlueprintInputConstants.NavigateParent, _parentHandler);
			_activeMap.Disable();
			_activeMap = null;
		}

		private static void Bind(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null) action.performed += handler;
		}

		private static void Unbind(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null) action.performed -= handler;
		}

		private static void BindCancel(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null) action.canceled += handler;
		}

		private static void UnbindCancel(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null) action.canceled -= handler;
		}

		private void DispatchShortcut(string actionName)
		{
			if (Graph == null) return;
			Graph.MarkDirty();
			BlueprintToolProvider.DispatchAction(Graph, this, actionName);
		}

		// ===== 快捷键树注册 =====

		private void RegisterShortcutHandlers()
		{
			var tree = MacroKey.ShortcutTreeInstance;
			RegisterAction(tree, BlueprintInputConstants.Undo);
			RegisterAction(tree, BlueprintInputConstants.Redo);
			RegisterAction(tree, BlueprintInputConstants.FocusHome);
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
	}
}

#endif
