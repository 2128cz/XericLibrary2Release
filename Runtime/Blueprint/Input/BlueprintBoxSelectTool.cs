using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图框选工具 —— 鼠标左键在空白区域拖拽，绘制矩形选框，
	/// 释放时计算框选范围内所有元素的交集并输出统计日志。
	/// <para>选框 UGUI Image 默认渲染在所有层之上（SetAsLastSibling）。</para>
	/// <para>框选结果通过 <see cref="BlueprintToolProvider.GetSelector"/>(Graph).<see cref="SelectorHandle.CurrentSelection"/> 读取。</para>
	/// <para>
	/// 选框仅在拖拽距离超过 <see cref="BoxSelectThreshold"/> 后才显示，
	/// 避免误触显示闪烁。
	/// </para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 60)]
	public class BlueprintBoxSelectTool : BlueprintTool
	{
		// ---------- 状态 ----------
		private bool _isBoxSelecting;
		private Vector2 _dragStartCanvas; // 按下时的画布坐标
		private Vector2 _dragEndCanvas;   // 当前的画布坐标

		/// <summary>选框 GameObject 是否已创建并可见（超过阈值后）</summary>
		private bool _boxVisible;

		// 选框 UI
		private GameObject _selectionBoxGo;
		private Image _selectionBoxImage;
		private RectTransform _selectionBoxRt;

		// ---------- 配置 ----------

		/// <summary>框选拖拽触发阈值（画布单位）。拖拽距离超过此值后才显示选框并开始框选。</summary>
		public float BoxSelectThreshold = 10f;

		// ---------- 结果 ----------

		/// <summary>结果：通过 Provider.Selector.CurrentSelection 读取框选结果。</summary>

		// ===== 鼠标按下 =====

		public override void OnPointerDown(GraphInputTool sender, Vector2 canvasPoint, int mouseButton)
		{
			if (Graph == null) return;
			if (mouseButton != 0) return; // 仅左键

			// 仅在空白区域开始框选（HitTest 无结果）
			var hit = Graph.HitTest(canvasPoint);
			if (hit != null) return;

			_isBoxSelecting = true;
			_boxVisible = false;
			_dragStartCanvas = canvasPoint;
			_dragEndCanvas = canvasPoint;
		}

		// ===== 拖拽 =====

		public override void OnPointerDrag(GraphInputTool sender, Vector2 canvasPoint, Vector2 delta, int mouseButton)
		{
			if (!_isBoxSelecting || mouseButton != 0) return;

			_dragEndCanvas = canvasPoint;

			// 检查拖拽距离是否超过阈值
			float threshold = BoxSelectThreshold;
			float dragDist = Vector2.Distance(_dragStartCanvas, _dragEndCanvas);

			if (!_boxVisible && dragDist > threshold)
			{
				// 首次超过阈值 → 创建选框
				_boxVisible = true;
				CreateSelectionBox();
			}

			if (_boxVisible)
			{
				UpdateSelectionBox();
			}
		}

		// ===== 释放 =====

		public override void OnPointerUp(GraphInputTool sender, Vector2 canvasPoint, int mouseButton)
		{
			if (!_isBoxSelecting || mouseButton != 0) return;

			_dragEndCanvas = canvasPoint;

			if (_boxVisible)
			{
				FinishSelection();
			}
			else
			{
				// 未超过阈值 → 点击行为（框选起点处的元素）
				SelectAtPoint(_dragStartCanvas);
			}

			_isBoxSelecting = false;
		}

		// ===== 销毁 =====

		public override void OnDestroy()
		{
			DestroySelectionBox();
		}

		// ===== 选框 UI =====

		private void CreateSelectionBox()
		{
			if (Graph?.Canvas == null) return;
			var root = Graph.Canvas.GetRootTransform();
			if (root == null) return;

			// 销毁已有的选框（应对重复点击）
			DestroySelectionBox();

			_selectionBoxGo = new GameObject("__Bp_BoxSelect", typeof(RectTransform), typeof(Image));
			_selectionBoxGo.hideFlags = HideFlags.HideAndDontSave;

			_selectionBoxRt = _selectionBoxGo.GetComponent<RectTransform>();
			_selectionBoxRt.SetParent(root, false);

			// 枢轴设为左下角；锚点居中对齐 RenderRoot 枢轴（与 CanvasToLocal 空间一致）
			_selectionBoxRt.pivot = Vector2.zero;
			_selectionBoxRt.anchorMin = new Vector2(0.5f, 0.5f);
			_selectionBoxRt.anchorMax = new Vector2(0.5f, 0.5f);

			_selectionBoxRt.SetAsLastSibling(); // 渲染在最顶层

			_selectionBoxImage = _selectionBoxGo.GetComponent<Image>();
			_selectionBoxImage.color = new Color(0.2f, 0.5f, 1.0f, 0.15f);
			var outline = _selectionBoxGo.AddComponent<Outline>();
			outline.effectColor = Color.white;
			outline.effectDistance = new Vector2(1, -1);
		}

		private void UpdateSelectionBox()
		{
			if (_selectionBoxRt == null || Graph?.Canvas == null) return;

			// 将画布坐标转为 RenderRoot 本地空间坐标（CanvasToLocal）
			var startLocal = Graph.Canvas.CanvasToLocal(_dragStartCanvas);
			var endLocal = Graph.Canvas.CanvasToLocal(_dragEndCanvas);

			// 计算 RectTransform 位置和尺寸（RenderRoot 本地空间）
			var minX = Mathf.Min(startLocal.x, endLocal.x);
			var minY = Mathf.Min(startLocal.y, endLocal.y);
			var maxX = Mathf.Max(startLocal.x, endLocal.x);
			var maxY = Mathf.Max(startLocal.y, endLocal.y);

			_selectionBoxRt.anchoredPosition = new Vector2(minX, minY);
			_selectionBoxRt.sizeDelta = new Vector2(maxX - minX, maxY - minY);
		}

		private void FinishSelection()
		{
			UpdateSelectionBox();

			// 计算选框内的元素（画布坐标空间）
			var selectList = new System.Collections.Generic.List<IBlueprintElement>();
			var min = new Vector2(
				Mathf.Min(_dragStartCanvas.x, _dragEndCanvas.x),
				Mathf.Min(_dragStartCanvas.y, _dragEndCanvas.y));
			var max = new Vector2(
				Mathf.Max(_dragStartCanvas.x, _dragEndCanvas.x),
				Mathf.Max(_dragStartCanvas.y, _dragEndCanvas.y));
			var selectRect = new Rect(min, max - min);

			var selector = BlueprintToolProvider.GetSelector(Graph);
			selector.RectHitTest(Graph, selectRect, selectList);

			// 通过当前 Graph 的 SelectorHandle 提交多选
			selector.CommitMultiSelection(selectList);

			// 销毁选框
			DestroySelectionBox();
		}

		private void SelectAtPoint(Vector2 canvasPoint)
		{
			if (Graph == null) return;

			var selector = BlueprintToolProvider.GetSelector(Graph);
			var hit = selector.HitTest(Graph, canvasPoint);
			if (hit != null)
				selector.CommitSelection(hit);
			else
				selector.SwapToHistory();
		}

		private void DestroySelectionBox()
		{
			if (_selectionBoxGo != null)
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
					Object.DestroyImmediate(_selectionBoxGo);
				else
#endif
					Object.Destroy(_selectionBoxGo);
				_selectionBoxGo = null;
				_selectionBoxRt = null;
				_selectionBoxImage = null;
			}
		}
	}
}
