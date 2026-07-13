using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XericLibrary.Runtime.UIGraph;
using PrimitiveType = XericLibrary.Runtime.UIGraph.PrimitiveType;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 快速 UGUI 图论节点样式渲染工具。
	/// 通过共享的 LayerContainerManager 管理每层容器，
	/// 计算端口相对位置并更新 TMP 文本显示。
	/// 可调参数由 QuickGraphNodeRenderConfig 配置资产提供，
	/// 拖入蓝图组件即可覆盖默认值。
	/// 
	/// <para>LOD 模式（由 zoom 决定）：
	///  - LOD 0：节点纯色填充（bg=borderColor），无边框无文本
	///  - LOD 1：正常节点+边框，无文本
	///  - LOD 2：完整渲染（含文本）
	/// </para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 100)]
	[BlueprintTheme("QuickGraph")]
	public class QuickUGUIGraphNodeRenderTool : BlueprintTool
	{
		private enum LodLevel { Minimal, Simplified, Full }

		// --- 默认配置（代码内建，ScriptableObject.CreateInstance 创建） ---
		private QuickGraphNodeRenderConfig _defaultConfig;

		// --- 当前生效的配置 ---
		private QuickGraphNodeRenderConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphNodeRenderConfig external)
					return external;
				if (_defaultConfig == null)
					_defaultConfig = ScriptableObject.CreateInstance<QuickGraphNodeRenderConfig>();
				return _defaultConfig;
			}
		}

		private Transform _renderRoot;
		private LayerContainerManager _layerMgr;

		public override void OnRender()
		{
			if (Graph == null) return;
			EnsureRenderRoot();
			if (_renderRoot == null) return;

			var buckets = Graph.RenderLayers;
			if (buckets == null) return;

			_layerMgr = LayerContainerManager.GetForGraph(Graph, _renderRoot);

			var cfg = Config;

			// ── 将配置默认值写入所有节点（config → node property） ──
			foreach (var node in Graph.Nodes)
			{
				ApplyConfigToNode(node, cfg);
			}

			// 视口裁剪：只渲染画布可见范围内的节点
			var viewport = Graph.Canvas.GetViewportRect();
			float margin = cfg.NodeWidth + cfg.NodeHeight; // 扩展一点避免边缘抖动
			viewport.xMin -= margin;
			viewport.xMax += margin;
			viewport.yMin -= margin;
			viewport.yMax += margin;

			int bucketCount = buckets.BucketCount;
			var layerNodes = new List<BlueprintNode>[bucketCount];
			for (int i = 0; i < bucketCount; i++)
				layerNodes[i] = new List<BlueprintNode>();

			foreach (var node in Graph.Nodes)
			{
				int layer = node.RenderLayer;
				if (layer < 0 || layer >= bucketCount) continue;
				// 视口裁剪过滤
				if (!viewport.Contains(node.Position)) continue;
				layerNodes[layer].Add(node);
			}

			int highestActiveLayer = -1;
			for (int i = 0; i < bucketCount; i++)
			{
				if (layerNodes[i].Count > 0)
				{
					highestActiveLayer = i;
					var entry = _layerMgr.GetOrCreateLayer(i);
					RenderLayerNodes(entry, layerNodes[i], i, cfg);
				}
				else
				{
					var entry = _layerMgr.TryGetLayer(i);
					entry?.PrimitiveRenderer?.ClearAll();
					// 空层也要回收残留的 TMP 文本（被视口裁剪掉的节点）
					_layerMgr.PruneDeadTexts(i, new HashSet<BlueprintNode>());
				}
			}

			_layerMgr.PruneLayersAbove(highestActiveLayer);
		}

		/// <summary>
		/// 根据归一化缩放值计算 LOD 等级。
		/// normalizedZoom = (zoom - MinZoom) / (MaxZoom - MinZoom)，将 zoom 映射到 [0, 1]。
		/// </summary>
		private LodLevel GetLodLevel(QuickGraphNodeRenderConfig cfg)
		{
			float min = Graph.Canvas.MinZoom;
			float max = Graph.Canvas.MaxZoom;
			float range = max - min;
			float normalized = range > 0.001f ? (Graph.Canvas.ZoomLevel - min) / range : 1f;

			if (normalized < cfg.Lod0Threshold) return LodLevel.Minimal;
			if (normalized < cfg.Lod1Threshold) return LodLevel.Simplified;
			return LodLevel.Full;
		}

		private void RenderLayerNodes(LayerContainerEntry entry, List<BlueprintNode> nodes, int layer,
			QuickGraphNodeRenderConfig cfg)
		{
			if (entry.PrimitiveRenderer != null)
				entry.PrimitiveRenderer.ClearAll();

			var aliveNodes = new HashSet<BlueprintNode>(nodes);

			float zoom = Graph.Canvas.ZoomLevel;
			LodLevel lod = GetLodLevel(cfg);

			float nw = cfg.NodeWidth * zoom;
			float nh = cfg.NodeHeight * zoom;
			float border = (lod == LodLevel.Minimal) ? 0f : cfg.BorderThickness * zoom;

			foreach (var node in nodes)
			{
				node.NodeSize = new Vector2(nw, nh);
				// 端口始终用未缩放尺寸计算（连线系统的坐标基准）
				CalculatePortPositions(node, cfg.NodeWidth, cfg.NodeHeight);

				var localPos = Graph.Canvas.CanvasToLocal(node.Position);

				Color bgColor, borderColor;
				if (lod == LodLevel.Minimal)
				{
					// LOD 0：纯色块，bg = 边框色, 无边框
					bgColor = node.NodeColor;
					borderColor = node.NodeColor;
				}
				else
				{
					bgColor = node.NodeBGColor;
					borderColor = node.NodeColor;
				}

				var primEntry = new PrimitiveCacheEntry
				{
					type = PrimitiveType.Rectangle,
					sizeMode = SizeMode.InscribedEllipse,
					center = localPos,
					size = new Vector2(nw, nh),
					angle = 0f,
					@params = new PrimitiveParams
					{
						axisScaleX = 1f,
						axisScaleY = 1f,
						sideCount = 4,
						chamferSize = cfg.ChamferSize,
						chamferSegments = cfg.ChamferSegments,
						bgColor = bgColor,
						centerColor = Color.white,
						borderColor = borderColor,
						borderThickness = border,
					}
				};
				entry.PrimitiveRenderer.AddPrimitive(primEntry);

				// LOD >= Full 时才显示节点文本
				if (lod == LodLevel.Full)
					UpdateNodeText(node, layer, nw, nh, border, cfg);
				else
					HideNodeText(node, layer);
			}

			entry.PrimitiveRenderer.SetPrimitiveDirty(0);
			_layerMgr.PruneDeadTexts(layer, aliveNodes);
		}

		private static void CalculatePortPositions(BlueprintNode node, float nw, float nh)
		{
			int inCount = node.InputPorts.Count;
			int outCount = node.OutputPorts.Count;

			for (int i = 0; i < inCount; i++)
			{
				float y = nh * (0.5f - (float)(i + 1) / (inCount + 1));
				node.InputPorts[i].RelativePosition = new Vector2(-nw * 0.5f, y);
			}

			for (int i = 0; i < outCount; i++)
			{
				float y = nh * (0.5f - (float)(i + 1) / (outCount + 1));
				node.OutputPorts[i].RelativePosition = new Vector2(nw * 0.5f, y);
			}
		}

		private void UpdateNodeText(BlueprintNode node, int layer, float nw, float nh, float border,
			QuickGraphNodeRenderConfig cfg)
		{
			var tmp = _layerMgr.GetOrCreateNodeText(node, layer);
			float zoom = Graph.Canvas.ZoomLevel;

			tmp.text = node.NodeTitle;
			tmp.color = node.NodeTextColor;
			tmp.fontSize = cfg.TitleFontSize * Mathf.Max(zoom, 0.5f);
			if (cfg.TitleFont != null)
				tmp.font = cfg.TitleFont;

			var rt = tmp.rectTransform;
			rt.anchoredPosition = Graph.Canvas.CanvasToLocal(node.Position);
			rt.sizeDelta = new Vector2(nw - border * 2f, nh - border * 2f);

			rt.SetAsLastSibling();
			tmp.gameObject.SetActive(true);
		}

		/// <summary>隐藏节点的 TMP 文本（不销毁，复用）。</summary>
		private void HideNodeText(BlueprintNode node, int layer)
		{
			var tmp = _layerMgr.TryGetNodeText(node, layer);
			if (tmp != null)
				tmp.gameObject.SetActive(false);
		}

		/// <summary>
		/// 将配置默认值写入节点，使渲染工具直接读取节点值。
		/// config 只控制背景色，边框色和文本色由节点初始化时定义。
		/// </summary>
		private static void ApplyConfigToNode(BlueprintNode node, QuickGraphNodeRenderConfig cfg)
		{
			node.NodeBGColor = cfg.DefaultBackgroundColor;
		}

		private void EnsureRenderRoot()
		{
			if (_renderRoot != null) return;
			if (Graph.Canvas == null) return;
			_renderRoot = Graph.Canvas.GetRootTransform();
		}

		public override void OnDestroy()
		{
			if (Graph != null)
				LayerContainerManager.ReleaseForGraph(Graph);
			_layerMgr = null;
			_renderRoot = null;
		}
	}
}
