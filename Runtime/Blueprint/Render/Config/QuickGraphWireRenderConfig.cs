using System;
using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// QuickGraph 连线渲染配置表 —— 控制贝塞尔曲线宽度、箭头形状等全部视觉参数。
	/// 右键 Project 窗口 → Create/Blueprint/QuickGraph/Wire Render Config 创建资产。
	/// 拖入蓝图组件的 ToolConfigAssets 列表即可覆盖默认值。
	/// </summary>
	[CreateAssetMenu(
		fileName = "QuickGraphWireRenderConfig",
		menuName = "Xeric Library/Blueprint/QuickGraph/Wire Render Config",
		order = 2)]
	[BlueprintTheme("QuickGraph")]
	public class QuickGraphWireRenderConfig : BlueprintToolConfigBase
	{
		/// <summary>目标工具类型</summary>
		public override System.Type TargetToolType
		{
			get { return typeof(QuickUGUIGraphWireRenderTool); }
		}

		[Header("连线")]
		[Tooltip("连线宽度（像素）")]
		[Range(1f, 20f)]
		public float WireWidth = 5f;

		[Header("贝塞尔手柄")]
		[Tooltip("源端手柄从端口伸出的距离（画布单位）。数值越大曲线越平缓。")]
		[Range(10f, 500f)]
		public float SourceHandleLength = 80f;
		[Tooltip("目标端手柄从端口伸出的距离（画布单位）。")]
		[Range(10f, 500f)]
		public float TargetHandleLength = 80f;

		[Header("箭头")]
		[Tooltip("箭头形状")]
		public ArrowShape ArrowShape = ArrowShape.Triangle;

		[Tooltip("箭头是否反向")]
		public bool ArrowReversed = false;

		[Tooltip("箭头在曲线上的位置（0=起点, 1=终点）")]
		[Range(0f, 1f)]
		public float ArrowProgress = 1f;

		[Tooltip("箭头宽度（垂直切线方向，像素）")]
		[Range(4f, 40f)]
		public float ArrowWidth = 12f;

		[Tooltip("箭头高度（沿切线方向，像素）")]
		[Range(4f, 40f)]
		public float ArrowHeight = 10f;

		[Tooltip("深度补偿：-1=尾部对齐曲线点，0=中心对齐，1=头部对齐曲线点")]
		[Range(-1f, 1f)]
		public float ArrowDepthCompensation = -1f;

		[Header("曲线质量")]
		[Tooltip("贝塞尔曲线细分段数")]
		[Range(8, 64)]
		public int TessellationSegments = 32;
	}
}
