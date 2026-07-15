using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.Canvas
{
	/// <summary>
	/// 屏幕空间蓝图画布实现，基于 UGUI Canvas。
	/// <para>
	/// 所有坐标转换统一通过 <see cref="RectTransformUtility"/> 完成，
	/// 以 <see cref="_renderRoot"/> RectTransform 为坐标基准，
	/// 不依赖蓝图自身的 RectTransform 是否撑满全屏。
	/// </para>
	/// <para>
	/// 支持 ScreenSpaceOverlay / ScreenSpaceCamera / WorldSpace 三种渲染模式。
	/// 由外层的 Mask（__Bp_GridBackground）裁剪，确保蓝图节点不溢出视图区域。
	/// 缩放范围：0.1x ~ 3x。
	/// </para>
	/// </summary>
	public class ScreenSpaceBlueprintCanvas : BlueprintCanvasBase
	{
		private readonly UnityEngine.Canvas _unityCanvas;
		private readonly RectTransform _renderRoot;

		/// <summary>动态获取摄像机引用（不缓存），
		/// 自动适应 Canvas.renderMode 运行时的变化。</summary>
		private Camera WorldCamera => _unityCanvas != null ? _unityCanvas.worldCamera : null;

		/// <summary>
		/// 构造屏幕空间蓝图画布
		/// </summary>
		/// <param name="unityCanvas">所属的 Unity UGUI Canvas 组件</param>
		/// <param name="renderRoot">蓝图渲染根 RectTransform，所有蓝图层容器和背景挂载于此</param>
		public ScreenSpaceBlueprintCanvas(UnityEngine.Canvas unityCanvas, RectTransform renderRoot)
		{
			_unityCanvas = unityCanvas;
			_renderRoot = renderRoot;
		}

		/// <summary>
		/// 将屏幕坐标转换为画布坐标。
		/// 通过 <see cref="RectTransformUtility.ScreenPointToLocalPointInRectangle"/>
		/// 以 <see cref="_renderRoot"/> 为参照转换，
		/// 自动处理 Canvas 渲染模式和 RenderRoot 的偏移。
		/// </summary>
		public override Vector2 ScreenToCanvas(Vector2 screenPoint)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_renderRoot, screenPoint, WorldCamera, out Vector2 localPoint);
			return (localPoint - _panOffset) / _zoomLevel;
		}

		/// <summary>
		/// 将画布坐标转换为屏幕坐标。
		/// 先计算 RenderRoot 本地空间位置，再通过 RectTransformUtility 转换为屏幕坐标。
		/// </summary>
		public override Vector2 CanvasToScreen(Vector2 canvasPoint)
		{
			Vector2 local = canvasPoint * _zoomLevel + _panOffset;
			Vector3 world = _renderRoot.TransformPoint(local);
			return RectTransformUtility.WorldToScreenPoint(WorldCamera, world);
		}

		/// <summary>
		/// 将画布坐标转换为世界坐标。
		/// </summary>
		public override Vector3 CanvasToWorld(Vector2 canvasPoint)
		{
			Vector2 local = canvasPoint * _zoomLevel + _panOffset;
			return _renderRoot.TransformPoint(local);
		}

		/// <summary>
		/// 将世界坐标转换为画布坐标。
		/// 先转换为屏幕坐标，再通过 RectTransformUtility 转换为 RenderRoot 本地坐标。
		/// </summary>
		public override Vector2 WorldToCanvas(Vector3 worldPoint)
		{
			Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(WorldCamera, worldPoint);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_renderRoot, screenPoint, WorldCamera, out Vector2 localPoint);
			return (localPoint - _panOffset) / _zoomLevel;
		}

		/// <summary>
		/// 获取渲染根 Transform（蓝图层容器和背景的父级）
		/// </summary>
		public override Transform GetRootTransform()
		{
			return _renderRoot;
		}

		/// <summary>
		/// 底层 Unity Canvas 引用
		/// </summary>
		public UnityEngine.Canvas UnityCanvas
		{
			get { return _unityCanvas; }
		}

		/// <summary>
		/// Canvas 组件未被销毁，画布可正常使用。
		/// </summary>
		public override bool IsAlive => _unityCanvas != null;
	}
}
