using System.Collections.Generic;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 连线渲染工具 — 按网格区块管理 BlueprintCurveRenderer，支持 Viewport Transform。
	/// <para>连线跟随其源节点（SourcePort.OwnerNode）进入对应 Chunk 的 CurveRenderer。</para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 110)]
	[BlueprintTheme("QuickGraph")]
	public class WireCanvasRenderTool : BlueprintCanvasRenderTool<BlueprintCurveRenderer>
	{
		private QuickGraphWireRenderConfig _defaultConfig;
		private QuickGraphWireRenderConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphWireRenderConfig external) return external;
				if (_defaultConfig == null)
					_defaultConfig = ScriptableObject.CreateInstance<QuickGraphWireRenderConfig>();
				return _defaultConfig;
			}
		}

		// ====================================================================
		// 抽象实现
		// ====================================================================

		protected override BlueprintCurveRenderer BuildChunkRenderer(GameObject chunkGo)
		{
			return chunkGo.AddComponent<BlueprintCurveRenderer>();
		}

		protected override void AppendElementData(BlueprintCurveRenderer renderer, IBlueprintElement element)
		{
			if (!(element is BlueprintWire wire)) return;
			if (wire.SourcePort == null || wire.TargetPort == null) return;
			if (wire.SourcePort.OwnerNode == null || wire.TargetPort.OwnerNode == null) return;

			var cfg = Config;

			// 端点：画布空间坐标（由 Viewport Transform 统一变换）
			Vector2 startPos = wire.SourcePort.GetWorldPosition();
			Vector2 endPos = wire.TargetPort.GetWorldPosition();

			// 手柄方向 = 端口法向
			float srcDirX = Mathf.Sign(wire.SourcePort.RelativePosition.x);
			float tgtDirX = Mathf.Sign(wire.TargetPort.RelativePosition.x);
			if (srcDirX == tgtDirX) srcDirX *= -1f;

			// 手柄长度（原始值，由 Viewport 统一缩放）
			Vector2 handle1 = startPos + new Vector2(srcDirX * cfg.SourceHandleLength, 0f);
			Vector2 handle2 = endPos + new Vector2(tgtDirX * cfg.TargetHandleLength, 0f);

			float ww = cfg.WireWidth;

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
				width = cfg.ArrowWidth,
				height = cfg.ArrowHeight,
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

			renderer.AddCurve(curveEntry, ctrlPts2, widths, new[] { arrow });
		}

		protected override void ClearChunkData(BlueprintCurveRenderer renderer)
		{
			renderer.ClearAll();
		}

		protected override void SetChunkDirty(BlueprintCurveRenderer renderer)
		{
			renderer.RebuildAll();
		}

		protected override Vector2 GetElementPosition(IBlueprintElement element)
		{
			if (!(element is BlueprintWire wire)) return Vector2.zero;
			// 以源节点的位置作为 Chunk 分组依据
			return wire.SourcePort?.OwnerNode?.Position ?? Vector2.zero;
		}

		protected override bool IsElementCulled(IBlueprintElement element)
		{
			if (!(element is BlueprintWire wire)) return true;
			if (wire.SourcePort?.OwnerNode == null || wire.TargetPort?.OwnerNode == null)
				return true;

			var viewport = Graph?.Canvas?.GetViewportRect() ?? new Rect();
			float margin = Config.WireWidth * 20f;
			viewport.xMin -= margin;
			viewport.xMax += margin;
			viewport.yMin -= margin;
			viewport.yMax += margin;

			// 两端都不在视口内则跳过
			return !viewport.Contains(wire.SourcePort.OwnerNode.Position)
			    && !viewport.Contains(wire.TargetPort.OwnerNode.Position);
		}
	}
}
