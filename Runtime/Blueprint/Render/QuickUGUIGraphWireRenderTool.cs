using System.Collections.Generic;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 快速 UGUI 图论连线渲染工具。
	/// 通过共享的 LayerContainerManager 使用与节点相同的层容器，
	/// 连线绘制在两端节点所在层的较低层中（不遮挡节点）。
	/// 可调参数由 QuickGraphWireRenderConfig 配置资产提供。
	/// </summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 110)]
	[BlueprintTheme("QuickGraph")]
	public class QuickUGUIGraphWireRenderTool : BlueprintTool
	{
		private QuickGraphWireRenderConfig _defaultConfig;

		private QuickGraphWireRenderConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphWireRenderConfig external)
					return external;
				if (_defaultConfig == null)
					_defaultConfig = ScriptableObject.CreateInstance<QuickGraphWireRenderConfig>();
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
			int bucketCount = buckets.BucketCount;

			// 视口裁剪
			var viewport = Graph.Canvas.GetViewportRect();
			float margin = cfg.WireWidth * 20f;
			viewport.xMin -= margin;
			viewport.xMax += margin;
			viewport.yMin -= margin;
			viewport.yMax += margin;

			var layerWires = new List<BlueprintWire>[bucketCount];
			for (int i = 0; i < bucketCount; i++)
				layerWires[i] = new List<BlueprintWire>();

			foreach (var wire in Graph.Wires)
			{
				int srcLayer = wire.SourcePort?.OwnerNode?.RenderLayer ?? 0;
				int tgtLayer = wire.TargetPort?.OwnerNode?.RenderLayer ?? 0;

				int wireLayer = Mathf.Min(srcLayer, tgtLayer);
				if (wireLayer < 0 || wireLayer >= bucketCount) wireLayer = 0;

				// 两端都不在视口内则跳过
				if (wire.SourcePort?.OwnerNode != null && wire.TargetPort?.OwnerNode != null)
				{
					if (!viewport.Contains(wire.SourcePort.OwnerNode.Position) &&
					    !viewport.Contains(wire.TargetPort.OwnerNode.Position))
						continue;
				}

				layerWires[wireLayer].Add(wire);
			}

			for (int i = 0; i < bucketCount; i++)
			{
				if (layerWires[i].Count > 0)
				{
					var entry = _layerMgr.GetOrCreateLayer(i);
					RenderLayerWires(entry, layerWires[i], cfg, Graph.Canvas);
				}
				else
				{
					var entry = _layerMgr.TryGetLayer(i);
					entry?.CurveRenderer?.ClearAll();
				}
			}
		}

		private static void RenderLayerWires(LayerContainerEntry entry, List<BlueprintWire> wires,
			QuickGraphWireRenderConfig cfg, IBlueprintCanvas canvas)
		{
			entry.CurveRenderer.ClearAll();

			float zoom = canvas.ZoomLevel;

			foreach (var wire in wires)
			{
				if (wire.SourcePort == null || wire.TargetPort == null) continue;
				if (wire.SourcePort.OwnerNode == null || wire.TargetPort.OwnerNode == null) continue;

				Vector2 startPos = canvas.CanvasToLocal(wire.SourcePort.GetWorldPosition());
				Vector2 endPos = canvas.CanvasToLocal(wire.TargetPort.GetWorldPosition());

				// 手柄方向 = 端口在节点上的朝向
				// 端口在节点右侧 (RelativePosition.x > 0) → 手柄朝右 (+X)
				// 端口在节点左侧 (RelativePosition.x < 0) → 手柄朝左 (-X)
				float srcDirX = Mathf.Sign(wire.SourcePort.RelativePosition.x);
				float tgtDirX = Mathf.Sign(wire.TargetPort.RelativePosition.x);
				// 同一侧（如两个端口都在右侧）则方向取反
				if (srcDirX == tgtDirX) srcDirX *= -1f;

				// 手柄距离 = 配置值 × zoom（画布单位 → local 坐标，与 CanvasToLocal 一致）
				Vector2 handle1 = startPos + new Vector2(srcDirX * cfg.SourceHandleLength * zoom, 0f);
				Vector2 handle2 = endPos   + new Vector2(tgtDirX * cfg.TargetHandleLength * zoom, 0f);

				float ww = cfg.WireWidth * zoom;

				var ctrlPts = new Vector3[]
				{
					new Vector3(startPos.x, startPos.y, ww),
					new Vector3(handle1.x, handle1.y, ww),
					new Vector3(handle2.x, handle2.y, ww),
					new Vector3(endPos.x, endPos.y, ww),
				};

				Color32 srcColor = wire.SourcePort.OwnerNode.NodeColor;
				Color32 tgtColor = wire.TargetPort.OwnerNode.NodeColor;

				var arrow = new ArrowHeadData
				{
					shape = cfg.ArrowShape,
					reversed = cfg.ArrowReversed,
					progress = cfg.ArrowProgress,
					width = cfg.ArrowWidth * zoom,
					height = cfg.ArrowHeight * zoom,
					depthCompensation = cfg.ArrowDepthCompensation,
					color = tgtColor,
				};

				int count = ctrlPts.Length;
				var ctrlPts2 = new Vector2[count];
				var widths = new float[count];
				for (int i = 0; i < count; i++)
				{
					ctrlPts2[i] = ctrlPts[i];
					widths[i] = ctrlPts[i].z;
				}

				var curveEntry = new CurveCacheEntry
				{
					startColor = srcColor,
					endColor = tgtColor,
					tessellationSegments = cfg.TessellationSegments,
				};

				entry.CurveRenderer.AddCurve(curveEntry, ctrlPts2, widths, new[] { arrow });
			}

			if (wires.Count > 0)
				entry.CurveRenderer.RebuildAll();
		}

		private void EnsureRenderRoot()
		{
			if (_renderRoot != null) return;
			if (Graph.Canvas == null) return;
			_renderRoot = Graph.Canvas.GetRootTransform();
		}

		public override void OnDestroy()
		{
			_layerMgr = null;
			_renderRoot = null;
		}
	}
}
