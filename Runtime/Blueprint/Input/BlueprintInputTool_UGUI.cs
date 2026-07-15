using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图输入工具基类（UGUI）—— 注册 EventTrigger 到蓝图背景上监听鼠标事件，
	/// 并将事件发布到 <see cref="BlueprintToolProvider"/>，同时记录光标上下文。
	/// <para>
	/// 注意：此基类本身不被注册为独立工具（无 <c>[BlueprintTool]</c> 属性）。
	/// 由子类 <c>BlueprintInputTool_InputSystem</c> 和 <c>BlueprintInputTool_InputManager</c>
	/// 通过继承获得 UGUI 绑定能力。
	/// </para>
	/// </summary>
	public class BlueprintInputTool_UGUI : GraphInputTool
	{
		// ===== EventTrigger 绑定 =====

		private Transform _bgTransform;
		private EventTrigger _eventTrigger;
		private RectTransform _bgRectTransform;

		private const string BackgroundGoName = "__Bp_GridBackground";

		// 上一帧的鼠标画布坐标（用于计算 PointerMove delta）
		private Vector2 _lastCanvasPos;

		/// <summary>
		/// 是否通过 EventTrigger 处理鼠标按键/拖拽/滚轮事件。
		/// 新输入系统路径下为 true（默认），旧输入系统路径下为 false（由 PollUpdate 轮询）。
		/// 子类可在 <see cref="OnInitialize"/> 中修改此值。
		/// </summary>
		protected bool UseEventTriggerForMouse { get; set; } = true;

		// ===== 生命周期 =====

		public override void OnInitialize()
		{
			TryBindEventTrigger();
		}

		public override void OnUpdate(float deltaTime)
		{
			if (Graph == null) return;

			// 检查底层的 Unity Canvas 组件是否仍存活
			// Graph?.Canvas 是 C# 接口引用（非 Unity Object），不适用 Unity 的假空检测
			if (Graph.Canvas == null || !Graph.Canvas.IsAlive)
				return;

			Graph.BeginFrame();

			BlueprintUGUIInputManager.UpdateMousePosition();
			BlueprintToolProvider.MousePosition = BlueprintUGUIInputManager.MousePosition;

			// 每帧检查 EventTrigger 是否有效（应对 Editor 重收集工具场景）
			if (_eventTrigger == null || !IsEventTriggerValid())
			{
				_eventTrigger = null; // 强制重新绑定
				TryBindEventTrigger();
				return;
			}

			var canvasPt = Graph.Canvas != null
				? Graph.Canvas.ScreenToCanvas(BlueprintUGUIInputManager.MousePosition)
				: Vector2.zero;
			UpdateCursorContext(canvasPt);

			// ── 每帧分发 PointerMove ──
			var delta = canvasPt - _lastCanvasPos;
			if (delta.sqrMagnitude > 0.0001f || _lastCanvasPos == Vector2.zero)
			{
				BlueprintToolProvider.DispatchPointerMove(Graph, this, canvasPt, delta);
			}
			_lastCanvasPos = canvasPt;
		}

		/// <summary>
		/// 检查已绑定的 EventTrigger 实例是否仍然有效（回调仍指向本实例）。
		/// </summary>
		private bool IsEventTriggerValid()
		{
			if (_eventTrigger == null) return false;
			var triggersList = _eventTrigger.triggers;
			if (triggersList == null || triggersList.Count == 0) return false;
			var callbacks = triggersList[0].callback;
			if (callbacks == null) return false;
			int persistentCount = callbacks.GetPersistentEventCount();
			if (persistentCount > 0)
			{
				var target = callbacks.GetPersistentTarget(0);
				return target != null && target.Equals(this);
			}
			return true;
		}

		public override void OnDestroy()
		{
			UnbindEventTrigger();
		}

		/// <summary>
		/// 确保场景中存在 EventSystem 和合适的 InputModule。
		/// 旧输入系统 → StandaloneInputModule。
		/// 新输入系统 → InputSystemUIInputModule（由 GraphTheoryBlueprintComponent 创建）。
		/// 不会重复创建模块（通过 GetComponent 检查）。
		/// </summary>
		private static void EnsureEventSystemExists()
		{
			var es = Object.FindObjectOfType<EventSystem>();
			if (es == null)
			{
				var go = new GameObject("EventSystem");
				es = go.AddComponent<EventSystem>();
			}

#if !ENABLE_INPUT_SYSTEM
			if (es.GetComponent<StandaloneInputModule>() == null)
			{
				es.gameObject.AddComponent<StandaloneInputModule>();
			}
#endif
		}

		// ===== EventTrigger 绑定/解绑 =====

		private void TryBindEventTrigger()
		{
			if (_eventTrigger != null) return;
			if (Graph?.Canvas == null || !Graph.Canvas.IsAlive) return;

			var root = Graph.Canvas.GetRootTransform();
			if (root == null) return;

			_bgTransform = root.Find(BackgroundGoName);
			if (_bgTransform == null) return;

			_bgRectTransform = _bgTransform as RectTransform;

			// ── 自动创建 EventSystem（含合适 InputModule）──
			EnsureEventSystemExists();

			_eventTrigger = _bgTransform.gameObject.GetComponent<EventTrigger>();
			if (_eventTrigger == null)
			{
				_eventTrigger = _bgTransform.gameObject.AddComponent<EventTrigger>();
			}
			else
				_eventTrigger.triggers.Clear();

			BindAllEntries();
		}

		private void UnbindEventTrigger()
		{
			if (_eventTrigger != null)
			{
				_eventTrigger.triggers.Clear();
				_eventTrigger = null;
			}
			_bgTransform = null;
			_bgRectTransform = null;
		}

		private void BindAllEntries()
		{
			if (_eventTrigger == null) return;

			// ── 始终绑定的基础回调（指针进入/离开） ──
			AddEntry(EventTriggerType.PointerEnter,    OnPointerEnterEvent);
			AddEntry(EventTriggerType.PointerExit,     OnPointerExitEvent);

			// ── 鼠标按键/拖拽/滚轮（仅在 UseEventTriggerForMouse 时绑定） ──
			if (UseEventTriggerForMouse)
			{
				AddEntry(EventTriggerType.PointerDown,     OnPointerDownEvent);
				AddEntry(EventTriggerType.PointerUp,       OnPointerUpEvent);
				AddEntry(EventTriggerType.BeginDrag,       OnBeginDragEvent);
				AddEntry(EventTriggerType.Drag,            OnDragEvent);
				AddEntry(EventTriggerType.EndDrag,         OnEndDragEvent);
				AddEntry(EventTriggerType.Scroll,          OnScrollEvent);
			}
		}

		private void AddEntry(EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
		{
			var entry = new EventTrigger.Entry { eventID = type };
			entry.callback.AddListener(callback);
			_eventTrigger.triggers.Add(entry);
		}

		// ===== 坐标转换 =====

		private Vector2 ScreenToCanvas(Vector2 screenPos)
		{
			return Graph?.Canvas != null ? Graph.Canvas.ScreenToCanvas(screenPos) : screenPos;
		}

		private Vector2 PointerCanvasPos(PointerEventData data)
		{
			return ScreenToCanvas(data.position);
		}

		// ===== EventTrigger 回调（以 self 作为 sender 派发） =====

		private void OnPointerEnterEvent(BaseEventData _)
		{
			BlueprintToolProvider.DispatchPointerEnter(Graph, this);
		}

		private void OnPointerExitEvent(BaseEventData _)
		{
			BlueprintToolProvider.DispatchPointerExit(Graph, this);
		}

		private void OnPointerDownEvent(BaseEventData data)
		{
			if (!(data is PointerEventData pData)) return;
			var pt = PointerCanvasPos(pData);
			Graph?.MarkDirty();
			UpdateCursorContext(pt);
			BlueprintToolProvider.DispatchPointerDown(Graph, this, pt, (int)pData.button);
		}

		private void OnPointerUpEvent(BaseEventData data)
		{
			if (!(data is PointerEventData pData)) return;
			var pt = PointerCanvasPos(pData);
			UpdateCursorContext(pt);

			// 有拖拽的释放由 OnEndDragEvent 处理，这里只响应无拖拽的点击释放
			if (!_hadDragThisClick)
			{
				bool isEmptyClick = Graph?.HitTest(pt) == null;
				BlueprintToolProvider.DispatchPointerUp(Graph, this, pt, (int)pData.button, isEmptyClick);
			}
		}

		private Vector2 _dragStartPos;
		private bool _hadDragThisClick;

		private void OnBeginDragEvent(BaseEventData data)
		{
			if (!(data is PointerEventData pData)) return;
			_dragStartPos = PointerCanvasPos(pData);
			_hadDragThisClick = true;
		}

		private void OnDragEvent(BaseEventData data)
		{
			if (!(data is PointerEventData pData)) return;
			var current = PointerCanvasPos(pData);
			var delta = current - _dragStartPos;
			_dragStartPos = current;
			UpdateCursorContext(current);
			BlueprintToolProvider.DispatchPointerDrag(Graph, this, current, delta, (int)pData.button);
		}

		private void OnEndDragEvent(BaseEventData data)
		{
			if (!(data is PointerEventData pData)) return;
			var pt = PointerCanvasPos(pData);
			UpdateCursorContext(pt);
			BlueprintToolProvider.DispatchPointerUp(Graph, this, pt, (int)pData.button);
		}

		private void OnScrollEvent(BaseEventData data)
		{
			if (!(data is PointerEventData pData)) return;
			var pt = PointerCanvasPos(pData);
			var scroll = new Vector2(-pData.scrollDelta.x, -pData.scrollDelta.y);
			UpdateCursorContext(pt);
			Graph?.MarkDirty();
			BlueprintToolProvider.DispatchScroll(Graph, this, pt, scroll);
		}
	}
}
