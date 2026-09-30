using UnityEditor;
using UnityEngine;

using XericLibrary.Runtime;
using XericLibrary.Runtime.UIGraph;
using XericLibrary.Runtime.XericComponent;

namespace XericLibraryEditor.Spline
{
	/// <summary>
	/// 样条曲线路径的 Hierarchy 右键创建菜单。
	/// 此文件直接放在 Release/Editor/ 下由 Unity 编译，
	/// 不经过外部 DLL 管线，确保菜单立即生效。
	/// </summary>
	public static class SplinePathCreateMenuItems
	{
		private const string MenuRoot = "GameObject/Xeric Library/Spline/";

		/// <summary>
		/// 创建带 XericRectSplinePath 的 UI 节点（二维样条）
		/// </summary>
		[MenuItem(MenuRoot + "2D Spline", false, 20)]
		private static void CreateRectSplinePath(MenuCommand menuCommand)
		{
			var go = CreateUiGameObject("2D Spline", new Vector2(300f, 150f));
			var path = go.AddComponent<XericRectSplinePath>();

			// 给两个默认控制点，创建后场景里立刻有点可选中
			path.xericSpline.Add(MakeDefaultPoint(new Vector2(0.25f, 0.5f)));
			path.xericSpline.Add(MakeDefaultPoint(new Vector2(0.75f, 0.5f)));

			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			Finish(go, "Create 2D Spline");
		}

		/// <summary>
		/// 创建带 XericSplinePath 的场景节点（三维样条）
		/// </summary>
		[MenuItem(MenuRoot + "3D Spline", false, 21)]
		private static void CreateSplinePath(MenuCommand menuCommand)
		{
			var go = new GameObject("3D Spline");
			var path = go.AddComponent<XericSplinePath>();

			path.xericSpline.Add(new XericSpline.SplinePoint(Vector3.left));
			path.xericSpline.Add(new XericSpline.SplinePoint(Vector3.right));

			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			Finish(go, "Create 3D Spline");
		}

		/// <summary>
		/// 创建由二维样条驱动的 UI 曲线渲染节点，并自动绑定同物体上的样条路径
		/// </summary>
		[MenuItem(MenuRoot + "Spline Curve Renderer", false, 22)]
		private static void CreateSplineCurveRenderer(MenuCommand menuCommand)
		{
			var go = CreateUiGameObject("Spline Curve Renderer", new Vector2(300f, 150f));
			var path = go.AddComponent<XericRectSplinePath>();

			path.xericSpline.Add(MakeDefaultPoint(new Vector2(0.1f, 0.5f)));
			path.xericSpline.Add(MakeDefaultPoint(new Vector2(0.9f, 0.5f)));

			var renderer = go.AddComponent<UISplineCurveRenderer>();
			renderer.splinePath = path;

			GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
			Finish(go, "Create Spline Curve Renderer");
		}

		/// <summary>
		/// 建一条能直接跑起来的曲线进度条：底轨铺满 + 填充到 60%，两条面片共用同一条样条
		/// </summary>
		[MenuItem(MenuRoot + "Progress Bar Demo", false, 23)]
		private static void CreateProgressBarDemo(MenuCommand menuCommand)
		{
			var go = CreateUiGameObject("Spline Progress Bar", new Vector2(520f, 220f));
			var path = go.AddComponent<XericRectSplinePath>();

			// S 形：弧长校正在这条形状上才看得出差别（匀速直线段上校正前后一模一样）
			AddDemoPoint(path, new Vector2(0.08f, 0.3f), new Vector2(0.28f, 0f));
			AddDemoPoint(path, new Vector2(0.5f, 0.78f), new Vector2(0.3f, 0f));
			AddDemoPoint(path, new Vector2(0.92f, 0.22f), new Vector2(0.26f, 0f));

			var renderer = go.AddComponent<UISplineCurveRenderer>();
			renderer.splinePath = path;
			// 面片0＝底轨铺满并压暗，面片1＝填充到 60% 保持全亮
			renderer.headTailPairs = new[] { 1f, 0f, 0.6f, 0f };
			renderer.ribbonTints = new[]
			{
				new Color32(90, 90, 90, 255),
				new Color32(255, 255, 255, 255),
			};
			renderer.curveWidth = 14f;
			renderer.fillRibbonIndex = 1;
			go.AddComponent<UISplineProgressDriver>();
			ApplyProgressMaterial(renderer);

			PlaceUnderCanvas(go, menuCommand);
			Finish(go, "Create Spline Progress Bar");
		}

		/// <summary>
		/// 换上真正消费 uv0.y 的材质，并配一张沿路径方向的手风琴条纹，这样一进来就能看见流动。
		/// 纹理是运行时生成的（不落盘、不进版本库），要长期用请自己把图拖到 progressTexture 上。
		/// </summary>
		private static void ApplyProgressMaterial(UISplineCurveRenderer renderer)
		{
			var shader = Shader.Find("XericLibrary/UIGraph/XericUICurveProgress");
			if (shader == null || !shader.isSupported)
			{
				// 必须写全：本文件在 XericLibraryEditor.Spline 里，裸 Debug 会被
				// 外层命名空间 XericLibraryEditor.Debug 抢走
				UnityEngine.Debug.LogWarning("[Spline Progress] 找不到 XericUICurveProgress 着色器，演示沿用默认材质（默认材质不读 uv0.y，也没有采贴图）");
				return;
			}

			var texture = new Texture2D(4, 128, TextureFormat.RGBA32, false);
			texture.wrapMode = TextureWrapMode.Repeat;
			for (int y = 0; y < texture.height; y++)
			{
				// 沿 uv.y（也就是路径进度）方向的亮带 + 暗谷
				float wave = 0.5f + 0.5f * Mathf.Sin(y * Mathf.PI * 2f / texture.height * 3f);
				var color = new Color(1f, 1f, 1f, Mathf.Lerp(0.25f, 1f, wave));
				for (int x = 0; x < texture.width; x++)
					texture.SetPixel(x, y, color);
			}
			texture.Apply();

			var material = new Material(shader);
			material.SetFloat("_Tile", 5f);
			material.SetFloat("_Scroll", 0.45f);
			material.SetFloat("_EdgeExp", 0.85f);
			material.SetFloat("_EdgeStepRange", 0.12f);

			renderer.progressTexture = texture;
			renderer.material = material;
		}

		// 工具方法

		/// <summary>默认手柄偏移（锚点空间）：手柄全零时贝塞尔段退化成直线，看不出这是一条曲线</summary>
		private const float DefaultHandleOffset = 0.1f;

		/// <summary>
		/// 造一个带默认手柄的端点。与 2D 编辑器里插入端点用的是同一套默认值，
		/// 两边各自实现（编辑器在 DLL 里，这里要当场编译，不能等它重新构建）。
		/// </summary>
		private static XericRectSpline.RectSplinePoint MakeDefaultPoint(Vector2 anchor)
		{
			var point = new XericRectSpline.RectSplinePoint(anchor);
			// Local 是相对锚点的偏移；StartHandle/EndHandle 是叠在锚点上的绝对值
			point.StartHandleLocal = new Vector2(-DefaultHandleOffset, 0f);
			point.EndHandleLocal = new Vector2(DefaultHandleOffset, 0f);
			return point;
		}

		private static void AddDemoPoint(XericRectSplinePath path, Vector2 anchor, Vector2 handleOffset)
		{
			var point = new XericRectSpline.RectSplinePoint(anchor);
			// Start/EndHandle 是"叠在锚点上的"绝对锚点值，不是偏移量
			point.EndHandle = anchor + handleOffset;
			point.StartHandle = anchor - handleOffset;
			path.xericSpline.Add(point);
		}

		/// <summary>
		/// Graphic 不在 Canvas 下就画不出来，演示物必须落到某个 Canvas 里；
		/// 场景里没有就现建一个（与 BlueprintMenuItems 同一套路）
		/// </summary>
		private static void PlaceUnderCanvas(GameObject go, MenuCommand menuCommand)
		{
			var parent = menuCommand.context as GameObject;
			if (parent != null && parent.GetComponentInParent<Canvas>() != null)
			{
				GameObjectUtility.SetParentAndAlign(go, parent);
				return;
			}

			var canvas = Object.FindObjectOfType<Canvas>();
			if (canvas == null)
			{
				var canvasGo = new GameObject("Spline Demo Canvas",
					typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
				canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
				Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas For Spline Demo");
			}

			GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);
		}

		/// <summary>
		/// 创建带 RectTransform 的 UI 节点；界面样条的计算范围就是这个矩形
		/// </summary>
		private static GameObject CreateUiGameObject(string name, Vector2 size)
		{
			var go = new GameObject(name, typeof(RectTransform));
			var rt = go.GetComponent<RectTransform>();
			rt.sizeDelta = size;
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(0.5f, 0.5f);
			return go;
		}

		private static void Finish(GameObject go, string undoName)
		{
			Undo.RegisterCreatedObjectUndo(go, undoName);
			Selection.activeObject = go;
		}
	}
}
