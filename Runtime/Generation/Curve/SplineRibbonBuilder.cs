using System;
using System.Collections.Generic;

using UnityEngine;

namespace XericLibrary.Runtime.UIGraph
{
	/// <summary>
	/// 一条面片切出来的一个贝塞尔小块：几何、逐点宽度、端点颜色、uv 进度区间。
	/// 位置都在 Graphic 的局部平面内，与渲染器同一坐标约定。
	/// </summary>
	internal struct RibbonPiece
	{
		public Vector2 c0;
		public Vector2 c1;
		public Vector2 c2;
		public Vector2 c3;
		public float w0;
		public float w1;
		public float w2;
		public float w3;
		public Color32 startColor;
		public Color32 endColor;
		public float uvYStart;
		public float uvYEnd;
		public int ribbonIndex;
		/// <summary>本小块占它所在样条段的比例，用来折算细分数</summary>
		public float segmentFraction;
	}

	/// <summary>面片上 uv0.y 代表什么进度</summary>
	public enum SplineUvProgressSpace
	{
		/// <summary>每条面片各自 0→1：尾部 0、头部 1，效果跟着已填充部分伸缩</summary>
		RibbonLocal,
		/// <summary>钉在整条样条上：贴图位置不随进度变化</summary>
		SplineGlobal,
	}

	/// <summary>
	/// 样条面片切分器：把若干条 (头部,尾部) 面片切成逐段贝塞尔小块。
	/// <code>
	/// 纯函数：不碰 MonoBehaviour，也不碰 Debug.Log —— 后者在 Unity 之外会抛
	/// "ECall could not be defined"，那样就没法在离线门里直接跑出数来验证。
	/// 几何切分用贝塞尔在 [t0,t1] 上的精确限制（Restrict）；
	/// t0=0,t1=1 时逐点还原原始四个控制点，所以整段不产生任何漂移。
	/// </code>
	/// </summary>
	internal static class SplineRibbonBuilder
	{
		internal struct Request
		{
			/// <summary>各控制点锚点（局部坐标），长度 = pointCount</summary>
			public Vector2[] anchors;
			/// <summary>各点的出手柄（局部坐标世界式，即已含锚点）</summary>
			public Vector2[] outHandles;
			/// <summary>各点的入手柄（局部坐标世界式，即已含锚点）</summary>
			public Vector2[] inHandles;
			/// <summary>各点颜色</summary>
			public Color[] pointColors;
			/// <summary>各点宽度</summary>
			public float[] pointWidths;

			public int pointCount;
			public int segmentCount;
			public bool curveLoop;

			/// <summary>偶数下标=头部、奇数下标=尾部，成对；取值 [0,1]</summary>
			public float[] headTailPairs;
			/// <summary>可空；与面片同序时逐条叠乘到样条颜色上</summary>
			public Color32[] ribbonTints;

			/// <summary>进度 → 参数 t（距离校正就落在这个回调里）</summary>
			public Func<float, float> progressToParametric;

			public SplineUvProgressSpace uvSpace;
			/// <summary>每条面片至少切成几块；决定 uv0.y 与视觉距离对齐的精度</summary>
			public int chunksPerRibbon;
		}

		/// <summary>
		/// 切分所有面片。无效输入返回空表而不是抛异常。
		/// </summary>
		internal static List<RibbonPiece> Build(Request req)
		{
			var pieces = new List<RibbonPiece>();

			if (req.anchors == null || req.outHandles == null || req.inHandles == null)
				return pieces;
			if (req.headTailPairs == null || req.progressToParametric == null)
				return pieces;
			if (req.pointCount < 2 || req.segmentCount < 1 || req.headTailPairs.Length < 2)
				return pieces;

			int chunks = Mathf.Max(1, req.chunksPerRibbon);
			int ribbonCount = req.headTailPairs.Length / 2;

			for (int r = 0; r < ribbonCount; r++)
			{
				float head = Mathf.Clamp01(req.headTailPairs[r * 2]);
				float tail = Mathf.Clamp01(req.headTailPairs[r * 2 + 1]);
				if (Mathf.Abs(head - tail) <= 1e-6f)
					continue;

				BuildRibbon(req, r, tail, head, chunks, pieces);
			}

			return pieces;
		}

		/// <summary>
		/// 面片沿路径的一段连续进度区间（起点进度、终点进度、起终点的参数 t）
		/// </summary>
		private struct Span
		{
			public float fromProgress;
			public float toProgress;
			public float fromParam;
			public float toParam;
			public float ArcLength;
		}

		private static void BuildRibbon(Request req, int ribbonIndex, float tail, float head,
			int chunks, List<RibbonPiece> output)
		{
			var spans = ResolveSpans(req, tail, head);
			if (spans.Count == 0)
				return;

			float segments = req.segmentCount;

			Color32 tint = (req.ribbonTints != null && ribbonIndex < req.ribbonTints.Length)
				? req.ribbonTints[ribbonIndex]
				: new Color32(255, 255, 255, 255);
			bool global = req.uvSpace == SplineUvProgressSpace.SplineGlobal;

			int firstOfRibbon = output.Count;
			// 与本次新增的 piece 一一对应的实测弧长：RibbonLocal 的 uv 分母就用这一份加总
			var arcs = new List<float>();

			foreach (var span in spans)
			{
				if (span.ArcLength <= 0f)
					continue;

				var cuts = CollectCuts(req, span);
				int intervalCount = cuts.Count - 1;
				float spanWalked = 0f;

				for (int i = 0; i < intervalCount; i++)
				{
					float a = cuts[i];
					float b = cuts[i + 1];
					float arc = MeasureArc(req, a, b);

					int seg = Mathf.Clamp(Mathf.FloorToInt(a * segments), 0, req.segmentCount - 1);
					float localA = Mathf.Clamp01(a * segments - seg);
					float localB = Mathf.Clamp01(b * segments - seg);
					if (localB - localA <= 1e-6f)
						continue;

					Vector2 p0, p1, p2, p3;
					GetSegment(req, seg, out p0, out p1, out p2, out p3);
					float q0, q1, q2, q3;
					GetWidthSegment(req, seg, out q0, out q1, out q2, out q3);

					var piece = new RibbonPiece();
					piece.ribbonIndex = ribbonIndex;
					piece.segmentFraction = localB - localA;

					Restrict(p0, p1, p2, p3, localA, localB,
						out piece.c0, out piece.c1, out piece.c2, out piece.c3);
					Restrict(q0, q1, q2, q3, localA, localB,
						out piece.w0, out piece.w1, out piece.w2, out piece.w3);

					piece.startColor = Tint(ColorAt(req, a), tint);
					piece.endColor = Tint(ColorAt(req, b), tint);

					// SplineGlobal：落在路径上的绝对进度，环绕时在 1→0 处天然断开。
					// spanWalked 只在这里用 —— 它要的是"在本区间内走了多长"。
					// RibbonLocal 的 uv 留给下面那一趟，用和分子同一份来源的弧长来分配。
					if (global)
					{
						piece.uvYStart = ProgressAt(span, spanWalked / span.ArcLength);
						piece.uvYEnd = ProgressAt(span, (spanWalked + arc) / span.ArcLength);
						spanWalked += arc;
						output.Add(piece);
						continue;
					}

					spanWalked += arc;
					output.Add(piece);
					arcs.Add(arc);
				}
			}

			if (global)
				return;

			// RibbonLocal：整条面片（含环绕的两段）合成一个连续的 0→1。
			// <code>
			// 分子分母必须同源。以前分母用 MeasureSpanArc（整个区间一次量完的折线长），
			// 分子用逐小块的 MeasureArc（切得更细、量得更长），两者能差 0.2%，
			// 于是中间的小块算出 uv > 1，末片又被钉成 1 ⇒ 出现"某片 uv 非增"。
			// 更糟的是原先靠"emit 计数 == 预计块数"来认最后一块：被跳过的退化小块
			// 也已经计过一次数 ⇒ 1 被压到中间某块上，下一块又从 0.6 起重来 ——
			// 真机门量到的"跨尾面片 uv 回跳 4 次"就是这么来的。
			// 现在出完片再按这份弧长统一分配，两端自然落在 0 与 1，中间跳过几块都不影响单调。
			// </code>
			float totalArc = 0f;
			for (int i = 0; i < arcs.Count; i++)
				totalArc += arcs[i];

			if (arcs.Count == 0 || totalArc <= 0f)
			{
				output.RemoveRange(firstOfRibbon, output.Count - firstOfRibbon);
				return;
			}

			float cursor = 0f;
			for (int i = 0; i < arcs.Count; i++)
			{
				int pieceIndex = firstOfRibbon + i;
				var piece = output[pieceIndex];
				float from = cursor;
				cursor += arcs[i];

				piece.uvYStart = from / totalArc;
				piece.uvYEnd = i == arcs.Count - 1 ? 1f : cursor / totalArc;
				output[pieceIndex] = piece;
			}
		}

		/// <summary>
		/// 把一对 (头部,尾部) 展开成沿路径的区间。
		/// head &gt; tail 是一个区间；循环样条上 head &lt; tail 表示"跨过终点绕回去"，
		/// 拆成 [tail,1] 与 [0,head] 两个区间，面片仍然连续。非循环样条不做环绕，直接不出片。
		/// </summary>
		private static List<Span> ResolveSpans(Request req, float tail, float head)
		{
			var spans = new List<Span>(req.curveLoop ? 2 : 1);

			if (head > tail)
			{
				AddSpan(spans, req, tail, head);
				return spans;
			}

			if (!req.curveLoop)
				return spans;

			AddSpan(spans, req, tail, 1f);
			AddSpan(spans, req, 0f, head);
			return spans;
		}

		private static void AddSpan(List<Span> spans, Request req, float fromProgress, float toProgress)
		{
			if (toProgress - fromProgress <= 1e-6f)
				return;

			// 退化：校正后端点仍然重合（长度为零的样条会出现），这个区间不出
			float fromParam = Mathf.Clamp01(req.progressToParametric(fromProgress));
			float toParam = Mathf.Clamp01(req.progressToParametric(toProgress));
			if (toParam - fromParam <= 1e-6f)
				return;

			var span = new Span
			{
				fromProgress = fromProgress,
				toProgress = toProgress,
				fromParam = fromParam,
				toParam = toParam,
			};
			span.ArcLength = MeasureSpanArc(req, fromParam, toParam);
			spans.Add(span);
		}

		/// <summary>
		/// 跨段的区间弧长：先按样条段边界切开，再逐段量。
		/// MeasureArc 只能量"同一段内"的两点，直接拿来量整段会静默少算。
		/// </summary>
		private static float MeasureSpanArc(Request req, float a, float b)
		{
			float segments = req.segmentCount;
			float sum = 0f;
			float cursor = a;

			while (cursor < b - 1e-6f)
			{
				int seg = Mathf.Clamp(Mathf.FloorToInt(cursor * segments), 0, req.segmentCount - 1);
				float nextBoundary = (seg + 1f) / segments;
				float stop = Mathf.Min(b, nextBoundary);
				sum += MeasureArc(req, cursor, stop);
				cursor = stop;
			}
			return sum;
		}

		/// <summary>区间内按弧长占比折算出该处的进度值</summary>
		private static float ProgressAt(Span span, float fraction)
		{
			return Mathf.Clamp01(span.fromProgress + (span.toProgress - span.fromProgress) * fraction);
		}

		/// <summary>
		/// 切分点：等进度网格 + 所有落在区间内的样条段边界，去重后升序。
		/// 有了段边界，每个小块必然完整落在同一段贝塞尔里。
		/// </summary>
		private static List<float> CollectCuts(Request req, Span span)
		{
			int chunks = Mathf.Max(1, req.chunksPerRibbon);
			var cuts = new List<float>(chunks + req.segmentCount + 2) { span.fromParam, span.toParam };

			for (int s = 1; s < req.segmentCount; s++)
			{
				float boundary = (float)s / req.segmentCount;
				if (boundary > span.fromParam && boundary < span.toParam)
					cuts.Add(boundary);
			}

			for (int k = 1; k < chunks; k++)
			{
				float progress = span.fromProgress
					+ (span.toProgress - span.fromProgress) * k / chunks;
				cuts.Add(Mathf.Clamp01(req.progressToParametric(progress)));
			}

			cuts.Sort();

			var unique = new List<float>(cuts.Count);
			for (int i = 0; i < cuts.Count; i++)
			{
				if (unique.Count > 0 && cuts[i] - unique[unique.Count - 1] <= 1e-6f)
					continue;
				unique.Add(cuts[i]);
			}
			return unique;
		}

		/// <summary>段 seg 的四个控制点：本点锚、本点出手柄、下一点入手柄、下一点锚</summary>
		private static void GetSegment(Request req, int seg,
			out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
		{
			int next = NextPoint(req, seg);
			p0 = req.anchors[seg];
			p1 = req.outHandles[seg];
			p2 = req.inHandles[next];
			p3 = req.anchors[next];
		}

		/// <summary>
		/// 宽度也走同一条贝塞尔：本段两端取本点宽、下段两端取下点宽。
		/// 与几何用同一个 [t0,t1] 限制，带宽过渡才不会和形状错位。
		/// </summary>
		private static void GetWidthSegment(Request req, int seg,
			out float w0, out float w1, out float w2, out float w3)
		{
			int next = NextPoint(req, seg);
			w0 = WidthAt(req, seg);
			w1 = WidthAt(req, seg);
			w2 = WidthAt(req, next);
			w3 = WidthAt(req, next);
		}

		private static int NextPoint(Request req, int seg)
		{
			return req.curveLoop
				? PositiveMod(seg + 1, req.pointCount)
				: Mathf.Min(seg + 1, req.pointCount - 1);
		}

		private static float WidthAt(Request req, int index)
		{
			if (req.pointWidths == null || req.pointWidths.Length == 0)
				return 0f;
			return req.pointWidths[Mathf.Clamp(index, 0, req.pointWidths.Length - 1)];
		}

		/// <summary>按渐层取色；索引越界一律夹紧，绝不裸索引（渐层列表可能比点少）</summary>
		private static Color ColorAt(Request req, float parametric)
		{
			if (req.pointColors == null || req.pointColors.Length == 0)
				return Color.white;

			int count = req.pointColors.Length;
			// 与 XericRectSpline.GetDeltaT 同一套映射（那个是 internal，Release 侧够不到），
			// 所以取色和几何形状保持同相
			float usable = req.curveLoop ? count : Mathf.Max(1, count - 1);
			float scaled = Mathf.Clamp01(parametric) * usable;
			int index = Mathf.FloorToInt(scaled);
			float dt = Mathf.Clamp01(scaled - index);

			int a = req.curveLoop ? PositiveMod(index, count) : Mathf.Clamp(index, 0, count - 1);
			int b = req.curveLoop ? PositiveMod(index + 1, count) : Mathf.Clamp(index + 1, 0, count - 1);
			return Color.Lerp(req.pointColors[a], req.pointColors[b], dt);
		}

		private static Color32 Tint(Color baseColor, Color32 tint)
		{
			var t = (Color)tint;
			var r = new Color(baseColor.r * t.r, baseColor.g * t.g,
				baseColor.b * t.b, baseColor.a * t.a);
			return r;
		}

		private static int PositiveMod(int value, int modulus)
		{
			if (modulus <= 0)
				return 0;
			return ((value % modulus) + modulus) % modulus;
		}

		// ---------------------------------------------------------------- 贝塞尔裁剪

		/// <summary>
		/// 三次贝塞尔在 [t0,t1] 上的精确限制：
		/// Q0=B(t0)，Q1=B(t0)+(t1-t0)/3·B'(t0)，Q2=B(t1)-(t1-t0)/3·B'(t1)，Q3=B(t1)。
		/// </summary>
		internal static void Restrict(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
			float t0, float t1, out Vector2 q0, out Vector2 q1, out Vector2 q2, out Vector2 q3)
		{
			float span = t1 - t0;
			q0 = Eval(p0, p1, p2, p3, t0);
			q1 = q0 + Derivative(p0, p1, p2, p3, t0) * (span / 3f);
			q3 = Eval(p0, p1, p2, p3, t1);
			q2 = q3 - Derivative(p0, p1, p2, p3, t1) * (span / 3f);
		}

		internal static void Restrict(float p0, float p1, float p2, float p3,
			float t0, float t1, out float q0, out float q1, out float q2, out float q3)
		{
			float span = t1 - t0;
			q0 = Eval(p0, p1, p2, p3, t0);
			q1 = q0 + Derivative(p0, p1, p2, p3, t0) * (span / 3f);
			q3 = Eval(p0, p1, p2, p3, t1);
			q2 = q3 - Derivative(p0, p1, p2, p3, t1) * (span / 3f);
		}

		internal static Vector2 Eval(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			var a = Vector2.Lerp(p0, p1, t);
			var b = Vector2.Lerp(p1, p2, t);
			var c = Vector2.Lerp(p2, p3, t);
			var d = Vector2.Lerp(a, b, t);
			var e = Vector2.Lerp(b, c, t);
			return Vector2.Lerp(d, e, t);
		}

		internal static float Eval(float p0, float p1, float p2, float p3, float t)
		{
			var a = Mathf.Lerp(p0, p1, t);
			var b = Mathf.Lerp(p1, p2, t);
			var c = Mathf.Lerp(p2, p3, t);
			var d = Mathf.Lerp(a, b, t);
			var e = Mathf.Lerp(b, c, t);
			return Mathf.Lerp(d, e, t);
		}

		/// <summary>导数 = 3 × 一阶差分控制点的二次贝塞尔</summary>
		internal static Vector2 Derivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			var d0 = p1 - p0;
			var d1 = p2 - p1;
			var d2 = p3 - p2;
			var a = Vector2.Lerp(d0, d1, t);
			var b = Vector2.Lerp(d1, d2, t);
			return Vector2.Lerp(a, b, t) * 3f;
		}

		internal static float Derivative(float p0, float p1, float p2, float p3, float t)
		{
			var d0 = p1 - p0;
			var d1 = p2 - p1;
			var d2 = p3 - p2;
			var a = Mathf.Lerp(d0, d1, t);
			var b = Mathf.Lerp(d1, d2, t);
			return Mathf.Lerp(a, b, t) * 3f;
		}

		/// <summary>小块弧长（折线近似，够 uv 分配用）</summary>
		private static float MeasureArc(Request req, float a, float b)
		{
			const int samples = 5;
			int seg = Mathf.Clamp(Mathf.FloorToInt(a * req.segmentCount), 0, req.segmentCount - 1);

			Vector2 p0, p1, p2, p3;
			GetSegment(req, seg, out p0, out p1, out p2, out p3);

			float la = Mathf.Clamp01(a * req.segmentCount - seg);
			float lb = Mathf.Clamp01(b * req.segmentCount - seg);

			var last = Eval(p0, p1, p2, p3, la);
			float sum = 0f;
			for (int i = 1; i <= samples; i++)
			{
				var next = Eval(p0, p1, p2, p3, Mathf.Lerp(la, lb, (float)i / samples));
				sum += Vector2.Distance(last, next);
				last = next;
			}
			return sum;
		}
	}
}
