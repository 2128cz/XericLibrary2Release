using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint
{
	/// <summary>
	/// 蓝图框选工具 —— 鼠标左键在空白区域拖拽，绘制矩形选框，
	/// 释放时计算框选范围内所有元素的交集并输出统计日志。
	/// <para>选框 UGUI Image 默认渲染在所有层之上（SetAsLastSibling）。</para>
	/// <para>框选结果保存在 <see cref="SelectedElements"/> 中供其他工具读取。</para>
	/// </summary>
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 60)]
	[BlueprintTheme("QuickGraph")]
	public class BlueprintBoxSelectTool : BlueprintTool
	{
		// ---------- 状态 ----------
		private bool _isBoxSelecting;
		private Vector2 _dragStartCanvas; // 按下时的画布坐标
		private Vector2 _dragEndCanvas;   // 当前的画布坐标

		// 选框 UI
		private GameObject _selectionBoxGo;
		private Image _selectionBoxImage;
		private RectTransform _selectionBoxRt;

		// ---------- 结果 ----------

		/// <summary>本次框选选中的元素列表。</summary>
		public List<IBlueprintElement> SelectedElements { get; } = new List<IBlueprintElement>();

		// ===== 鼠标按下 =====

		public override void OnPointerDown(Vector2 canvasPoint, int mouseButton)
		{
			if (Graph == null) return;
			if (mouseButton != 0) return; // 仅左键

			// 仅在空白区域开始框选（HitTest 无结果）
			var hit = Graph.HitTest(canvasPoint);
			if (hit != null) return;

			_isBoxSelecting = true;
			_dragStartCanvas = canvasPoint;
			_dragEndCanvas = canvasPoint;

			// 创建选框 UI
			CreateSelectionBox();
		}

		// ===== 拖拽 =====

		public override void OnPointerDrag(Vector2 canvasPoint, Vector2 delta, int mouseButton)
		{
			if (!_isBoxSelecting || mouseButton != 0) return;

			_dragEndCanvas = canvasPoint;
			UpdateSelectionBox();
		}

		// ===== 释放 =====

		public override void OnPointerUp(Vector2 canvasPoint, int mouseButton)
		{
			if (!_isBoxSelecting || mouseButton != 0) return;

			_dragEndCanvas = canvasPoint;
			FinishSelection();
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

			_selectionBoxGo = new GameObject("__Bp_BoxSelect", typeof(RectTransform), typeof(Image));
			_selectionBoxGo.hideFlags = HideFlags.HideAndDontSave;

			_selectionBoxRt = _selectionBoxGo.GetComponent<RectTransform>();
			_selectionBoxRt.SetParent(root, false);
			_selectionBoxRt.SetAsLastSibling(); // 渲染在最顶层

			_selectionBoxImage = _selectionBoxGo.GetComponent<Image>();
			_selectionBoxImage.color = new Color(0.2f, 0.5f, 1.0f, 0.15f); // 半透明蓝
			// 边框通过 Outline 或额外 Image 实现，直接使用透明填充 + 轮廓不好做，
			// 使用两个 Image：填充（当前） + 边框（另一个，1px 白色）
			// 为简化，在填充 Image 上挂一个 Outline 组件模拟边框
			var outline = _selectionBoxGo.AddComponent<Outline>();
			outline.effectColor = Color.white;
			outline.effectDistance = new Vector2(1, -1);
		}

		private void UpdateSelectionBox()
		{
			if (_selectionBoxRt == null || Graph?.Canvas == null) return;

			// 将画布坐标两角转为屏幕坐标
			var startScreen = Graph.Canvas.CanvasToScreen(_dragStartCanvas);
			var endScreen = Graph.Canvas.CanvasToScreen(_dragEndCanvas);

			// 计算 RectTransform 位置和尺寸（屏幕空间）
			var minX = Mathf.Min(startScreen.x, endScreen.x);
			var minY = Mathf.Min(startScreen.y, endScreen.y);
			var maxX = Mathf.Max(startScreen.x, endScreen.x);
			var maxY = Mathf.Max(startScreen.y, endScreen.y);

			_selectionBoxRt.anchoredPosition = new Vector2(minX, minY);
			_selectionBoxRt.sizeDelta = new Vector2(maxX - minX, maxY - minY);
		}

		private void FinishSelection()
		{
			_isBoxSelecting = false;
			UpdateSelectionBox();

			// 计算选框内的元素（画布坐标空间）
			SelectedElements.Clear();
			var min = new Vector2(
				Mathf.Min(_dragStartCanvas.x, _dragEndCanvas.x),
				Mathf.Min(_dragStartCanvas.y, _dragEndCanvas.y));
			var max = new Vector2(
				Mathf.Max(_dragStartCanvas.x, _dragEndCanvas.x),
				Mathf.Max(_dragStartCanvas.y, _dragEndCanvas.y));
			var selectRect = new Rect(min, max - min);

			foreach (var element in Graph.Elements)
			{
				if (element == null) continue;
				if (element.BoundingBox.Overlaps(selectRect))
				{
					SelectedElements.Add(element);
				}
			}

			// 销毁选框
			DestroySelectionBox();
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
