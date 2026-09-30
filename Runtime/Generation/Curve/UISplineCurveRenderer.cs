using System;
using System.Collections.Generic;
using UnityEngine;

using XericLibrary.Runtime.XericComponent;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XericLibrary.Runtime.UIGraph
{
	/// <summary>
	/// 由界面样条（XericRectSplinePath）驱动的曲线绘制组件。
	/// <code>
	/// 一条样条上可以同时铺多条"面片"，每条由一对 (头部, 尾部) 进度值决定覆盖范围，
	/// 于是 {1,0} 是铺满整条的底轨，{0.6f,0} 就是填到 60% 的进度条本体。
	/// 进度默认经样条烘焙后的弧长重映射校正，画面上的推进长度才和进度值成正比；
	/// 面片上的 uv0.y 被重映射成整条面片的 0→1，材质按 UV 就能做流动/遮罩类效果。
	/// </code>
	/// </summary>
	[AddComponentMenu("Xeric Library/UI/UISplineCurveRenderer", 17)]
	public class UISplineCurveRenderer : CurveCacheRendererBase
	{
		#region 字段属性

		/// <summary>驱动本曲线的界面样条</summary>
		public XericRectSplinePath splinePath;

		/// <summary>
		/// 面片对：偶数下标=头部、奇数下标=尾部，取值 [0,1]。
		/// 例：{1,0} 一条铺满；{1,0, 0.6f,0} 两条＝底轨 + 60% 填充。
		/// </summary>
		public float[] headTailPairs = { 1f, 0f };

		/// <summary>可空；与面片同序时逐条叠乘到样条颜色上（底轨压暗、填充提亮）</summary>
		public Color32[] ribbonTints;

		/// <summary>进度值的含义：直接按参数，还是先经烘焙做弧长校正</summary>
		public SplineProgressMapping progressMapping = SplineProgressMapping.BakedDistance;

		/// <summary>烘焙采样数，越大校正越细；同时压住库侧 1/(N+1) 的系统偏差</summary>
		[Range(8, 4096)]
		public int bakeSubdivide = 256;

		/// <summary>
		/// 什么时候做弧长烘焙。默认 OnEditEnd：几何停止变化后补烘一次。
		/// 烘焙是 O(subdivide) 的采样，放在每帧会把编辑器拖到卡。
		/// </summary>
		public SplineBakeTrigger bakeTrigger = SplineBakeTrigger.OnEditEnd;

		/// <summary>已经烘过几次（排查用：编辑器里拖动一整轮应该只加 1）</summary>
		public int BakeRunCount => m_BakeRunCount;

		/// <summary>面片上 uv0.y 代表条内进度还是全样条进度</summary>
		public SplineUvProgressSpace uvProgressSpace = SplineUvProgressSpace.RibbonLocal;

		/// <summary>每条面片至少切成几块，决定 uv 与视觉距离对齐的精度</summary>
		[Range(1, 64)]
		public int chunksPerRibbon = 8;

		/// <summary>基准带宽（样条所在矩形的局部像素）</summary>
		[Min(0.01f)]
		public float curveWidth = 10f;

		/// <summary>带宽来源：常量，还是乘上样条的缩放渐层 Y</summary>
		public SplineWidthSource widthSource = SplineWidthSource.ScaleGradientY;

		/// <summary>单段贝塞尔的细分段数</summary>
		[Range(2, 128)]
		public int tessellationSegments = 32;

		/// <summary>样条颜色渐层全透明时的兜底颜色</summary>
		public Color fallbackColor = Color.white;

		/// <summary>
		/// 推给材质 _MainTex 的贴图。
		/// Canvas 是用 MaterialPropertyBlock 把 Graphic.mainTexture 顶到 _MainTex 上的，
		/// 所以必须走这个口子交出去；只改 material 的贴图会被默认白图覆盖掉。
		/// </summary>
		public Texture progressTexture;

		/// <summary>SetProgress 作用在哪条面片的头部；-1 表示不指定</summary>
		public int fillRibbonIndex = 1;

		/// <summary>每帧跟随样条变化重建（关掉后只能靠 RebuildCurve 手动触发）</summary>
		public bool followSplineChanges = true;

		/// <summary>与曲线缓存同序的 uv 区间，x=起点 y=终点</summary>
		private readonly List<Vector2> m_PieceUv = new List<Vector2>();

		private bool m_NeedUvPatch;
		private int m_LastBakeSignature = int.MinValue;
		private bool m_BakePending;
		private int m_BakeRunCount;

		#endregion

		#region 生命周期

		public override Texture mainTexture
			=> progressTexture != null ? progressTexture : base.mainTexture;

		protected override void OnEnable()
		{
			base.OnEnable();
			RebuildCurve();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			if (curveWidth < 0.01f) curveWidth = 0.01f;
			if (tessellationSegments < 2) tessellationSegments = 2;
			if (followSplineChanges && isActiveAndEnabled)
			{
				EditorApplication.delayCall += () =>
				{
					if (this != null && isActiveAndEnabled)
						RebuildCurve();
				};
			}
		}
#endif

		protected override void LateUpdate()
		{
			if (followSplineChanges)
				RebuildCurve();

			base.LateUpdate();

			// 只能在"刚刚重建过缓存"之后改写一次：
			// 改写后 uv0.y 就不再是生成器写的段内 t，重复改写会把映射套两遍
			if (m_NeedUvPatch)
			{
				m_NeedUvPatch = false;
				ApplyRibbonUv();
			}
		}

		#endregion

		#region API

		/// <summary>
		/// 把指定面片的头部推到 value（进度条前进就调这个）
		/// </summary>
		public void SetProgress(float value)
		{
			int index = fillRibbonIndex >= 0 ? fillRibbonIndex : 0;
			if (headTailPairs == null || headTailPairs.Length < (index + 1) * 2)
				return;

			headTailPairs[index * 2] = Mathf.Clamp01(value);
			RebuildCurve();
		}

		/// <summary>
		/// 读取指定面片的头部进度
		/// </summary>
		public float GetProgress()
		{
			int index = fillRibbonIndex >= 0 ? fillRibbonIndex : 0;
			if (headTailPairs == null || headTailPairs.Length < (index + 1) * 2)
				return 0f;
			return headTailPairs[index * 2];
		}

		/// <summary>
		/// 按当前样条与面片配置重建曲线
		/// </summary>
		public void RebuildCurve()
		{
			ClearAll();
			m_PieceUv.Clear();
			m_NeedUvPatch = false;

			if (splinePath == null || rectTransform == null)
				return;

			var spline = splinePath.xericSpline;
			if (spline == null || spline.Count < 2)
				return;

			// 锚点→世界的换算依赖矩形角点
			splinePath.UpdateRectDepend();
			var rect = rectTransform.rect;
			if (Mathf.Abs(rect.width) < 1e-6f || Mathf.Abs(rect.height) < 1e-6f)
				return;

			int pointCount = spline.Count;
			var anchors = new Vector2[pointCount];
			var outHandles = new Vector2[pointCount];
			var inHandles = new Vector2[pointCount];
			var colors = new Color[pointCount];
			var widths = new float[pointCount];

			int signature = pointCount;
			for (int i = 0; i < pointCount; i++)
			{
				anchors[i] = (Vector2)rectTransform.InverseTransformPoint(splinePath.GetPointPosition(i));
				outHandles[i] = (Vector2)rectTransform.InverseTransformPoint(splinePath.GetEndHandlePosition(i));
				inHandles[i] = (Vector2)rectTransform.InverseTransformPoint(splinePath.GetStartHandlePosition(i));
				colors[i] = ColorAt(spline, i);
				widths[i] = WidthAt(spline, i);

				unchecked
				{
					signature = signature * 31 + anchors[i].GetHashCode()
						+ outHandles[i].GetHashCode() * 7 + inHandles[i].GetHashCode() * 13;
				}
			}
			// 非均匀缩放矩形会改变世界空间弧长的比例，签名要把矩形尺寸也算进去
			unchecked
			{
				signature = signature * 31 + rect.width.GetHashCode() + rect.height.GetHashCode() * 3;
			}

			var request = new SplineRibbonBuilder.Request
			{
				anchors = anchors,
				outHandles = outHandles,
				inHandles = inHandles,
				pointColors = colors,
				pointWidths = widths,
				pointCount = pointCount,
				segmentCount = spline.useablePointCount,
				curveLoop = spline.CurveLoop,
				headTailPairs = headTailPairs,
				ribbonTints = ribbonTints,
				uvSpace = uvProgressSpace,
				chunksPerRibbon = chunksPerRibbon,
				progressToParametric = BuildMapping(spline, signature),
			};

			var pieces = SplineRibbonBuilder.Build(request);
			if (pieces.Count == 0)
				return;

			var ctrl = new Vector2[4];
			var pieceWidths = new float[4];

			foreach (var piece in pieces)
			{
				ctrl[0] = piece.c0; ctrl[1] = piece.c1; ctrl[2] = piece.c2; ctrl[3] = piece.c3;
				pieceWidths[0] = piece.w0; pieceWidths[1] = piece.w1;
				pieceWidths[2] = piece.w2; pieceWidths[3] = piece.w3;

				var entry = new CurveCacheEntry
				{
					startColor = piece.startColor,
					endColor = piece.endColor,
					tessellationSegments = PieceTessellation(piece),
				};

				AddCurve(entry, (Vector2[])ctrl.Clone(), (float[])pieceWidths.Clone());
				m_PieceUv.Add(new Vector2(piece.uvYStart, piece.uvYEnd));
			}

			m_NeedUvPatch = m_PieceUv.Count > 0;
		}

		#endregion

		#region 取数

		/// <summary>
		/// 造出"进度 → 参数 t"的映射。校正走样条烘焙的弧长重映射，
		/// 库侧要求先烘焙且长度不足时会抛，所以这里自己把关并回落。
		/// </summary>
		private Func<float, float> BuildMapping(XericRectSpline spline, int signature)
		{
			if (progressMapping != SplineProgressMapping.BakedDistance)
				return ParametricIdentity;

			if (!EnsureBaked(spline, signature))
				return ParametricIdentity;

			return ParametricFromBakedDistance;
		}

		private float ParametricIdentity(float progress) => progress;

		/// <summary>
		/// 进度→参数 t。两端是边界真值：进度 0 就是曲线起点、1 就是终点，
		/// 所以先把两端钉死再走重映射 —— 库里的弧长重映射原本按"采样点数"而不是"段数"
		/// 做分母，进度 1 会落在 (N-1)/N，{1,0} 的面片就永远短一小截贴不到末尾。
		/// 库侧已经修了分母，这里钉端点既是兜底也让旧 DLL 下也能立刻闭合。
		/// </summary>
		private float ParametricFromBakedDistance(float progress)
		{
			if (progress <= 0f) return 0f;
			if (progress >= 1f) return 1f;
			return Mathf.Clamp01(splinePath.RemappingTimeValue(progress));
		}

		/// <summary>
		/// 按 <see cref="bakeTrigger"/> 决定这一趟烘不烘，返回 false 表示当前用不了弧长校正。
		/// <code>
		/// 烘焙是 O(subdivide) 的采样：以前几何签名一变就烘，编辑器里拖动控制点等于每帧烘一次。
		/// OnEditEnd 的做法是"几何正在线上变"时先挂起、继续用上一次那份烘焙数据，
		/// 等下一趟签名不再变化（编辑停了）才补烘那一次。
		/// </code>
		/// </summary>
		private bool EnsureBaked(XericRectSpline spline, int signature)
		{
			int target = Mathf.Max(bakeSubdivide, spline.Count);
			bool geometryChanged = signature != m_LastBakeSignature;
			m_LastBakeSignature = signature;

			switch (bakeTrigger)
			{
				case SplineBakeTrigger.Manual:
					// 一次都不自动烘，等 BakeNow
					return HasUsableBake(spline, target);

				case SplineBakeTrigger.EveryRebuild:
					if (!geometryChanged && HasUsableBake(spline, target))
						return true;
					return RunBake(spline, target);

				default:
					if (geometryChanged)
					{
						// 几何正在线上变：这一趟不烘，也**不用**上一份烘焙数据。
						// 结构大改（清点重铺、切循环）后那份表对应的已经不是这条曲线了，
						// 拿它做校正会把面片排到错处 —— 宁可这一趟退回参数映射，
						// 等编辑停下来的下一趟烘完再恢复校正。
						m_BakePending = true;
						return false;
					}
					if (!m_BakePending)
						return HasUsableBake(spline, target);

					m_BakePending = false;
					return RunBake(spline, target);
			}
		}

		/// <summary>烘焙数据存在且长度够用（旧数据只要长度够就继续用，别再烘一次）</summary>
		private static bool HasUsableBake(XericRectSpline spline, int target)
			=> spline.BakeCount >= target;

		private bool RunBake(XericRectSpline spline, int target)
		{
			splinePath.BakePoint(target);
			m_BakeRunCount++;
			return spline.BakeCount >= target;
		}

		/// <summary>
		/// 手动烘一次（bakeTrigger 为 Manual 时用这个，或改完曲线想立刻精确时按）。
		/// 烘完直接重建，按下就能看见贴合结果。
		/// </summary>
		public void BakeNow()
		{
			if (splinePath == null || splinePath.xericSpline == null)
				return;

			var spline = splinePath.xericSpline;
			RunBake(spline, Mathf.Max(bakeSubdivide, spline.Count));
			m_BakePending = false;
			RebuildCurve();
		}

		private Color ColorAt(XericRectSpline spline, int index)
		{
			var gradient = spline.ColorGradient;
			if (gradient == null || index < 0 || index >= gradient.Count)
				return fallbackColor;

			var color = gradient[index];
			return color.a <= 0f ? fallbackColor : color;
		}

		private float WidthAt(XericRectSpline spline, int index)
		{
			switch (widthSource)
			{
				case SplineWidthSource.ScaleGradientY:
					return curveWidth * Mathf.Abs(GradientAt(spline.ScaleGradient, index, Vector3.one).y);
				case SplineWidthSource.FloatGradient:
					// 额外参数本来就是"每点多大"的语义时用它最直观；
					// 注意它默认全 0，不填就什么都画不出来
					return curveWidth * Mathf.Max(0f, GradientAt(spline.FloatGradient, index, 1f));
				default:
					return curveWidth;
			}
		}

		/// <summary>渐层可能比点数短（在 Inspector 里直接改 ControlPoint 长度就会这样），一律夹紧</summary>
		private static T GradientAt<T>(SplineGradientBase<T> gradient, int index, T fallback)
		{
			if (gradient == null || index < 0 || index >= gradient.Count)
				return fallback;
			return gradient[index];
		}

		private int PieceTessellation(RibbonPiece piece)
		{
			int tess = Mathf.RoundToInt(tessellationSegments * Mathf.Clamp01(piece.segmentFraction));
			return Mathf.Clamp(tess, 2, 512);
		}

		#endregion

		#region uv 重写

		/// <summary>
		/// 把生成器写在每个小块里的"段内 t"重映射成整条面片的 0→1。
		/// 默认材质只用到 uv0.x（边缘羽化），uv0.y 留给换上去的材质消费。
		/// </summary>
		private void ApplyRibbonUv()
		{
			if (m_PieceUv.Count == 0)
				return;

			for (int i = 0; i < m_Curves.Count && i < m_PieceUv.Count; i++)
			{
				var curve = m_Curves[i];
				var range = m_PieceUv[i];
				int end = curve.startVertexIndex + curve.vertexCount;
				if (end > m_MeshVertexCache.Count)
					end = m_MeshVertexCache.Count;

				for (int v = curve.startVertexIndex; v < end; v++)
				{
					var vert = m_MeshVertexCache[v];
					vert.uv0.y = Mathf.LerpUnclamped(range.x, range.y, vert.uv0.y);
					m_MeshVertexCache[v] = vert;
				}
			}

			SetVerticesDirty();
		}

		#endregion
	}

	/// <summary>进度值如何换算成曲线参数</summary>
	public enum SplineProgressMapping
	{
		/// <summary>进度直接当参数 t：段与段的视觉长度不等</summary>
		Parametric,
		/// <summary>进度按烘焙后的弧长重映射：画面上推进的长度和进度成正比</summary>
		BakedDistance,
	}

	/// <summary>弧长烘焙的触发时机（校正的数据源就是这份烘焙数据）</summary>
	public enum SplineBakeTrigger
	{
		/// <summary>从不自动烘，只认 BakeNow / Inspector 上的烘焙按钮</summary>
		Manual,
		/// <summary>几何停止变化后补烘一次：编辑器里拖动控制点不会逐帧烘</summary>
		OnEditEnd,
		/// <summary>几何一变就烘（每帧一变烘一次，贵；只在需要立刻精确时用）</summary>
		EveryRebuild,
	}

	/// <summary>带宽来源</summary>
	public enum SplineWidthSource
	{
		/// <summary>恒用 curveWidth</summary>
		Constant,
		/// <summary>curveWidth × 该点缩放渐层的 Y（渐层默认是 1，不填也等价于常量）</summary>
		ScaleGradientY,
		/// <summary>curveWidth × 该点的额外参数。默认全 0，不填什么都画不出来</summary>
		FloatGradient,
	}
}
