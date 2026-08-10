using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.Blueprint.QuickGraph;
namespace XericLibrary.Runtime.Blueprint
{
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 60)]
	public class BlueprintBoxSelectTool : BlueprintTool
	{
		private bool _selecting, _visible; private Vector2 _start, _end; private GameObject _box; private Image _image; private RectTransform _rect; private Outline _outline; private QuickGraphRenderStyleResolver _styles;
		public override void OnInitialize() { _styles = new QuickGraphRenderStyleResolver(Graph); _styles.Changed += _ => ApplyStyle(); }
		public override void OnConfigChanged() => _styles?.Refresh();
		public override void OnPointerDown(GraphInputTool sender, Vector2 point, int button) { if (button != 0 || Graph == null || Graph.HitTest(point) != null) return; _selecting = true; _visible = false; _start = _end = point; }
		public override void OnPointerDrag(GraphInputTool sender, Vector2 point, Vector2 delta, int button) { if (!_selecting || button != 0) return; _end = point; if (!_visible && Vector2.Distance(_start, _end) > _styles.Snapshot.Selection.DragThreshold) { _visible = true; Create(); } if (_visible) UpdateBox(); }
		public override void OnPointerUp(GraphInputTool sender, Vector2 point, int button) { if (!_selecting || button != 0) return; _end = point; if (_visible) Finish(); else SelectAt(_start); _selecting = false; }
		private void Create() { if (Graph?.Canvas == null) return; var root = Graph.Canvas.GetRootTransform(); if (root == null) return; DestroyBox(); _box = new GameObject("__Bp_BoxSelect", typeof(RectTransform), typeof(Image), typeof(Outline)); _box.hideFlags = HideFlags.HideAndDontSave; _rect = _box.GetComponent<RectTransform>(); _rect.SetParent(root, false); _rect.pivot = Vector2.zero; _rect.anchorMin = _rect.anchorMax = new Vector2(.5f, .5f); _rect.SetAsLastSibling(); _image = _box.GetComponent<Image>(); _outline = _box.GetComponent<Outline>(); ApplyStyle(); }
		private void ApplyStyle() { if (_image == null || _styles == null) return; var s = _styles.Snapshot.Selection; _image.color = s.FillColor; _image.raycastTarget = s.RaycastTarget; _outline.effectColor = s.OutlineColor; _outline.effectDistance = s.OutlineOffset; }
		private void UpdateBox() { if (_rect == null || Graph?.Canvas == null) return; var a = Graph.Canvas.CanvasToLocal(_start); var b = Graph.Canvas.CanvasToLocal(_end); var min = Vector2.Min(a, b); _rect.anchoredPosition = min; _rect.sizeDelta = Vector2.Max(a, b) - min; }
		private void Finish() { UpdateBox(); var min = Vector2.Min(_start, _end); var selector = BlueprintToolProvider.GetSelector(Graph); var result = new List<IBlueprintElement>(); selector.RectHitTest(Graph, new Rect(min, Vector2.Max(_start, _end) - min), result); selector.CommitMultiSelection(result); DestroyBox(); }
		private void SelectAt(Vector2 point) { var selector = BlueprintToolProvider.GetSelector(Graph); var hit = selector.HitTest(Graph, point); if (hit != null) selector.CommitSelection(hit); else selector.SwapToHistory(); }
		private void DestroyBox() { if (_box == null) return; if (Application.isPlaying) Object.Destroy(_box); else Object.DestroyImmediate(_box); _box = null; _image = null; _rect = null; _outline = null; }
		public override void OnDestroy() { _styles?.Dispose(); _styles = null; DestroyBox(); }
	}
}
