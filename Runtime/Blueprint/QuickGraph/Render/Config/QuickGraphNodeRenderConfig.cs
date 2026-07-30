using System;
using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	/// <summary>
	/// QuickGraph 节点渲染配置表 —— 控制节点矩形、边框、圆角等视觉参数。
	/// 右键 Project 窗口 → Create/Blueprint/QuickGraph/Node Render Config 创建资产。
	/// 拖入蓝图组件的 ToolConfigAssets 列表即可覆盖默认值。
	/// </summary>
	[CreateAssetMenu(
		fileName = "QuickGraphNodeRenderConfig",
		menuName = "Xeric Library/Blueprint/QuickGraph/Node Render Config",
		order = 1)]
	[BlueprintTheme("QuickGraph")]
	public class QuickGraphNodeRenderConfig : BlueprintToolConfigBase
	{
		/// <summary>目标工具类型</summary>
		public override System.Type TargetToolType => typeof(NodeCanvasRenderTool);

		[Header("尺寸")]
		[Tooltip("节点宽度（像素）")]
		public float NodeWidth = 90f;

		[Tooltip("节点高度（像素）")]
		public float NodeHeight = 130f;

		[Header("边框")]
		[Tooltip("边框厚度（像素）")]
		public float BorderThickness = 3f;

		[Header("圆角")]
		[Tooltip("圆角大小（0~1 归一化）")]
		[Range(0f, 0.5f)]
		public float ChamferSize = 0.2f;

		[Tooltip("圆角细分段数")]
		[Range(1, 16)]
		public int ChamferSegments = 4;

		[Header("颜色")]
		[Tooltip("节点默认背景色")]
		public Color DefaultBackgroundColor = new Color(0.16f, 0.16f, 0.16f);
		[Tooltip("节点默认文字色")]
		public Color DefaultTextColor = Color.white;

		[Header("文本")]
		[Tooltip("节点标题字号")]
		public float TitleFontSize = 14f;
		[Tooltip("节点标题字体（留空使用默认 TMP 字体）")]
		public TMPro.TMP_FontAsset TitleFont;

		[Header("已弃用的 LOD 兼容字段")]
		[Obsolete("节点 LOD 已迁移至 BlueprintRenderLodConfig（绝对 Zoom + 滞回），此字段不再参与渲染。")]
		[HideInInspector] public float Lod0Threshold = 0.3f;
		[Obsolete("节点 LOD 已迁移至 BlueprintRenderLodConfig（绝对 Zoom + 滞回），此字段不再参与渲染。")]
		[HideInInspector] public float Lod1Threshold = 0.7f;
	}
}
