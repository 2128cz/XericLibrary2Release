using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图 UGUI 输入管理器 —— 负责绑定 UGUI EventTrigger 到背景 Image，
	/// 将 UGUI 指针事件转发到 <see cref="BlueprintToolProvider"/> 的分发方法。
	/// <para>
	/// 该类仅处理与 UGUI EventTrigger 相关的绑定逻辑，不感知新旧输入系统的差异。
	/// 由 <see cref="BlueprintInputReceiver"/> 创建并在 OnUpdate 中驱动。
	/// </para>
	/// </summary>
	public class BlueprintUGUIInputManager
	{
		/// <summary>
		/// 当前帧鼠标/指针在屏幕空间的位置。
		/// 每帧由 <see cref="UpdateMousePosition"/> 刷新。
		/// </summary>
		public static Vector2 MousePosition { get; private set; }

		/// <summary>
		/// 当前帧鼠标/指针是否处于新输入系统模式。
		/// <c>true</c> = 新输入系统（<c>ENABLE_INPUT_SYSTEM</c> 已定义）；
		/// <c>false</c> = 旧输入系统。
		/// </summary>
		public static bool IsNewInputSystem
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// 更新静态鼠标位置。每帧由 BlueprintInputReceiver 的 OnUpdate 调用。
		/// 内部根据条件编译选择鼠标位置读取源。
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
		/// 内部根据条件编译选择检测方式。
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

		// ===== 实例部分 =====

		private BlueprintGraph _graph;
		private Transform _bgTransform;
		private EventTrigger _eventTrigger;
		private bool _isBound;

		// 背景 GameObject 名称（与 QuickUGUIGraphGridBackgroundTool 一致）
		private const string BackgroundGoName = "__Bp_GridBackground";

		// ===== 生命周期 =====

		/// <summary>初始化 UGUI 输入管理器并绑定到指定蓝图。</summary>
		public BlueprintUGUIInputManager(BlueprintGraph graph)
		{
			_graph = graph;
		}

		/// <summary>
		/// 尝试绑定 EventTrigger 到背景 Image。
		/// 返回 <c>true</c> 表示绑定成功，<c>false</c> 表示背景尚未创建。
		/// </summary>
		public bool TryBind()
		{
			if (_isBound) return true;
			if (_graph?.Canvas == null) return false;

			var root = _graph.Canvas.GetRootTransform();
			if (root == null) return false;

			_bgTransform = root.Find(BackgroundGoName);
			if (_bgTransform == null) return false;

			_eventTrigger = _bgTransform.gameObject.GetComponent<EventTrigger>();
			if (_eventTrigger == null)
				_eventTrigger = _bgTransform.gameObject.AddComponent<EventTrigger>();
			else
				_eventTrigger.triggers.Clear();

			BindEventTrigger();
			_isBound = true;
			return true;
		}

		/// <summary>解绑并清理 EventTrigger。</summary>
		public void Unbind()
		{
			if (_eventTrigger != null)
			{
				_eventTrigger.triggers.Clear();
				_eventTrigger = null;
			}
			_bgTransform = null;
			_isBound = false;
		}

		/// <summary>是否已绑定。</summary>
		public bool IsBound => _isBound;

		/// <summary>背景 RectTransform（用于屏幕点包含检测）。</summary>
		public RectTransform BgRectTransform => _bgTransform as RectTransform;

		// ===== EventTrigger 绑定 =====

		private void BindEventTrigger()
		{
			if (_eventTrigger == null) return;

			AddEntry(EventTriggerType.PointerEnter, OnEventPointerEnter);
			AddEntry(EventTriggerType.PointerExit,  OnEventPointerExit);
			AddEntry(EventTriggerType.PointerDown,  OnEventPointerDown);
			AddEntry(EventTriggerType.PointerUp,    OnEventPointerUp);
			AddEntry(EventTriggerType.BeginDrag,    OnEventBeginDrag);
			AddEntry(EventTriggerType.Drag,         OnEventDrag);
			AddEntry(EventTriggerType.EndDrag,      OnEventEndDrag);
			AddEntry(EventTriggerType.Scroll,       OnEventScroll);
		}

		private void AddEntry(EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
		{
			var entry = new EventTrigger.Entry { eventID = eventType };
			entry.callback.AddListener(callback);
			_eventTrigger.triggers.Add(entry);
		}

		// ===== 坐标转换 =====

		private Vector2 ScreenToCanvas(Vector2 screenPos)
		{
			return _graph?.Canvas != null ? _graph.Canvas.ScreenToCanvas(screenPos) : screenPos;
		}

		private Vector2 ScreenToCanvas(PointerEventData data)
		{
			return ScreenToCanvas(data.position);
		}

		// ===== EventTrigger 回调 =====

		private void OnEventPointerEnter(BaseEventData _)
		{
			if (_graph != null)
				BlueprintToolProvider.DispatchPointerEnter(_graph);
		}

		private void OnEventPointerExit(BaseEventData _)
		{
			if (_graph != null)
				BlueprintToolProvider.DispatchPointerExit(_graph);
		}

		private void OnEventPointerDown(BaseEventData data)
		{
			if (_graph == null || !(data is PointerEventData pData)) return;
			_graph.BeginFrame();
			var pt = ScreenToCanvas(pData);
			_graph.MarkDirty();
			BlueprintToolProvider.DispatchPointerDown(_graph, pt, (int)pData.button);
		}

		private void OnEventPointerUp(BaseEventData data)
		{
			if (_graph == null || !(data is PointerEventData pData)) return;
			var pt = ScreenToCanvas(pData);
			BlueprintToolProvider.DispatchPointerUp(_graph, pt, (int)pData.button);
		}

		// 拖拽状态
		private int _dragButton = -1;
		private Vector2 _dragStartPos;

		private void OnEventBeginDrag(BaseEventData data)
		{
			if (_graph == null || !(data is PointerEventData pData)) return;
			_dragButton = (int)pData.button;
			_dragStartPos = ScreenToCanvas(pData);
		}

		private void OnEventDrag(BaseEventData data)
		{
			if (_graph == null || !(data is PointerEventData pData)) return;
			var current = ScreenToCanvas(pData);
			var delta = current - _dragStartPos;
			_dragStartPos = current;
			BlueprintToolProvider.DispatchPointerDrag(_graph, current, delta, _dragButton);
		}

		private void OnEventEndDrag(BaseEventData data)
		{
			if (_graph == null || !(data is PointerEventData pData)) return;
			BlueprintToolProvider.DispatchPointerUp(_graph, ScreenToCanvas(pData), (int)pData.button);
			_dragButton = -1;
		}

		private void OnEventScroll(BaseEventData data)
		{
			if (_graph == null || !(data is PointerEventData pData)) return;
			_graph.BeginFrame();
			var pt = ScreenToCanvas(pData);
			var scroll = new Vector2(-pData.scrollDelta.x, -pData.scrollDelta.y);
			_graph.MarkDirty();
			BlueprintToolProvider.DispatchScroll(_graph, pt, scroll);
		}
	}
}
