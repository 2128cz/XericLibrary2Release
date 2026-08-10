using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XericLibrary.Runtime.Blueprint.Render;
using XericLibrary.Runtime.UIGraph;
using PrimitiveType = XericLibrary.Runtime.UIGraph.PrimitiveType;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	[BlueprintTool(phase: ToolPhase.Render, order: 100)] [BlueprintTheme("QuickGraph")]
	public class NodeCanvasRenderTool : BlueprintCanvasRenderTool<BlueprintPrimitiveRenderer>
	{
		private QuickGraphRenderStyleResolver _styles;
		private readonly Dictionary<string, (ulong chunkID, TMP_Text text)> _nodeTexts = new Dictionary<string, (ulong, TMP_Text)>();
		private readonly HashSet<string> _staleTextCandidates = new HashSet<string>();

		public override void OnInitialize()
		{
			base.OnInitialize();
			_styles = new QuickGraphRenderStyleResolver(Graph);
			_styles.Changed += _ => SyncAllNodeGeometry();
			SyncAllNodeGeometry();
		}

		public override void OnConfigChanged()
		{
			_styles?.Refresh();
			base.OnConfigChanged();
		}

		protected override BlueprintPrimitiveRenderer BuildChunkRenderer(GameObject go) => go.AddComponent<BlueprintPrimitiveRenderer>();
		protected override void ClearChunkData(BlueprintPrimitiveRenderer renderer) => renderer.ClearAll();
		protected override void SetChunkDirty(BlueprintPrimitiveRenderer renderer) => renderer.SetPrimitiveDirty(0);
		protected override bool AcceptsElement(IBlueprintElement element) => element is BlueprintNode;

		protected override void AppendElementData(BlueprintPrimitiveRenderer renderer, IBlueprintElement element, ulong chunkID)
		{
			if (!(element is BlueprintNode node))
				return;

			var graphStyle = _styles.Snapshot;
			var nodeStyle = graphStyle.Node;
			var lod = graphStyle.GetLod(CurrentElementSubmission.LOD);
			CalculatePortPositions(node, nodeStyle.Width, nodeStyle.Height, graphStyle.Port);

			float border = lod.NodeBorder;
			Color fillColor = node.GetStyleValue<Color>(QuickGraphStylePaths.NodeFillColor);
			Color borderColor = node.GetStyleValue<Color>(QuickGraphStylePaths.NodeBorderColor);
			Color titleColor = node.GetStyleValue<Color>(QuickGraphStylePaths.NodeTitleColor);
			Color centerColor = lod.UseNodeColorAsBackground ? borderColor : fillColor;

			renderer.AddPrimitive(new PrimitiveCacheEntry
			{
				type = PrimitiveType.Rectangle,
				sizeMode = SizeMode.InscribedEllipse,
				center = CurrentElementSubmission.ChunkLocalPosition,
				size = new Vector2(nodeStyle.Width, nodeStyle.Height),
				@params = new PrimitiveParams
				{
					axisScaleX = nodeStyle.AxisScaleX,
					axisScaleY = nodeStyle.AxisScaleY,
					sideCount = nodeStyle.SideCount,
					chamferSize = nodeStyle.Chamfer,
					chamferSegments = nodeStyle.ChamferSegments,
					bgColor = nodeStyle.OuterColor,
					centerColor = centerColor,
					borderColor = borderColor,
					borderThickness = border
				}
			});

			if (lod.ShowTitle && (CurrentElementSubmission.LOD != BlueprintLodLevel.Full || nodeStyle.ShowTitleFull))
				UpdateNodeText(node, chunkID, CurrentElementSubmission.ChunkLocalPosition, nodeStyle, border, titleColor);
			else
				HideNodeText(node.ElementId);
		}

		private void SyncAllNodeGeometry()
		{
			if (Graph == null || _styles == null)
				return;

			var style = _styles.Snapshot;
			foreach (var node in Graph.Nodes)
			{
				var old = node.NodeSize;
				CalculatePortPositions(node, style.Node.Width, style.Node.Height, style.Port);
				if (old != node.NodeSize)
					Graph.UpdateNodeGeometry(node);
			}
		}

		private static void CalculatePortPositions(BlueprintNode node, float width, float height, QuickGraphPortRenderStyle style)
		{
			node.NodeSize = new Vector2(width, height);
			for (int i = 0; i < node.InputPorts.Count; i++)
				node.InputPorts[i].RelativePosition = new Vector2(width * style.InputSide, height * Mathf.Lerp(style.TopRatio, style.BottomRatio, node.InputPorts.Count == 1 ? .5f : (float)i / (node.InputPorts.Count - 1)));
			for (int i = 0; i < node.OutputPorts.Count; i++)
				node.OutputPorts[i].RelativePosition = new Vector2(width * style.OutputSide, height * Mathf.Lerp(style.TopRatio, style.BottomRatio, node.OutputPorts.Count == 1 ? .5f : (float)i / (node.OutputPorts.Count - 1)));
		}

		protected override void BeginChunkSubmission(ulong chunk, BlueprintPrimitiveRenderer renderer)
		{
			foreach (var pair in _nodeTexts)
				if (pair.Value.chunkID == chunk)
					_staleTextCandidates.Add(pair.Key);
		}

		protected override void EndChunkSubmission(ulong chunk, BlueprintPrimitiveRenderer renderer)
		{
			var remove = new List<string>();
			foreach (var id in _staleTextCandidates)
				if (_nodeTexts.TryGetValue(id, out var item) && item.chunkID == chunk)
					remove.Add(id);
			foreach (var id in remove)
			{
				if (_nodeTexts[id].text != null)
					Object.Destroy(_nodeTexts[id].text.gameObject);
				_nodeTexts.Remove(id);
			}
			_staleTextCandidates.Clear();
		}

		private void UpdateNodeText(BlueprintNode node, ulong chunk, Vector2 local, QuickGraphNodeRenderStyle style, float border, Color titleColor)
		{
			_staleTextCandidates.Remove(node.ElementId);
			TMP_Text text;
			if (_nodeTexts.TryGetValue(node.ElementId, out var old) && old.chunkID == chunk && old.text != null)
			{
				text = old.text;
			}
			else
			{
				if (old.text != null)
					Object.Destroy(old.text.gameObject);
				var go = new GameObject("__Text_" + node.ElementId, typeof(RectTransform));
				go.hideFlags = HideFlags.HideAndDontSave;
				go.transform.SetParent(GetOrCreateChunk(chunk).transform, false);
				text = go.AddComponent<TextMeshProUGUI>();
				_nodeTexts[node.ElementId] = (chunk, text);
			}

			text.text = node.NodeTitle;
			text.color = titleColor;
			text.fontSize = style.TitleFontSize;
			text.font = style.TitleFont;
			text.alignment = style.TitleAlignment;
			var rt = text.rectTransform;
			var pivot = text.transform.parent is RectTransform parent ? parent.pivot : new Vector2(.5f, .5f);
			rt.anchorMin = pivot;
			rt.anchorMax = pivot;
			rt.pivot = new Vector2(.5f, .5f);
			rt.anchoredPosition = local;
			rt.sizeDelta = new Vector2(style.Width - border * 2, style.Height - border * 2);
			text.gameObject.SetActive(true);
			rt.SetAsLastSibling();
		}

		private void HideNodeText(string id)
		{
			if (_nodeTexts.TryGetValue(id, out var item) && item.text != null)
				item.text.gameObject.SetActive(false);
		}

		protected override void OnChunkRecycled(ulong chunk, BlueprintPrimitiveRenderer renderer)
		{
			var remove = new List<string>();
			foreach (var pair in _nodeTexts)
				if (pair.Value.chunkID == chunk)
					remove.Add(pair.Key);
			foreach (var id in remove)
			{
				if (_nodeTexts[id].text != null)
					Object.Destroy(_nodeTexts[id].text.gameObject);
				_nodeTexts.Remove(id);
			}
		}

		protected override void DestroyAllChunks()
		{
			foreach (var pair in _nodeTexts)
				if (pair.Value.text != null)
					Object.Destroy(pair.Value.text.gameObject);
			_nodeTexts.Clear();
			base.DestroyAllChunks();
		}

		public override void OnDestroy()
		{
			_styles?.Dispose();
			_styles = null;
			base.OnDestroy();
		}
	}
}
