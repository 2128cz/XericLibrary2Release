using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 层容器条目 —— 一层中挂载的所有渲染组件
	/// </summary>
	public sealed class LayerContainerEntry
	{
		/// <summary>容器根 GameObject，锚点撑满蓝图</summary>
		public GameObject Container;

		/// <summary>图元渲染器（节点矩形）</summary>
		public BlueprintPrimitiveRenderer PrimitiveRenderer;

		/// <summary>曲线渲染器（连线）</summary>
		public BlueprintCurveRenderer CurveRenderer;

		/// <summary>节点 → TMP Text 映射（挂在此容器下）</summary>
		public Dictionary<BlueprintNode, TMPro.TMP_Text> NodeTexts
			= new Dictionary<BlueprintNode, TMPro.TMP_Text>();
	}

	/// <summary>
	/// 共享层容器管理器 —— 管理桶排序中每一层的容器 GameObject。
	/// 一层一个容器，内部按 sibling 顺序挂载：
	/// 1. Image 背景（由 GridBackgroundTool 处理，挂 root 下）
	/// 2. __Curve（连线）
	/// 3. __Primitive（节点矩形）
	/// 4. TMP Text（节点文字）
	/// 各渲染工具通过此管理器共享同一组层容器。
	/// 
	/// 所有创建的 GameObject 默认 HideFlags.HideAndDontSave，
	/// 避免在编辑器→运行/运行→编辑器切换时残留场景。
	/// 调试时可通过 GraphTheoryBlueprintComponent._showDebugObjects 强制显示。
	/// </summary>
	public class LayerContainerManager
	{
		/// <summary>所有蓝图实例共享的层管理器（蓝图实例 → 层管理器）</summary>
		private static Dictionary<BlueprintGraph, LayerContainerManager> s_Instances
			= new Dictionary<BlueprintGraph, LayerContainerManager>();

		/// <summary>获取或创建指定蓝图实例的层容器管理器</summary>
		public static LayerContainerManager GetForGraph(BlueprintGraph graph, Transform renderRoot)
		{
			if (s_Instances.TryGetValue(graph, out var mgr) && mgr != null)
				return mgr;

			mgr = new LayerContainerManager(renderRoot);
			s_Instances[graph] = mgr;
			return mgr;
		}

		/// <summary>释放蓝图实例的层容器</summary>
		public static void ReleaseForGraph(BlueprintGraph graph)
		{
			if (s_Instances.TryGetValue(graph, out var mgr))
			{
				mgr.DestroyAll();
				s_Instances.Remove(graph);
			}
		}

		private readonly Transform _renderRoot;
		private readonly Dictionary<int, LayerContainerEntry> _layers
			= new Dictionary<int, LayerContainerEntry>();

		private LayerContainerManager(Transform renderRoot)
		{
			_renderRoot = renderRoot;
		}

		/// <summary>
		/// 获取指定层级的容器条目。不存在则创建。
		/// </summary>
		public LayerContainerEntry GetOrCreateLayer(int layer)
		{
			if (_layers.TryGetValue(layer, out var entry) && entry != null)
				return entry;

			// --- 创建容器 ---
			var container = new GameObject($"__BpLayer_{layer:D2}", typeof(RectTransform));
			container.hideFlags = HideFlags.HideAndDontSave;
			container.transform.SetParent(_renderRoot, false);
			var ctRt = container.GetComponent<RectTransform>();
			ctRt.anchorMin = Vector2.zero;
			ctRt.anchorMax = Vector2.one;
			ctRt.offsetMin = Vector2.zero;
			ctRt.offsetMax = Vector2.zero;

			entry = new LayerContainerEntry { Container = container };

			// --- 创建连线渲染器（底层） ---
			var curveGo = new GameObject("__Curve", typeof(RectTransform));
			curveGo.hideFlags = HideFlags.HideAndDontSave;
			curveGo.transform.SetParent(container.transform, false);
			var crRt = curveGo.GetComponent<RectTransform>();
			crRt.anchorMin = Vector2.zero;
			crRt.anchorMax = Vector2.one;
			crRt.offsetMin = Vector2.zero;
			crRt.offsetMax = Vector2.zero;
			entry.CurveRenderer = curveGo.AddComponent<BlueprintCurveRenderer>();

			// --- 创建图元渲染器（中层） ---
			var primGo = new GameObject("__Primitive", typeof(RectTransform));
			primGo.hideFlags = HideFlags.HideAndDontSave;
			primGo.transform.SetParent(container.transform, false);
			var prRt = primGo.GetComponent<RectTransform>();
			prRt.anchorMin = Vector2.zero;
			prRt.anchorMax = Vector2.one;
			prRt.offsetMin = Vector2.zero;
			prRt.offsetMax = Vector2.zero;
			entry.PrimitiveRenderer = primGo.AddComponent<BlueprintPrimitiveRenderer>();

			_layers[layer] = entry;
			ReorderAllContainers();
			return entry;
		}

		/// <summary>
		/// 尝试获取已存在的层容器（不创建）
		/// </summary>
		public LayerContainerEntry TryGetLayer(int layer)
		{
			_layers.TryGetValue(layer, out var entry);
			return entry;
		}

		/// <summary>
		/// 获取或创建节点文本（挂到指定层容器下）
		/// </summary>
		public TMPro.TMP_Text GetOrCreateNodeText(BlueprintNode node, int layer)
		{
			var entry = GetOrCreateLayer(layer);
			if (entry.NodeTexts.TryGetValue(node, out var text) && text != null)
				return text;

			var textGo = new GameObject($"__Text_{node.ElementId}", typeof(RectTransform));
			textGo.hideFlags = HideFlags.HideAndDontSave;
			textGo.transform.SetParent(entry.Container.transform, false);

			var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
			tmp.alignment = TMPro.TextAlignmentOptions.Center;
			tmp.fontSize = 12;
			tmp.enableAutoSizing = false;

			var rt = tmp.rectTransform;
			rt.pivot = new Vector2(0.5f, 0.5f);
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);

			// 放到该层最末尾（覆盖矩形之上）
			tmp.rectTransform.SetAsLastSibling();

			entry.NodeTexts[node] = tmp;
			return tmp;
		}

		/// <summary>
		/// 尝试获取已存在的节点文本对象，不存在则返回 null。
		/// </summary>
		public TMPro.TMP_Text TryGetNodeText(BlueprintNode node, int layer)
		{
			if (!_layers.TryGetValue(layer, out var entry) || entry == null) return null;
			entry.NodeTexts.TryGetValue(node, out var text);
			return text;
		}

		/// <summary>
		/// 清除指定层容器的所有渲染内容（不清除容器结构）
		/// </summary>
		public void ClearLayer(int layer)
		{
			if (!_layers.TryGetValue(layer, out var entry) || entry == null) return;

			entry.PrimitiveRenderer?.ClearAll();
			entry.CurveRenderer?.ClearAll();

			foreach (var kvp in entry.NodeTexts)
			{
				if (kvp.Value != null)
					kvp.Value.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// 清理指定层中已不存在的节点的文本对象
		/// </summary>
		public void PruneDeadTexts(int layer, HashSet<BlueprintNode> aliveNodes)
		{
			if (!_layers.TryGetValue(layer, out var entry) || entry == null) return;

			var dead = new List<BlueprintNode>();
			foreach (var kvp in entry.NodeTexts)
			{
				if (!aliveNodes.Contains(kvp.Key))
					dead.Add(kvp.Key);
			}

			foreach (var node in dead)
			{
				if (entry.NodeTexts.TryGetValue(node, out var text) && text != null)
				{
#if UNITY_EDITOR
					if (!Application.isPlaying)
						Object.DestroyImmediate(text.gameObject);
					else
#endif
						Object.Destroy(text.gameObject);
				}
				entry.NodeTexts.Remove(node);
			}
		}

		/// <summary>
		/// 移除超出活跃范围的所有层容器
		/// </summary>
		public void PruneLayersAbove(int highestActiveLayer)
		{
			var toRemove = new List<int>();
			foreach (var kvp in _layers)
			{
				if (kvp.Key > highestActiveLayer)
					toRemove.Add(kvp.Key);
			}

			foreach (int layer in toRemove)
			{
				if (_layers.TryGetValue(layer, out var entry) && entry != null)
				{
					if (entry.Container != null)
					{
#if UNITY_EDITOR
						if (!Application.isPlaying)
							Object.DestroyImmediate(entry.Container);
						else
#endif
							Object.Destroy(entry.Container);
					}
				}
				_layers.Remove(layer);
			}
		}

		/// <summary>
		/// 销毁所有层容器
		/// </summary>
		public void DestroyAll()
		{
			foreach (var kvp in _layers)
			{
				if (kvp.Value?.Container != null)
				{
#if UNITY_EDITOR
					if (!Application.isPlaying)
						Object.DestroyImmediate(kvp.Value.Container);
					else
#endif
						Object.Destroy(kvp.Value.Container);
				}
			}
			_layers.Clear();
		}

		private void ReorderAllContainers()
		{
			var sorted = new List<KeyValuePair<int, LayerContainerEntry>>(_layers);
			sorted.Sort((a, b) => a.Key.CompareTo(b.Key));
			foreach (var kvp in sorted)
				kvp.Value.Container.transform.SetAsLastSibling();
		}
	}
}
