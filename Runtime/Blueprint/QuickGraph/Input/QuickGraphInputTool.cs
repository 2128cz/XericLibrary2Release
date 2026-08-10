using UnityEngine;
namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	[BlueprintTool(phase: ToolPhase.PreUpdate, order: 50)] [BlueprintTheme("QuickGraph")]
	public class QuickGraphInputTool : BlueprintTool
	{
		private QuickGraphRenderStyleResolver _styles; private BlueprintViewportController _viewport; private bool _dragging; private int _button = -1; private BlueprintViewportController Viewport => _viewport ?? (_viewport = BlueprintToolProvider.GetToolByType<BlueprintViewportController>(Graph));
		private Vector2 Pan => GraphInputTool.GetAdapter(Graph)?.PanDirection ?? Vector2.zero; private float Zoom => GraphInputTool.GetAdapter(Graph)?.ZoomAxis ?? 0f;
		public override void OnInitialize() { _styles = new QuickGraphRenderStyleResolver(Graph); _styles.Changed += _ => ApplyViewport(); ApplyViewport(); }
		public override void OnConfigChanged() => _styles?.Refresh(); private void ApplyViewport() { if (Viewport == null || _styles == null) return; Graph?.MarkDirty(); }
		public override void OnUpdate(float dt) { if (Graph?.Canvas == null || Viewport == null || _styles == null) return; var s = _styles.Snapshot.Input; ApplyViewport(); Viewport.TargetPanOffset += Pan * s.KeyboardPanSpeed * dt; var input = Zoom; if (!Mathf.Approximately(input, 0f)) { var old = Viewport.TargetZoomLevel; Viewport.TargetZoomLevel += input * s.KeyboardZoomSpeed; ApplyFocus(old); } }
		public override void OnPointerDown(GraphInputTool sender, Vector2 point, int button) { if (_styles != null && button == _styles.Snapshot.Input.PanMouseButton) { _dragging = true; _button = button; } }
		public override void OnPointerUp(GraphInputTool sender, Vector2 point, int button) { if (_dragging && _button == button) { _dragging = false; _button = -1; } }
		public override void OnPointerDrag(GraphInputTool sender, Vector2 point, Vector2 delta, int button) { if (_dragging && _button == button && Viewport != null) { Viewport.TargetPanOffset += (sender?.PointerLocalDelta ?? Vector2.zero) * _styles.Snapshot.Input.DragPanSpeed; Graph.MarkDirty(); } }
		public override void OnScroll(GraphInputTool sender, Vector2 point, Vector2 delta) { if (Viewport == null || _styles == null) return; var old = Viewport.TargetZoomLevel; Viewport.TargetZoomLevel += delta.y / _styles.Snapshot.Input.ZoomDivisor; if (!Mathf.Approximately(old, Viewport.TargetZoomLevel)) { Viewport.TargetPanOffset += point * (old - Viewport.TargetZoomLevel); Graph.MarkDirty(); } }
		private void ApplyFocus(float old) { if (Viewport == null || Graph?.Canvas == null) return; var cursor = Graph.Canvas.ScreenToCanvas(BlueprintUGUIInputManager.MousePosition); Viewport.TargetPanOffset += cursor * (old - Viewport.TargetZoomLevel); }
		public override void OnDestroy() { _styles?.Dispose(); _styles = null; }
	}
}
