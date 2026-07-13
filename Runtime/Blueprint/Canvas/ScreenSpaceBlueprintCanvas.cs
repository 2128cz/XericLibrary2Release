using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.Canvas
{
	/// <summary>
	/// 屏幕空间蓝图画布实现，基于 UGUI Canvas。
	/// <code>
	/// 使用 RectTransformUtility 处理屏幕坐标和画布坐标之间的转换。
	/// 缩放范围：0.1x ~ 3x。
	/// </code>
	/// </summary>
	public class ScreenSpaceBlueprintCanvas : BlueprintCanvasBase
	{
		private readonly UnityEngine.Canvas _unityCanvas;
		private readonly RectTransform _rectTransform;

		/// <summary>
		/// 构造屏幕空间蓝图画布
		/// </summary>
		/// <param name="unityCanvas">Unity UGUI Canvas 组件</param>
		public ScreenSpaceBlueprintCanvas(UnityEngine.Canvas unityCanvas)
		{
			_unityCanvas = unityCanvas;
			_rectTransform = unityCanvas.GetComponent<RectTransform>();
		}

		/// <summary>
		/// 将屏幕坐标转换为画布坐标
		/// </summary>
		/// <param name="screenPoint">屏幕空间的坐标点</param>
		/// <returns>画布空间的坐标点</returns>
		public override Vector2 ScreenToCanvas(Vector2 screenPoint)
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_rectTransform, screenPoint, _unityCanvas.worldCamera, out Vector2 localPoint);
			return (localPoint - _panOffset) / _zoomLevel;
		}

		/// <summary>
		/// 将画布坐标转换为屏幕坐标
		/// </summary>
		/// <param name="canvasPoint">画布空间的坐标点</param>
		/// <returns>屏幕空间的坐标点</returns>
		public override Vector2 CanvasToScreen(Vector2 canvasPoint)
		{
			Vector2 local = canvasPoint * _zoomLevel + _panOffset;
			Vector3 world = _rectTransform.TransformPoint(local);
			if (_unityCanvas.worldCamera != null)
			{
				return _unityCanvas.worldCamera.WorldToScreenPoint(world);
			}

			return world;
		}

		/// <summary>
		/// 将画布坐标转换为世界坐标
		/// <code>
		/// 屏幕空间画布的世界空间转换依赖于 Canvas 的渲染模式：
		/// Screen Space - Overlay 时直接使用 localPoint 作为屏幕坐标；
		/// Screen Space - Camera 时通过 camera 进行坐标转换。
		/// </code>
		/// </summary>
		/// <param name="canvasPoint">画布空间的坐标点</param>
		/// <returns>世界空间的坐标点</returns>
		public override Vector3 CanvasToWorld(Vector2 canvasPoint)
		{
			Vector2 local = canvasPoint * _zoomLevel + _panOffset;
			Vector3 world = _rectTransform.TransformPoint(local);
			if (_unityCanvas.worldCamera != null)
			{
				Vector3 screenPoint = _unityCanvas.worldCamera.WorldToScreenPoint(world);
				return _unityCanvas.worldCamera.ScreenToWorldPoint(screenPoint);
			}

			return world;
		}

		/// <summary>
		/// 将世界坐标转换为画布坐标
		/// </summary>
		/// <param name="worldPoint">世界空间的坐标点</param>
		/// <returns>画布空间的坐标点</returns>
		public override Vector2 WorldToCanvas(Vector3 worldPoint)
		{
			if (_unityCanvas.worldCamera != null)
			{
				Vector3 screenPoint = _unityCanvas.worldCamera.WorldToScreenPoint(worldPoint);
				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					_rectTransform, screenPoint, _unityCanvas.worldCamera, out Vector2 localPoint);
				return (localPoint - _panOffset) / _zoomLevel;
			}

			Vector3 local = _rectTransform.InverseTransformPoint(worldPoint);
			return (new Vector2(local.x, local.y) - _panOffset) / _zoomLevel;
		}

		/// <summary>
		/// 获取渲染根 Transform（UGUI Canvas 的 RectTransform）
		/// </summary>
		public override Transform GetRootTransform()
		{
			return _rectTransform;
		}

		/// <summary>
		/// 底层 Unity Canvas 引用
		/// </summary>
		public UnityEngine.Canvas UnityCanvas
		{
			get { return _unityCanvas; }
		}
	}
}
