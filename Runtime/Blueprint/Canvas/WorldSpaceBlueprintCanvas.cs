using UnityEngine;

namespace XericLibrary.Runtime.Blueprint.Canvas
{
	/// <summary>
	/// 世界空间蓝图画布实现，用于 Sprite 渲染等世界空间场景。
	/// <code>
	/// 通过相机射线与画布平面的交点实现屏幕坐标到画布坐标的转换。
	/// 缩放范围：0.05x ~ 5x。
	/// </code>
	/// </summary>
	public class WorldSpaceBlueprintCanvas : BlueprintCanvasBase
	{
		private readonly Transform _canvasTransform;

		/// <summary>
		/// 构造世界空间蓝图画布
		/// </summary>
		/// <param name="canvasTransform">画布的 Transform 组件</param>
		/// <param name="camera">使用的摄像机，如果为 null 则使用 Camera.main</param>
		public WorldSpaceBlueprintCanvas(Transform canvasTransform, Camera camera = null)
		{
			_canvasTransform = canvasTransform;
			SetCamera(camera != null ? camera : Camera.main);
			MinZoom = 0.05f;
			MaxZoom = 5f;
		}

		/// <summary>
		/// 将屏幕坐标转换为画布坐标
		/// <code>
		/// 通过相机的屏幕射线与画布平面的交点计算画布坐标。
		/// 画布平面由 Transform 的前向方向和位置定义。
		/// </code>
		/// </summary>
		/// <param name="screenPoint">屏幕空间的坐标点</param>
		/// <returns>画布空间的坐标点</returns>
		public override Vector2 ScreenToCanvas(Vector2 screenPoint)
		{
			if (_camera == null)
			{
				return Vector2.zero;
			}

			Ray ray = _camera.ScreenPointToRay(screenPoint);
			Plane plane = new Plane(-_canvasTransform.forward, _canvasTransform.position);
			if (plane.Raycast(ray, out float dist))
			{
				Vector3 worldPoint = ray.GetPoint(dist);
				Vector3 local = _canvasTransform.InverseTransformPoint(worldPoint);
				return (new Vector2(local.x, local.y) - _panOffset) / _zoomLevel;
			}

			return Vector2.zero;
		}

		/// <summary>
		/// 将画布坐标转换为屏幕坐标
		/// </summary>
		/// <param name="canvasPoint">画布空间的坐标点</param>
		/// <returns>屏幕空间的坐标点</returns>
		public override Vector2 CanvasToScreen(Vector2 canvasPoint)
		{
			if (_camera == null)
			{
				return Vector2.zero;
			}

			Vector2 local = canvasPoint * _zoomLevel + _panOffset;
			Vector3 world = _canvasTransform.TransformPoint(new Vector3(local.x, local.y, 0f));
			return _camera.WorldToScreenPoint(world);
		}

		/// <summary>
		/// 将画布坐标转换为世界坐标
		/// </summary>
		/// <param name="canvasPoint">画布空间的坐标点</param>
		/// <returns>世界空间的坐标点</returns>
		public override Vector3 CanvasToWorld(Vector2 canvasPoint)
		{
			Vector2 local = canvasPoint * _zoomLevel + _panOffset;
			return _canvasTransform.TransformPoint(new Vector3(local.x, local.y, 0f));
		}

		/// <summary>
		/// 将世界坐标转换为画布坐标
		/// </summary>
		/// <param name="worldPoint">世界空间的坐标点</param>
		/// <returns>画布空间的坐标点</returns>
		public override Vector2 WorldToCanvas(Vector3 worldPoint)
		{
			Vector3 local = _canvasTransform.InverseTransformPoint(worldPoint);
			return (new Vector2(local.x, local.y) - _panOffset) / _zoomLevel;
		}

		/// <summary>
		/// 获取渲染根 Transform
		/// </summary>
		public override Transform GetRootTransform()
		{
			return _canvasTransform;
		}

		/// <summary>
		/// Transform 组件未被销毁，画布可正常使用。
		/// </summary>
		public override bool IsAlive => _canvasTransform != null;
	}
}
