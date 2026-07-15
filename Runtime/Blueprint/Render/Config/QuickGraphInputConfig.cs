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

		[Tooltip("滚轮缩放除数。公式：zoom += scrollDelta / ZoomDivisor。\n旧输入系统 scrollDelta.y 约 ±3，默认 30 = 每格 ±0.1")]
		[Range(1f, 200f)]
		public float ZoomDivisor = 30f;

		[Header("拖拽平移")]
		[Tooltip("中键拖拽平移速度系数。1=鼠标移动 1px，画布移动 1px（经缩放校正）")]
		[Range(0.1f, 10f)]
		public float DragPanSpeed = 1f;

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

		[Tooltip("键盘 +/- 缩放速度（与 ZoomDivisor 配合，公式：zoom += 1 / ZoomDivisor）")]
		[Range(0.005f, 0.5f)]
		public float KeyboardZoomSpeed = 0.05f;

		[Header("框选")]
		[Tooltip("框选拖拽触发阈值（画布单位）。拖拽距离超过此值后才显示选框并开始框选")]
		[Range(1f, 100f)]
		public float BoxSelectThreshold = 10f;
	}
}
