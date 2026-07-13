#if ENABLE_INPUT_SYSTEM

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入管理器（新输入系统）—— 管理 InputActionAsset 的引用计数与启用/停用。
	/// <para>
	/// 多个蓝图可能引用同一个 InputActionAsset，管理器确保：
	/// - 首个蓝图注册时启用 asset 并绑定 Action 回调；
	/// - 后续蓝图注册仅增加计数，不重复启用；
	/// - 蓝图注销时减少计数，计数归零时才解绑并禁用 asset。
	/// </para>
	/// <para>
	/// 蓝图组件在 Awake 时调用 <see cref="RegisterAsset"/>，OnDestroy 时调用
	/// <see cref="UnregisterAsset"/>；运行时动态更换 asset 也通过这两方法。
	/// </para>
	/// </summary>
	public static class BlueprintInputManager
	{
		/// <summary>asset → 引用计数</summary>
		private static Dictionary<InputActionAsset, int> _refCounts
			= new Dictionary<InputActionAsset, int>();

		/// <summary>asset → 已绑定回调的 ActionMap</summary>
		private static Dictionary<InputActionAsset, InputActionMap> _activeMaps
			= new Dictionary<InputActionAsset, InputActionMap>();

		// ===== 快捷键最新值（供 QuickGraphInputTool.OnUpdate 轮询） =====

		/// <summary>Pan 动作最新的 Vector2 方向值（在 performed 中写入，canceled 中归零）。</summary>
		public static Vector2 LastPanDirection;

		/// <summary>Zoom 动作最新的 float 轴值（在 performed 中写入，canceled 中归零）。</summary>
		public static float LastZoomAxis;

		// 缓存的回调引用（确保 Bind/Unbind 使用同一委托实例）
		private static readonly System.Action<InputAction.CallbackContext> s_panHandler
			= ctx => LastPanDirection = ctx.ReadValue<Vector2>();
		private static readonly System.Action<InputAction.CallbackContext> s_panCanceled
			= _ => LastPanDirection = Vector2.zero;
		private static readonly System.Action<InputAction.CallbackContext> s_zoomHandler
			= ctx => LastZoomAxis = ctx.ReadValue<float>();
		private static readonly System.Action<InputAction.CallbackContext> s_zoomCanceled
			= _ => LastZoomAxis = 0f;
		private static readonly System.Action<InputAction.CallbackContext> s_homeHandler
			= _ => OnActionReceived(BlueprintInputConstants.FocusHome);
		private static readonly System.Action<InputAction.CallbackContext> s_undoHandler
			= _ => OnActionReceived(BlueprintInputConstants.Undo);
		private static readonly System.Action<InputAction.CallbackContext> s_redoHandler
			= _ => OnActionReceived(BlueprintInputConstants.Redo);
		private static readonly System.Action<InputAction.CallbackContext> s_parentHandler
			= _ => OnActionReceived(BlueprintInputConstants.NavigateParent);

		// ===== 当前焦点蓝图（快捷键分发目标） =====

		private static BlueprintGraph s_focusedGraph;

		/// <summary>设置当前焦点蓝图（快捷键分发目标）。</summary>
		public static void SetFocusedGraph(BlueprintGraph graph)
		{
			s_focusedGraph = graph;
		}

		// ===== 注册 / 注销 =====

		/// <summary>
		/// 注册一个蓝图对 InputActionAsset 的引用。
		/// 若 asset 尚未启用，则启用并绑定快捷键回调。
		/// 允许多个蓝图共享同一 asset。
		/// </summary>
		public static void RegisterAsset(InputActionAsset asset)
		{
			if (asset == null) return;

			if (_refCounts.TryGetValue(asset, out int count))
			{
				_refCounts[asset] = count + 1;
				return;
			}

			_refCounts[asset] = 1;

			var map = asset.FindActionMap(BlueprintInputConstants.MapName);
			if (map != null)
			{
				map.Enable();
				BindActions(map);
				_activeMaps[asset] = map;
			}
		}

		/// <summary>
		/// 注销一个蓝图对 InputActionAsset 的引用。
		/// 引用计数归零时解绑回调并禁用 asset。
		/// </summary>
		public static void UnregisterAsset(InputActionAsset asset)
		{
			if (asset == null) return;

			if (!_refCounts.TryGetValue(asset, out int count)) return;

			count--;
			if (count > 0)
			{
				_refCounts[asset] = count;
				return;
			}

			_refCounts.Remove(asset);

			if (_activeMaps.TryGetValue(asset, out var map))
			{
				UnbindActions(map);
				map.Disable();
				_activeMaps.Remove(asset);
			}
		}

		// ===== Action 回调 =====

		private static void BindActions(InputActionMap map)
		{
			Bind(map, BlueprintInputConstants.Pan,      s_panHandler);
			BindCancelled(map, BlueprintInputConstants.Pan, s_panCanceled);
			Bind(map, BlueprintInputConstants.Zoom,     s_zoomHandler);
			BindCancelled(map, BlueprintInputConstants.Zoom, s_zoomCanceled);
			Bind(map, BlueprintInputConstants.FocusHome,s_homeHandler);
			Bind(map, BlueprintInputConstants.Undo,     s_undoHandler);
			Bind(map, BlueprintInputConstants.Redo,     s_redoHandler);
			Bind(map, BlueprintInputConstants.NavigateParent, s_parentHandler);
		}

		private static void UnbindActions(InputActionMap map)
		{
			Unbind(map, BlueprintInputConstants.Pan,      s_panHandler);
			UnbindCancelled(map, BlueprintInputConstants.Pan, s_panCanceled);
			Unbind(map, BlueprintInputConstants.Zoom,     s_zoomHandler);
			UnbindCancelled(map, BlueprintInputConstants.Zoom, s_zoomCanceled);
			Unbind(map, BlueprintInputConstants.FocusHome,s_homeHandler);
			Unbind(map, BlueprintInputConstants.Undo,     s_undoHandler);
			Unbind(map, BlueprintInputConstants.Redo,     s_redoHandler);
			Unbind(map, BlueprintInputConstants.NavigateParent, s_parentHandler);
		}

		private static void Bind(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null)
				action.performed += handler;
		}

		private static void Unbind(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null)
				action.performed -= handler;
		}

		private static void BindCancelled(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null)
				action.canceled += handler;
		}

		private static void UnbindCancelled(InputActionMap map, string name, System.Action<InputAction.CallbackContext> handler)
		{
			var action = map.FindAction(name);
			if (action != null)
				action.canceled -= handler;
		}

		private static void OnActionReceived(string actionName)
		{
			if (s_focusedGraph != null)
			{
				s_focusedGraph.MarkDirty();
				BlueprintToolProvider.DispatchAction(s_focusedGraph, actionName);
			}
		}
	}
}

#endif
