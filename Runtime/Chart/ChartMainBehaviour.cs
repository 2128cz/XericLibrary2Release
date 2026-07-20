using UnityEngine;

namespace XericLibrary.Runtime.Chart
{
    /// <summary>
    /// 图表主行为组件。标记 [ExecuteAlways] 确保编辑器下生命周期正常执行。
    /// </summary>
    [AddComponentMenu("Xeric/Chart/Chart Main")]
    [ExecuteAlways]
    public class ChartMainBehaviour : MonoBehaviour
    {
        [SerializeField] [Tooltip("图表数据模型")]
        private ChartDataModel m_DataModel;

        public ChartDataModel DataModel
        {
            get => m_DataModel;
            set => m_DataModel = value;
        }

        [Header("工具管理")]
        [SerializeField] private bool m_ShowDebug;
        [SerializeField] private bool m_HideAllTools;

        public ChartToolsManager ToolManager { get; private set; }
        public Transform ToolsRoot { get; private set; }

        private bool m_IsInitialized;

        // ── 编辑器/编辑器通用 hideFlags ──────────────────────────────

        /// <summary>编辑器下不隐藏，允许在 Hierarchy 查看和调试</summary>
        private const HideFlags k_EditorFlags = HideFlags.DontSaveInEditor;

        /// <summary>运行时隐藏且不保存</summary>
        private const HideFlags k_PlayFlags = HideFlags.HideAndDontSave;

        internal static HideFlags CurrentFlags =>
            Application.isPlaying ? k_PlayFlags : k_EditorFlags;

        // ── 生命周期 ──────────────────────────────────────────────

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            // ExecuteAlways 模式下 OnEnable 在编辑器全程可用
            if (!m_IsInitialized) Initialize();
        }

        private void Initialize()
        {
            if (m_IsInitialized) return;
            if (ToolManager != null) return;

            var flags = CurrentFlags;

            var toolsObj = new GameObject("[ChartTools]");
            toolsObj.transform.SetParent(transform, false);
            toolsObj.hideFlags = flags;
            ToolsRoot = toolsObj.transform;

            ToolManager = new ChartToolsManager
            {
                ToolsRoot = ToolsRoot,
                ShowDebug = m_ShowDebug,
                HideAllTools = m_HideAllTools,
            };

            m_IsInitialized = true;

            if (!Application.isPlaying)
                Debug.Log("[ChartMain] 编辑器模式初始化完成", this);
        }

        private void Update()
        {
            if (!m_IsInitialized) return;
            if (ToolManager == null) return;
            ToolManager.ShowDebug = m_ShowDebug;
            ToolManager.HideAllTools = m_HideAllTools;
            ToolManager.UpdateAll();
        }

        private void LateUpdate()
        {
            if (!m_IsInitialized) return;
            ToolManager?.RenderAll();
        }

        // ── 工具管理 ──────────────────────────────────────────────

        public T AddTool<T>(string toolName, int orderIndex = 0)
            where T : ChartToolBase, new()
        {
            if (ToolManager == null) Initialize();
            return ToolManager.AddTool<T>(toolName, orderIndex);
        }

        public bool RemoveTool(string toolName)
        {
            if (ToolManager == null) return false;
            return ToolManager.RemoveToolByName(toolName);
        }

        public void RemoveAllTools() => ToolManager?.RemoveAllTools();

        // ── 清理 ──────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (ToolManager != null)
            {
                ToolManager.Dispose();
                ToolManager = null;
            }
            if (ToolsRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(ToolsRoot.gameObject);
                else
                    DestroyImmediate(ToolsRoot.gameObject);
                ToolsRoot = null;
            }
            m_IsInitialized = false;
        }

        private void OnValidate()
        {
            if (ToolManager != null)
            {
                ToolManager.ShowDebug = m_ShowDebug;
                ToolManager.HideAllTools = m_HideAllTools;
            }
        }
    }
}
