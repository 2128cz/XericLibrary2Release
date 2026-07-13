using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using XericLibrary.Runtime.Blueprint.Canvas;
using XericLibrary.Runtime.Blueprint.Element;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 图论蓝图组件 —— 在一个 UGUI Canvas 上挂载蓝图系统。
	/// 使用 ExecuteAlways 在编辑器中也会运行，方便预览。
	/// <para>驱动方式：
	///  - 编辑器非运行态：EditorApplication.update → EditorTick
	///  - 运行时：MonoBehaviour.Update</para>
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

		[Header("Theme")]
		[Tooltip("蓝图主题名称。留空使用默认（所有 QuickGraph 工具可见）。")]
		[SerializeField] private string _themeName = string.Empty;

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
				if (_inputActions is InputActionAsset oldAsset)
					BlueprintInputManager.UnregisterAsset(oldAsset);
#endif
				_inputActions = value;
#if ENABLE_INPUT_SYSTEM
				if (value is InputActionAsset newAsset)
					BlueprintInputManager.RegisterAsset(newAsset);
#endif
			}
		}

		[Header("Tool Config Overrides")]
		[Tooltip("拖拽配置资产到此，渲染时自动按工具类型+主题匹配。")]
		[SerializeField] private List<BlueprintToolConfigBase> _toolConfigAssets = new List<BlueprintToolConfigBase>();

		[Header("Test")]
		[Tooltip("是否自动生成测试节点")]
		[SerializeField] private bool _autoGenerateTestNodes = true;

		[Header("Debug")]
		[Tooltip("显示所有隐藏的运行时对象（HideFlags），用于调试。默认关闭。")]
		[SerializeField] private bool _showDebugObjects = false;

		/// <summary>当前蓝图实例</summary>
		public BlueprintGraph Graph { get; private set; }

		/// <summary>当前画布</summary>
		public IBlueprintCanvas Canvas { get; private set; }

		public string ThemeName
		{
			get { return _themeName; }
			set
			{
				_themeName = value ?? string.Empty;
				if (Graph != null)
				{
					Graph.ThemeName = _themeName;
					Graph.RefreshTheme();
				}
			}
		}

		// ===== 初始化 =====

		private void Awake()
		{
#if UNITY_EDITOR
			// 编辑器下提前注册 tick
			if (!Application.isPlaying)
				EditorApplication.update += EditorTick;
#endif

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
				var unityCanvas = GetComponent<UnityEngine.Canvas>();
				if (unityCanvas == null)
				{
					unityCanvas = gameObject.AddComponent<UnityEngine.Canvas>();
					unityCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
				}
				if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
					gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

				Canvas = new ScreenSpaceBlueprintCanvas(unityCanvas);
			}
		}

		private void CreateGraph()
		{
			var system = BlueprintGraphSystem.GlobalInstance;
			Graph = system.CreateGraph(Canvas);
			Graph.ConfigAssets = _toolConfigAssets;

			if (!string.IsNullOrEmpty(_themeName))
			{
				Graph.ThemeName = _themeName;
				Graph.RefreshTheme();
			}

			// 立即执行一次渲染，确保背景等基础设施在首帧 OnUpdate 前就绪
			Graph.LateUpdate();
		}

		private void SetupInput()
		{
#if ENABLE_INPUT_SYSTEM
			EnsureEventSystem(_inputActions as InputActionAsset);

			if (_inputActions is InputActionAsset asset)
			{
				BlueprintInputManager.RegisterAsset(asset);
			}
#else
			EnsureEventSystem(null);
#endif
			BlueprintInputManager.SetFocusedGraph(Graph);
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
				if (eventSys.currentInputModule == null)
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

		// ===== 驱动：Update（运行时）=====

		private void Update()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif

			if (Graph == null) return;
			Graph.Update();
			Graph.LateUpdate();
			SyncDebugVisibility();
		}

#if UNITY_EDITOR
		// ===== 驱动：EditorTick（编辑器非运行态）=====

		private void EditorTick()
		{
			if (this == null || Graph == null) return;

			Graph.ConfigAssets = _toolConfigAssets;
			Graph.InvalidateToolConfigCache();

			Graph.Update();
			Graph.LateUpdate();
			SyncDebugVisibility();
		}

		private void OnValidate()
		{
			if (Graph == null || Application.isPlaying) return;
			Graph.ConfigAssets = _toolConfigAssets;
			Graph.InvalidateToolConfigCache();
		}
#endif

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
			node1.NodeSize = new Vector2(90, 130);

			var node2 = new GraphTheoryNode("Node 2", Color.green);
			node2.Position = new Vector2(startX + spacing, centerY);
			node2.NodeSize = new Vector2(90, 130);

			var node3 = new GraphTheoryNode("Node 3", Color.blue);
			node3.Position = new Vector2(startX + spacing * 2, centerY);
			node3.NodeSize = new Vector2(90, 130);

			Graph.AddNode(node1);
			Graph.AddNode(node2);
			Graph.AddNode(node3);

			Graph.TryConnectPorts(node1.OutputPorts[0], node2.InputPorts[0]);
			Graph.TryConnectPorts(node2.OutputPorts[0], node3.InputPorts[0]);

			var node4 = new GraphTheoryNode("Node 4", Color.yellow);
			node4.Position = new Vector2(startX + spacing, centerY + branchOffset);
			node4.NodeSize = new Vector2(90, 130);

			var node5 = new GraphTheoryNode("Node 5", Color.cyan);
			node5.Position = new Vector2(startX + spacing, centerY - branchOffset);
			node5.NodeSize = new Vector2(90, 130);

			Graph.AddNode(node4);
			Graph.AddNode(node5);

			Graph.TryConnectPorts(node2.OutputPorts[0], node4.InputPorts[0]);
			Graph.TryConnectPorts(node2.OutputPorts[0], node5.InputPorts[0]);
		}

		// ===== 清理 =====

		private void OnDestroy()
		{
#if UNITY_EDITOR
			EditorApplication.update -= EditorTick;
#endif
			BlueprintInputManager.SetFocusedGraph(null);

#if ENABLE_INPUT_SYSTEM
			if (_inputActions is InputActionAsset asset)
				BlueprintInputManager.UnregisterAsset(asset);
#endif

			if (Graph != null)
			{
				var system = BlueprintGraphSystem.LazyInstance;
				system?.DestroyGraph(Graph);
				Graph = null;
			}
		}
	}
}
