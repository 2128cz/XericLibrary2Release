using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XericLibrary.Runtime.Blueprint.Render;
using XericLibrary.Runtime.UIGraph;
using PrimitiveType = XericLibrary.Runtime.UIGraph.PrimitiveType;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	[BlueprintTool(phase: ToolPhase.Render, order: 100)]
	[BlueprintTheme("QuickGraph")]
	public class NodeCanvasRenderTool : BlueprintCanvasRenderTool<BlueprintPrimitiveRenderer>
	{
		private QuickGraphNodeRenderConfig _defaultConfig;
		private QuickGraphRenderStyleResolver _styleResolver;
		private readonly Dictionary<string, (ulong chunkID, TMP_Text text)> _nodeTexts = new Dictionary<string, (ulong, TMP_Text)>();
		private readonly HashSet<string> _staleTextCandidates = new HashSet<string>();
		private QuickGraphNodeRenderConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphNodeRenderConfig external) return external;
				if (_defaultConfig == null) _defaultConfig = ScriptableObject.CreateInstance<QuickGraphNodeRenderConfig>();
				return _defaultConfig;
			}
		}

		public override void OnInitialize() { base.OnInitialize(); _styleResolver = new QuickGraphRenderStyleResolver(Graph, nodeFallback: ConfigStyle); SyncAllNodeGeometry(); }
		public override void OnConfigChanged()
		{
			_styleResolver?.Refresh(); SyncAllNodeGeometry();
			base.OnConfigChanged();
		}
		private QuickGraphNodeRenderStyle ConfigStyle()
		{
			var cfg = Config;
			return new QuickGraphNodeRenderStyle { Width = cfg.NodeWidth, Height = cfg.NodeHeight, Border = cfg.BorderThickness, Chamfer = cfg.ChamferSize, ChamferSegments = cfg.ChamferSegments, TitleFontSize = cfg.TitleFontSize };
		}
		protected override BlueprintPrimitiveRenderer BuildChunkRenderer(GameObject go) => go.AddComponent<BlueprintPrimitiveRenderer>();
		protected override void ClearChunkData(BlueprintPrimitiveRenderer renderer) => renderer.ClearAll();
		protected override void SetChunkDirty(BlueprintPrimitiveRenderer renderer) => renderer.SetPrimitiveDirty(0);
		protected override bool AcceptsElement(IBlueprintElement element) => element is BlueprintNode;

		protected override void AppendElementData(BlueprintPrimitiveRenderer renderer, IBlueprintElement element, ulong chunkID)
		{
			if (!(element is BlueprintNode node)) return;
			var cfg = Config; var style = _styleResolver != null ? _styleResolver.Snapshot.Node : new QuickGraphNodeRenderStyle { Width=cfg.NodeWidth, Height=cfg.NodeHeight, Border=cfg.BorderThickness, Chamfer=cfg.ChamferSize, ChamferSegments=cfg.ChamferSegments, TitleFontSize=cfg.TitleFontSize }; var lod = CurrentElementSubmission.LOD;
			CalculatePortPositions(node, style.Width, style.Height);
			var local = CurrentElementSubmission.ChunkLocalPosition;
			float border = lod == BlueprintLodLevel.Minimal ? 0f : style.Border;
			Color bg = lod == BlueprintLodLevel.Minimal ? node.NodeColor : node.NodeBGColor;
			Color edge = node.NodeColor;
			renderer.AddPrimitive(new PrimitiveCacheEntry { type = PrimitiveType.Rectangle, sizeMode = SizeMode.InscribedEllipse, center = local, size = new Vector2(style.Width, style.Height), @params = new PrimitiveParams { axisScaleX = 1f, axisScaleY = 1f, sideCount = 4, chamferSize = style.Chamfer, chamferSegments = style.ChamferSegments, bgColor = new Color32(0,0,0,0), centerColor = bg, borderColor = edge, borderThickness = border } });
			if (lod == BlueprintLodLevel.Full) UpdateNodeText(node, chunkID, local, style.Width, style.Height, border, style.TitleFontSize, cfg); else HideNodeText(node.ElementId);
		}
		private void SyncAllNodeGeometry()
		{
			if (Graph == null) return;
			var cfg = Config; var style = _styleResolver != null ? _styleResolver.Snapshot.Node : new QuickGraphNodeRenderStyle { Width=cfg.NodeWidth, Height=cfg.NodeHeight };
			foreach (var node in Graph.Nodes)
			{
				var oldSize = node.NodeSize;
				CalculatePortPositions(node, style.Width, style.Height);
				if (oldSize != node.NodeSize) Graph.UpdateNodeGeometry(node);
			}
		}

		private static void CalculatePortPositions(BlueprintNode node, float width, float height)
		{
			// Position 语义为视觉中心；渲染配置尺寸必须同步给数据模型，供 BoundingBox / RTree 使用。
			node.NodeSize = new Vector2(width, height);
			for (int i=0;i<node.InputPorts.Count;i++) node.InputPorts[i].RelativePosition = new Vector2(-width*.5f, height*(.5f-(float)(i+1)/(node.InputPorts.Count+1)));
			for (int i=0;i<node.OutputPorts.Count;i++) node.OutputPorts[i].RelativePosition = new Vector2(width*.5f, height*(.5f-(float)(i+1)/(node.OutputPorts.Count+1)));
		}
		protected override void BeginChunkSubmission(ulong chunk, BlueprintPrimitiveRenderer renderer)
		{
			foreach (var pair in _nodeTexts)
				if (pair.Value.chunkID == chunk) _staleTextCandidates.Add(pair.Key);
		}
		protected override void EndChunkSubmission(ulong chunk, BlueprintPrimitiveRenderer renderer)
		{
			var remove = new List<string>();
			foreach (var id in _staleTextCandidates)
				if (_nodeTexts.TryGetValue(id, out var item) && item.chunkID == chunk) remove.Add(id);
			foreach (var id in remove)
			{
				if (_nodeTexts[id].text != null) Object.Destroy(_nodeTexts[id].text.gameObject);
				_nodeTexts.Remove(id);
			}
			_staleTextCandidates.Clear();
		}
		private void UpdateNodeText(BlueprintNode node, ulong chunk, Vector2 local, float width, float height, float border, float titleFontSize, QuickGraphNodeRenderConfig cfg)
		{
			_staleTextCandidates.Remove(node.ElementId);
			TMP_Text text;
			if (_nodeTexts.TryGetValue(node.ElementId, out var old) && old.chunkID == chunk && old.text != null) text = old.text;
			else
			{
				if (old.text != null) Object.Destroy(old.text.gameObject);
				var go = new GameObject($"__Text_{node.ElementId}", typeof(RectTransform)); go.hideFlags = HideFlags.HideAndDontSave; go.transform.SetParent(rendererTransform(chunk), false);
				text = go.AddComponent<TextMeshProUGUI>(); text.alignment = TextAlignmentOptions.Center; _nodeTexts[node.ElementId] = (chunk, text);
			}
			text.text=node.NodeTitle; text.color=node.NodeTextColor; text.fontSize=titleFontSize; if(cfg.TitleFont!=null) text.font=cfg.TitleFont;
			var rt=text.rectTransform; var chunkPivot = text.transform.parent is RectTransform parent ? parent.pivot : new Vector2(.5f, .5f);
			rt.anchorMin=chunkPivot;rt.anchorMax=chunkPivot;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=local;rt.sizeDelta=new Vector2(width-border*2,height-border*2);text.gameObject.SetActive(true);rt.SetAsLastSibling();
		}
		private Transform rendererTransform(ulong chunk) => GetOrCreateChunk(chunk).transform;
		private void HideNodeText(string id) { if (_nodeTexts.TryGetValue(id,out var item) && item.text != null) item.text.gameObject.SetActive(false); }
		protected override void OnChunkRecycled(ulong chunk, BlueprintPrimitiveRenderer renderer)
		{
			_staleTextCandidates.Clear();
			var remove=new List<string>(); foreach(var pair in _nodeTexts) if(pair.Value.chunkID==chunk) remove.Add(pair.Key);
			foreach(var id in remove) { if(_nodeTexts[id].text!=null) Object.Destroy(_nodeTexts[id].text.gameObject); _nodeTexts.Remove(id); }
		}
		protected override void DestroyAllChunks() { foreach(var pair in _nodeTexts) if(pair.Value.text!=null) Object.Destroy(pair.Value.text.gameObject); _nodeTexts.Clear(); base.DestroyAllChunks(); }
		public override void OnDestroy() { _styleResolver?.Dispose(); _styleResolver = null; base.OnDestroy(); }
	}
}
