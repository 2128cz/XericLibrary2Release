using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XericLibrary.Runtime.UIGraph;
using PrimitiveType = XericLibrary.Runtime.UIGraph.PrimitiveType;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 节点渲染工具 — 按网格区块管理 BlueprintPrimitiveRenderer，支持 Viewport Transform。
	/// <para>每个 Chunk 一个 PrimitiveRenderer + 每个节点一个 TMP Text（挂在该 Chunk 下）。</para>
	/// LOD 逻辑保留自 QuickUGUIGraphNodeRenderTool。
	/// </summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 100)]
	[BlueprintTheme("QuickGraph")]
	public class NodeCanvasRenderTool : BlueprintCanvasRenderTool<BlueprintPrimitiveRenderer>
	{
		private enum LodLevel { Minimal, Simplified, Full }

		private QuickGraphNodeRenderConfig _defaultConfig;
		private QuickGraphNodeRenderConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphNodeRenderConfig external) return external;
				if (_defaultConfig == null)
					_defaultConfig = ScriptableObject.CreateInstance<QuickGraphNodeRenderConfig>();
				return _defaultConfig;
			}
		}

		// TMP 文本：节点 → (ChunkID, Text 组件)
		private Dictionary<string, (ulong chunkID, TMP_Text text)> _nodeTexts
			= new Dictionary<string, (ulong, TMP_Text)>();

		// ====================================================================
		// 抽象实现
		// ====================================================================

		protected override BlueprintPrimitiveRenderer BuildChunkRenderer(GameObject chunkGo)
		{
			return chunkGo.AddComponent<BlueprintPrimitiveRenderer>();
		}

		protected override void AppendElementData(BlueprintPrimitiveRenderer renderer, IBlueprintElement element)
		{
			if (!(element is BlueprintNode node)) return;

			var cfg = Config;
			float zoom = Graph.Canvas.ZoomLevel;
			LodLevel lod = GetLodLevel();

			// 端口位置始终用原始尺寸计算
			CalculatePortPositions(node, cfg.NodeWidth, cfg.NodeHeight);

			// 节点尺寸和位置（画布空间，不转换，由 Viewport Transform 统一变换）
			float nw = cfg.NodeWidth;
			float nh = cfg.NodeHeight;
			float border = (lod == LodLevel.Minimal) ? 0f : cfg.BorderThickness;
			Vector2 pos = node.Position;

			Color bgColor, borderColor;
			if (lod == LodLevel.Minimal)
			{
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
				center = pos,
				size = new Vector2(nw, nh),
				angle = 0f,
				@params = new PrimitiveParams
				{
					axisScaleX = 1f,
					axisScaleY = 1f,
					sideCount = 4,
					chamferSize = cfg.ChamferSize,
					chamferSegments = cfg.ChamferSegments,
					bgColor = new Color32(0, 0, 0, 0),
					centerColor = bgColor,
					borderColor = borderColor,
					borderThickness = border,
				}
			};
			renderer.AddPrimitive(primEntry);

			// LOD Full 时显示文本
			if (lod == LodLevel.Full)
				UpdateNodeText(node, pos, nw, nh, border, cfg);
			else
				HideNodeText(node);
		}

		protected override void ClearChunkData(BlueprintPrimitiveRenderer renderer)
		{
			renderer.ClearAll();
		}

		protected override void SetChunkDirty(BlueprintPrimitiveRenderer renderer)
		{
			renderer.SetPrimitiveDirty(0);
		}

		protected override Vector2 GetElementPosition(IBlueprintElement element)
		{
			return element is BlueprintNode node ? node.Position : Vector2.zero;
		}

		protected override bool IsElementCulled(IBlueprintElement element)
		{
			if (!(element is BlueprintNode node)) return true;
			var viewport = Graph?.Canvas?.GetViewportRect() ?? new Rect();
			float margin = Config.NodeWidth + Config.NodeHeight;
			viewport.xMin -= margin;
			viewport.xMax += margin;
			viewport.yMin -= margin;
			viewport.yMax += margin;
			return !viewport.Contains(node.Position);
		}

		// ====================================================================
		// LOD
		// ====================================================================

		private LodLevel GetLodLevel()
		{
			var cfg = Config;
			float min = Graph.Canvas.MinZoom;
			float max = Graph.Canvas.MaxZoom;
			float range = max - min;
			float normalized = range > 0.001f ? (Graph.Canvas.ZoomLevel - min) / range : 1f;

			if (normalized < cfg.Lod0Threshold) return LodLevel.Minimal;
			if (normalized < cfg.Lod1Threshold) return LodLevel.Simplified;
			return LodLevel.Full;
		}

		// ====================================================================
		// 端口位置（和旧渲染工具一致）
		// ====================================================================

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

		// ====================================================================
		// TMP 文本 — 挂到节点所属的 Chunk 下
		// ====================================================================

		private void UpdateNodeText(BlueprintNode node, Vector2 pos, float nw, float nh,
			float border, QuickGraphNodeRenderConfig cfg)
		{
			var (cx, cy) = PositionToCoord(pos);
			ulong chunkID = CoordToID(cx, cy);

			if (_nodeTexts.TryGetValue(node.ElementId, out var existing))
			{
				// 节点已换 Chunk → 清理旧文本
				if (existing.chunkID != chunkID)
				{
					Object.Destroy(existing.text.gameObject);
					_nodeTexts.Remove(node.ElementId);
				}
				else
				{
					// 复用现有文本
					var tmp = existing.text;
					tmp.text = node.NodeTitle;
					tmp.color = node.NodeTextColor;
					tmp.fontSize = cfg.TitleFontSize;
					if (cfg.TitleFont != null) tmp.font = cfg.TitleFont;
					var rt = tmp.rectTransform;
					rt.anchorMin = Vector2.zero;
					rt.anchorMax = Vector2.zero;
					rt.anchoredPosition = new Vector2(pos.x - cx * ChunkSize, pos.y - cy * ChunkSize);
					rt.sizeDelta = new Vector2(nw - border * 2f, nh - border * 2f);
					rt.SetAsLastSibling();
					tmp.gameObject.SetActive(true);
					return;
				}
			}

			// 创建新文本
			var chunkGo = GetOrCreateChunk(chunkID).gameObject;
			var textGo = new GameObject($"__Text_{node.ElementId}", typeof(RectTransform));
			textGo.hideFlags = HideFlags.HideAndDontSave;
			textGo.transform.SetParent(chunkGo.transform, false);

			var tmp2 = textGo.AddComponent<TextMeshProUGUI>();
			tmp2.alignment = TextAlignmentOptions.Center;
			tmp2.fontSize = cfg.TitleFontSize;
			tmp2.enableAutoSizing = false;
			tmp2.text = node.NodeTitle;
			tmp2.color = node.NodeTextColor;
			if (cfg.TitleFont != null) tmp2.font = cfg.TitleFont;

			var rt2 = tmp2.rectTransform;
			rt2.pivot = new Vector2(0.5f, 0.5f);
			rt2.anchorMin = Vector2.zero;                    // 相对 Chunk 左下角
			rt2.anchorMax = Vector2.zero;
			// Chunk 原点 = 左下角(cx*size, cy*size)，文本相对 Chunk 原点偏移
			rt2.anchoredPosition = new Vector2(pos.x - cx * ChunkSize, pos.y - cy * ChunkSize);
			rt2.sizeDelta = new Vector2(nw - border * 2f, nh - border * 2f);
			rt2.SetAsLastSibling();

			_nodeTexts[node.ElementId] = (chunkID, tmp2);
		}

		private void HideNodeText(BlueprintNode node)
		{
			if (_nodeTexts.TryGetValue(node.ElementId, out var existing))
			{
				existing.text.gameObject.SetActive(false);
			}
		}

		// ====================================================================
		// 清理（重写基类，额外清理 TMP 文本）
		// ====================================================================

		protected override void CleanupInactiveChunks(
			Dictionary<ulong, List<IBlueprintElement>>.KeyCollection activeIDs)
		{
			// 清理空 Chunk 中的文本
			var deadNodes = new List<string>();
			foreach (var kvp in _nodeTexts)
			{
				ulong chunkID = kvp.Value.chunkID;
				if (!System.Linq.Enumerable.Contains(activeIDs, chunkID))
					deadNodes.Add(kvp.Key);
			}
			foreach (var id in deadNodes)
			{
				if (_nodeTexts.TryGetValue(id, out var entry))
				{
					Object.Destroy(entry.text.gameObject);
					_nodeTexts.Remove(id);
				}
			}

			base.CleanupInactiveChunks(activeIDs);
		}

		protected override void DestroyAllChunks()
		{
			foreach (var kvp in _nodeTexts)
			{
				if (kvp.Value.text != null)
					Object.Destroy(kvp.Value.text.gameObject);
			}
			_nodeTexts.Clear();

			base.DestroyAllChunks();
		}
	}
}
