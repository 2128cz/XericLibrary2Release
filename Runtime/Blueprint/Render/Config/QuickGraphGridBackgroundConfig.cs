using System;
using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// QuickGraph 网格背景配置表 —— 控制 Shader 全部可调参数。
	/// 材质实例由此配置统一管理，所有调用者通过 <see cref="GetOrCreateMaterial"/> 获取同一实例。
	/// <see cref="ApplyToMaterial"/> 封装所有材质属性写入（含 _Transform 同步画布平移/缩放）。
	/// 右键 Project 窗口 → Create/Blueprint/QuickGraph/Grid Background Config 创建资产。
	/// </summary>
	[CreateAssetMenu(
		fileName = "QuickGraphGridBackgroundConfig",
		menuName = "Xeric Library/Blueprint/QuickGraph/Grid Background Config",
		order = 3)]
	[BlueprintTheme("QuickGraph")]
	public class QuickGraphGridBackgroundConfig : BlueprintToolConfigBase
	{
		/// <summary>目标工具类型</summary>
		public override System.Type TargetToolType
		{
			get { return typeof(QuickUGUIGraphGridBackgroundTool); }
		}

		[Header("材质来源")]
		[Tooltip("网格 Shader 名称")]
		public string ShaderName = "XericLibrary/BluePrint/BlueprintBackgound_GridLine";

		[Tooltip("可选：直接指定背景材质。设置后将忽略 ShaderName。")]
		public Material OverrideMaterial = null;

		[Header("网格参数")]
		[Tooltip("网格间距（画布单位）。值越小网格越密。")]
		public float GridSize = 100f;

		[Tooltip("叠加网格密度")]
		public float GridOverlayPower = 5f;

		[Tooltip("线条阈值。0.5=极粗, 1=极细")]
		[Range(0.5f, 1f)]
		public float GridLineThreshold = 0.99f;

		[Tooltip("线条扩展/柔和度。越大线条越宽。")]
		[Range(0.001f, 1f)]
		public float GridExp = 0.001f;

		[Header("颜色")]
		[Tooltip("网格线颜色")]
		public Color GridColor = Color.white;

		[Tooltip("背景底色")]
		public Color GridBackgroundColor = Color.black;

		// ─── 运行时缓存的材质实例 ───

		[NonSerialized]
		private Material _cachedMaterial;

		/// <summary>
		/// 获取或创建材质实例。
		/// - 若 OverrideMaterial 不为空，直接返回。
		/// - 否则按 ShaderName 查找 Shader，创建材质并缓存。
		/// - 同一配置实例上多次调用返回同一材质对象。
		/// </summary>
		public Material GetOrCreateMaterial()
		{
			if (OverrideMaterial != null)
				return OverrideMaterial;

			if (_cachedMaterial != null)
				return _cachedMaterial;

			if (!string.IsNullOrEmpty(ShaderName))
			{
				var shader = Shader.Find(ShaderName);
				if (shader != null)
				{
					_cachedMaterial = new Material(shader);
					_cachedMaterial.name = "BpGridBackground_Mat";
				}
			}

			return _cachedMaterial;
		}

		/// <summary>
		/// 将当前配置的所有参数写入材质。
		/// <para>包括：
		/// - _Transform（scaleX, scaleY, offsetX, offsetY）：随画布 zoom / pan 同步更新；
		/// - _GridOverlayPower、_GridLineThreshold、_GridExp；
		/// - _GridColor、_GridBackgroundColor。</para>
		/// 每次渲染时调用以保持与蓝图画布的缩放/平移同步。
		/// </summary>
		/// <param name="mat">目标材质实例</param>
		/// <param name="rectSize">背景板 RectTransform 的像素尺寸 (width, height)</param>
		/// <param name="zoom">当前画布缩放级别</param>
		/// <param name="panOffset">当前画布平移偏移量</param>
		public void ApplyToMaterial(Material mat, Vector2 rectSize, float zoom, Vector2 panOffset)
		{
			if (mat == null) return;

			// ── _Transform: 基于画布 zoom / pan 同步 ──
			// Shader 中: texCoord2 = uv * _Transform.xy + _Transform.zw
			// uv (0,0) = Image 左下角, uv (1,1) = Image 右上角
			// CanvasToLocal: localPoint = canvasPoint * zoom + panOffset
			// UV ↔ localPoint:     uv = localPoint / rectSize + 0.5f
			//                        ^^^^^ pivot(0.5,0.5) 的 UV 偏移
			// 代入: uv = (canvasPoint * zoom + panOffset) / rectSize + 0.5f
			// 目标: gridPos = canvasPoint / GridSize = uv * scale + offset
			// 解出: scale = rectSize / (zoom * GridSize)
			//      offset = -(panOffset + 0.5 * rectSize) / (zoom * GridSize)
			float sx = rectSize.x / (zoom * GridSize);
			float sy = rectSize.y / (zoom * GridSize);
			float ox = -(panOffset.x + 0.5f * rectSize.x) / (zoom * GridSize);
			float oy = -(panOffset.y + 0.5f * rectSize.y) / (zoom * GridSize);
			mat.SetVector("_Transform", new Vector4(sx, sy, ox, oy));

			// ── 网格线参数 ──
			mat.SetFloat("_GridOverlayPower", GridOverlayPower);
			mat.SetFloat("_GridLineThreshold", GridLineThreshold);
			mat.SetFloat("_GridExp", GridExp);

			// ── 颜色 ──
			mat.SetColor("_GridColor", GridColor);
			mat.SetColor("_GridBackgroundColor", GridBackgroundColor);
		}

		/// <summary>
		/// 释放此配置创建的材质实例（不释放 OverrideMaterial）。
		/// </summary>
		public void ReleaseMaterial()
		{
			if (_cachedMaterial != null)
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
					DestroyImmediate(_cachedMaterial);
				else
#endif
					Destroy(_cachedMaterial);
				_cachedMaterial = null;
			}
		}
	}
}
