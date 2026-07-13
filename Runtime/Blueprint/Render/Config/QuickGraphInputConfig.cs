using System;
using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// QuickGraph 输入工具配置表 —— 控制平移/缩放插值速率、灵敏度等可调参数。
	/// 右键 Project 窗口 → Create/Blueprint/QuickGraph/Input Config 创建资产。
	/// 拖入蓝图组件的 ToolConfigAssets 列表即可覆盖默认值。
	/// </summary>
	[CreateAssetMenu(
		fileName = "QuickGraphInputConfig",
		menuName = "Xeric Library/Blueprint/QuickGraph/Input Config",
		order = 4)]
	[BlueprintTheme("QuickGraph")]
	public class QuickGraphInputConfig : BlueprintToolConfigBase
	{
		/// <summary>目标工具类型</summary>
		public override System.Type TargetToolType
		{
			get { return typeof(QuickGraphInputTool); }
		}

		[Header("平移")]
		[Tooltip("平移动画插值速率（越大跟随越快，3=适中，10=几乎瞬移）")]
		[Range(1f, 20f)]
		public float PanLerpSpeed = 3f;

		[Header("缩放")]
		[Tooltip("缩放动画插值速率（越大跟随更快）")]
		[Range(1f, 30f)]
		public float ZoomLerpSpeed = 8f;

		[Tooltip("滚轮缩放除数。公式：zoom += scrollDelta / ZoomDivisor。\nUnity Input System 的 scrollDelta.y 标准值为 120/格，\n1200 = 每格 +0.1，600 = 每格 +0.2，300 = 每格 +0.4")]
		[Range(100f, 5000f)]
		public float ZoomDivisor = 1200f;

		[Header("缩放范围")]
		[Tooltip("最小缩放级别")]
		[Range(0.01f, 1f)]
		public float MinZoom = 0.1f;
		[Tooltip("最大缩放级别")]
		[Range(1f, 10f)]
		public float MaxZoom = 3f;

		[Header("键盘")]
		[Tooltip("键盘方向键平移速度（画布单位/秒）")]
		[Range(50f, 2000f)]
		public float KeyboardPanSpeed = 600f;

		[Tooltip("键盘 +/- 缩放速度（每次帧增量，建议 0.01~0.5）")]
		[Range(0.005f, 0.5f)]
		public float KeyboardZoomSpeed = 0.05f;

		[Header("框选")]
		[Tooltip("框选拖拽触发阈值（像素）")]
		[Range(1f, 20f)]
		public float DragThreshold = 5f;
	}
}
