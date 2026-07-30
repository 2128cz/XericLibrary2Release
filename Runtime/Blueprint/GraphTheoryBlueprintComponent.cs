using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using XericLibrary.Runtime.Blueprint.Canvas;
using XericLibrary.Runtime.Blueprint.Element;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 图论蓝图组件：Unity Inspector 配置、画布创建及调试入口。
	/// 帧驱动完全由 <see cref="BlueprintGraphSystem"/> 承担。
	/// </summary>
	[DisallowMultipleComponent]
	[ExecuteAlways]
	public class GraphTheoryBlueprintComponent : MonoBehaviour
	{
		[Header("Canvas Settings")]
		[Tooltip("勾选则为世界空间画布，否则为屏幕空间画布")]
		[SerializeField] private bool _useWorldSpaceCanvas = false;

		[Tooltip("世界空间画布的相机引用")]
		[SerializeField] private Camera _worldCamera;

		[Tooltip("屏幕空间模式实际可见区域；留空时使用蓝图宿主 RectTransform。")]
		[SerializeField] private RectTransform _viewportRectTransform;

		[Header("Blueprint Context")]
		[Tooltip("嵌入式图配置：主题与按 TargetToolType 解析的工具配置资产。")]
		[SerializeField] private BlueprintGraphContext _context = new BlueprintGraphContext();

		[Header("Input")]
		[Tooltip("新输入系统 InputActionAsset。留空则仅支持鼠标基础操作（中键拖动平移、滚轮缩放）。")]
		[SerializeField] private Object _inputActions;

		public Object InputActions
		{
			get { return _inputActions; }
			set
			{
				if (_inputActions == value) return;
#if ENABLE_INPUT_SYSTEM
				if (Graph != null && _inputActions is InputActionAsset oldAsset)
				{
					var oldTool = BlueprintToolProvider.GetToolByType<BlueprintInputTool_InputSystem>(Graph);
					if (oldTool != null && oldTool.InputActionAsset == oldAsset)
						oldTool.InputActionAsset = null;
				}
#endif
				_inputActions = value;
#if ENABLE_INPUT_SYSTEM
				if (Graph != null && value is InputActionAsset newAsset)
				{
					var newTool = BlueprintToolProvider.GetToolByType<BlueprintInputTool_InputSystem>(Graph);
					if (newTool != null)
						newTool.InputActionAsset = newAsset;
				}
#endif
			}
		}

		[Header("Test")]
		[Tooltip("是否自动生成测试节点")]
		[SerializeField] public bool _autoGenerateTestNodes = true;

		[Header("Debug")]
		[Tooltip("显示所有隐藏的运行时对象（HideFlags），用于调试。默认关闭。")]
		[SerializeField] private bool _showDebugObjects = false;

		/// <summary>当前蓝图实例</summary>
		public BlueprintGraph Graph { get; private set; }

		/// <summary>当前画布</summary>
		public IBlueprintCanvas Canvas { get; private set; }

		public BlueprintGraphContext Context => _context;
		public string ThemeName
		{
			get { return _context?.ThemeName ?? string.Empty; }
			set
			{
				if (_context == null)
					_context = new BlueprintGraphContext();
				_context.ThemeName = value ?? string.Empty;
				Graph?.RequestContextRefresh(_context);
			}
		}

		// ===== 初始化 =====

		private void Awake()
		{
			if (_context == null)
				_context = new BlueprintGraphContext();
			CreateCanvas();
			CreateGraph();
			SetupInput();
		}

		private void CreateCanvas()
		{
			if (_useWorldSpaceCanvas)
			{
				Canvas = new WorldSpaceBlueprintCanvas(transform, _worldCamera);
			}
			else
			{
				// 1) 找父级 Canvas（不在自身挂 Canvas）
				var parentCanvas = GetComponentInParent<UnityEngine.Canvas>();
				if (parentCanvas == null)
				{
					// 场景中无 Canvas → 创建全屏 Canvas，将自身移入
					var canvasGo = new GameObject("Blueprint Canvas",
						typeof(UnityEngine.Canvas),
						typeof(UnityEngine.UI.CanvasScaler),
						typeof(UnityEngine.UI.GraphicRaycaster));
					var cc = canvasGo.GetComponent<UnityEngine.Canvas>();
					cc.renderMode = RenderMode.ScreenSpaceOverlay;
					canvasGo.transform.SetParent(transform.parent, false);
					canvasGo.transform.SetAsLastSibling();
					transform.SetParent(canvasGo.transform, false);

					// 将组件自身的 RectTransform 撑满整个 Canvas，
					// 确保 __Bp_RenderRoot 的尺寸与屏幕一致，
					// 避免 GetViewportRect() 基于默认 (100x100) 尺寸错误裁剪节点。
					var selfRt = GetComponent<RectTransform>();
					if (selfRt == null)
						selfRt = gameObject.AddComponent<RectTransform>();
					selfRt.anchorMin = Vector2.zero;
					selfRt.anchorMax = Vector2.one;
					selfRt.offsetMin = Vector2.zero;
					selfRt.offsetMax = Vector2.zero;

					parentCanvas = cc;
				}

				// 2) 创建渲染根子项（所有蓝图层挂载于此，由外层的 Mask 裁剪）
				var renderRootGo = new GameObject("__Bp_RenderRoot",
					typeof(RectTransform));
				renderRootGo.hideFlags = HideFlags.DontSave;
				var renderRootRt = renderRootGo.GetComponent<RectTransform>();
				renderRootRt.SetParent(transform, false);
				renderRootRt.anchorMin = Vector2.zero;
				renderRootRt.anchorMax = Vector2.one;
				renderRootRt.offsetMin = Vector2.zero;
				renderRootRt.offsetMax = Vector2.zero;

				// 3) 以父级 Canvas + 渲染根构建画布适配器；实际视口默认是宿主自身而非 RenderRoot。
				var hostRect = GetComponent<RectTransform>();
				Canvas = new ScreenSpaceBlueprintCanvas(parentCanvas, renderRootRt,
					_viewportRectTransform != null ? _viewportRectTransform : hostRect);
			}
		}

		private void CreateGraph()
		{
			// System 是唯一帧驱动；Context 在 Initialize/工具构建之前绑定。
			Graph = BlueprintGraphSystem.GlobalInstance.CreateGraph(Canvas, _context);
		}

		private void SetupInput()
		{
#if ENABLE_INPUT_SYSTEM
			if (_inputActions is InputActionAsset asset)
			{
				EnsureEventSystem(asset);
				// 将 InputActionAsset 传递给输入工具（由工具自身管理绑定）
				var tool = BlueprintToolProvider.GetToolByType<BlueprintInputTool_InputSystem>(Graph);
				if (tool != null)
					tool.InputActionAsset = asset;
			}
#endif
		}

#if ENABLE_INPUT_SYSTEM
		private void EnsureEventSystem(InputActionAsset actionsAsset)
#else
		private void EnsureEventSystem(Object _)
#endif
		{
			var es = FindObjectOfType<EventSystem>();
			EventSystem eventSys;
			if (es != null)
			{
				eventSys = es;
			}
			else
			{
				var esGo = new GameObject("EventSystem");
				eventSys = esGo.AddComponent<EventSystem>();
			}

#if ENABLE_INPUT_SYSTEM
			if (actionsAsset != null)
			{
				var uiModule = eventSys.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
				if (uiModule == null)
				{
					var standalone = eventSys.GetComponent<StandaloneInputModule>();
					if (standalone != null)
						DestroyImmediate(standalone);

					uiModule = eventSys.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
				}
				uiModule.actionsAsset = actionsAsset;
			}
			else
#endif
			{
				if (eventSys.GetComponent<StandaloneInputModule>() == null)
				{
					eventSys.gameObject.AddComponent<StandaloneInputModule>();
				}
			}
		}

		// ===== 调试可见性 =====

		private void SyncDebugVisibility()
		{
			if (Graph == null) return;
			var targetFlags = _showDebugObjects
				? HideFlags.None | HideFlags.DontSave
				: HideFlags.HideAndDontSave;

			var root = Graph.Canvas?.GetRootTransform();
			if (root == null) return;
			for (int i = 0; i < root.childCount; i++)
			{
				var child = root.GetChild(i);
				if (child.name.StartsWith("__BpLayer_") || child.name == "__Bp_GridBackground")
					child.gameObject.hideFlags = targetFlags;
			}
		}

		// ===== 测试节点 =====

		private void Start()
		{
			if (_autoGenerateTestNodes)
				GenerateTestNodes();
		}

		// 组件仅承担 Inspector 配置与调试入口；图的 Update/LateUpdate 只由 BlueprintGraphSystem 驱动。
		private void OnValidate()
		{
			if (_context == null)
				_context = new BlueprintGraphContext();
			Graph?.RequestContextRefresh(_context);
			SyncDebugVisibility();
		}

		// ===== 测试节点生成 =====

		public void GenerateTestNodes()
		{
			if (Graph == null) return;

			if (Graph.Nodes.Count > 0)
				return;

			float startX = 200f;
			float centerY = 300f;
			float spacing = 200f;
			float branchOffset = 200f;

			var node1 = new GraphTheoryNode("Node 1", Color.red);
			node1.Position = new Vector2(startX, centerY);

			var node2 = new GraphTheoryNode("Node 2", Color.green);
			node2.Position = new Vector2(startX + spacing, centerY);

			var node3 = new GraphTheoryNode("Node 3", Color.blue);
			node3.Position = new Vector2(startX + spacing * 2, centerY);

			Graph.AddNode(node1);
			Graph.AddNode(node2);
			Graph.AddNode(node3);

			Graph.TryConnectPorts(node1.OutputPorts[0], node2.InputPorts[0]);
			Graph.TryConnectPorts(node2.OutputPorts[0], node3.InputPorts[0]);

			var node4 = new GraphTheoryNode("Node 4", Color.yellow);
			node4.Position = new Vector2(startX + spacing * 2, centerY + branchOffset);

			var node5 = new GraphTheoryNode("Node 5", Color.cyan);
			node5.Position = new Vector2(startX + spacing * 2, centerY - branchOffset);

			Graph.AddNode(node4);
			Graph.AddNode(node5);

			Graph.TryConnectPorts(node2.OutputPorts[0], node4.InputPorts[0]);
			Graph.TryConnectPorts(node2.OutputPorts[0], node5.InputPorts[0]);
		}

		// ===== 清理 =====

		private void OnDestroy()
		{
			if (Graph != null)
			{
				var system = BlueprintGraphSystem.LazyInstance;
				system?.DestroyGraph(Graph);
				Graph = null;
			}
		}
	}
}
