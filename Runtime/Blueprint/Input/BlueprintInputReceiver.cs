using UnityEngine;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入接收工具 —— 继承自 <see cref="BlueprintTool"/>，由 BlueprintToolProvider 统一管理生命周期。
	/// <para>
	/// 职责：
	/// 1. 创建并驱动 <see cref="BlueprintUGUIInputManager"/>，绑定 UGUI EventTrigger 到背景 Image；
	/// 2. 在 <see cref="OnUpdate"/> 中统一刷新鼠标位置、检测指针进入/离开背景区域；
	/// 3. 将画布内的指针移动事件分发给 <see cref="BlueprintToolProvider"/>。
	/// </para>
	/// <para>
	/// 鼠标按键按下/释放/拖拽/滚轮等由 <see cref="BlueprintUGUIInputManager"/> 通过 EventTrigger 回调直接分发。
	/// </para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 0)]
	[BlueprintTheme("QuickGraph")]
	public class BlueprintInputReceiver : BlueprintTool
	{
		private BlueprintUGUIInputManager _uguiManager;
		private bool _initialized;

		private Vector2 _lastMousePos;
		private bool _isInside;

		// ===== 生命周期 =====

		public override void OnUpdate(float deltaTime)
		{
			if (Graph == null) return;

			// ── 每帧开始：刷新帧缓存 ──
			Graph.BeginFrame();

			// ── 初始化 UGUI 管理器 ──
			if (!_initialized)
			{
				_uguiManager = new BlueprintUGUIInputManager(Graph);
				_initialized = true;
			}

			// ── 更新鼠标位置（统一入口） ──
			BlueprintUGUIInputManager.UpdateMousePosition();
			BlueprintToolProvider.MousePosition = BlueprintUGUIInputManager.MousePosition;

			// ── 延迟绑定 EventTrigger ──
			if (!_uguiManager.IsBound)
			{
				_uguiManager.TryBind();
				return;
			}

			var mp = BlueprintUGUIInputManager.MousePosition;

			// ── 检测指针进入/离开背景 ──
			bool overBg = IsScreenPointOverBackground(mp);
			if (overBg && !_isInside)
			{
				_isInside = true;
				BlueprintToolProvider.DispatchPointerEnter(Graph);
			}
			else if (!overBg && _isInside)
			{
				_isInside = false;
				BlueprintToolProvider.DispatchPointerExit(Graph);
			}

			if (!_isInside) return;

			// ── 指针移动（轮询，EventTrigger 无通用移动事件） ──
			if (Vector2.Distance(_lastMousePos, mp) < 0.001f) return;

			var cur = ScreenToCanvas(mp);
			var prev = (_lastMousePos == Vector2.zero) ? cur : ScreenToCanvas(_lastMousePos);
			var delta = cur - prev;
			_lastMousePos = mp;

			BlueprintToolProvider.DispatchPointerMove(Graph, cur, delta);
		}

		public override void OnDestroy()
		{
			if (_uguiManager != null)
			{
				_uguiManager.Unbind();
				_uguiManager = null;
			}
			_initialized = false;
			_isInside = false;
			_lastMousePos = Vector2.zero;
		}

		// ===== 辅助 =====

		/// <summary>检查屏幕坐标是否落在背景 RectTransform 内。</summary>
		private bool IsScreenPointOverBackground(Vector2 screenPoint)
		{
			var rt = _uguiManager?.BgRectTransform;
			if (rt == null) return false;
			return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPoint, null);
		}

		/// <summary>屏幕坐标 → 画布坐标。</summary>
		private Vector2 ScreenToCanvas(Vector2 screenPos)
		{
			return Graph?.Canvas != null ? Graph.Canvas.ScreenToCanvas(screenPos) : screenPos;
		}
	}
}
