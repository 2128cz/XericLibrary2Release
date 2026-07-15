using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.Blueprint;

namespace XericLibraryEditor.Bluprint.Editor
{
	/// <summary>
	/// 蓝图右键菜单项 —— 通过 GameObject 菜单/Gizmo 快捷创建蓝图场景对象。
	/// <para>
	/// 创建逻辑：
	/// <list type="number">
	/// <item>检查选中节点是否已在 Canvas 子级下；若否则新建 Canvas（1024×720）。</item>
	/// <item>在 Canvas 下创建背景 Image 子项（1024×720，中心对齐）。</item>
	/// <item>在 Canvas 下创建蓝图组件子项。</item>
	/// </list>
	/// </para>
	/// </summary>
	public static class BlueprintMenuItems
	{
		private const string MenuRoot = "GameObject/Xeric Library/Blueprint/";
		private const string BackgroundGoName = "__Bp_GridBackground";

		// ========== 菜单项 ==========

		[MenuItem(MenuRoot + "UI Graph Bp", false, 10)]
		private static void CreateUIGraphBlueprint()
		{
			var parent = GetOrCreateCanvasParent();
			var bg = CreateBackgroundImage(parent);
			var bpGo = CreateChild(parent, "Blueprint Graph (UI)");
			bpGo.transform.SetParent(bg.transform, false);
			bpGo.AddComponent<GraphTheoryBlueprintComponent>();
			Finish(bpGo, "Create UI Graph Blueprint");
		}

		[MenuItem(MenuRoot + "UI Graph Bp (QuickGraph)", false, 11)]
		private static void CreateUIQuickGraphBlueprint()
		{
			var parent = GetOrCreateCanvasParent();
			var bg = CreateBackgroundImage(parent);
			var bpGo = CreateChild(parent, "Blueprint Graph - QuickGraph (UI)");
			bpGo.transform.SetParent(bg.transform, false);
			var comp = bpGo.AddComponent<GraphTheoryBlueprintComponent>();
			comp.ThemeName = "QuickGraph";
			Finish(bpGo, "Create UI QuickGraph Blueprint");
		}

		[MenuItem(MenuRoot + "World Space Graph Bp", false, 12)]
		private static void CreateWorldSpaceGraphBlueprint()
		{
			var go = CreateBlueprintGameObject("Blueprint Graph (World)");
			go.AddComponent<GraphTheoryBlueprintComponent>();
			Finish(go, "Create World Space Graph Blueprint");
		}

		// ========== 工具方法 ==========

		/// <summary>
		/// 确保存在 Canvas 父节点。
		/// 当前选中节点已在 Canvas 子级下 → 直接返回根 Canvas。
		/// 否则 → 新建 Canvas 并设为父级。
		/// </summary>
		private static Transform GetOrCreateCanvasParent()
		{
			var active = Selection.activeTransform;
			if (active != null)
			{
				var existingCanvas = active.GetComponentInParent<Canvas>();
				if (existingCanvas != null)
					return existingCanvas.transform;

				// 如果选中对象不是 Canvas 但已有 Canvas 同级或父级
				existingCanvas = Object.FindObjectOfType<Canvas>();
				if (existingCanvas != null)
					return existingCanvas.transform;
			}
			else
			{
				// 场景中没有任何选中对象，找现有的 Canvas
				var existingCanvas = Object.FindObjectOfType<Canvas>();
				if (existingCanvas != null)
					return existingCanvas.transform;
			}

			// 真的没有 Canvas → 新建一个
			var canvasGo = new GameObject("Blueprint Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			var canvas = canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var rt = canvasGo.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(1024, 720);
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.pivot = new Vector2(0.5f, 0.5f);

			if (Selection.activeTransform != null)
				canvasGo.transform.SetParent(Selection.activeTransform, false);

			canvasGo.transform.SetAsLastSibling();
			return canvasGo.transform;
		}

		/// <summary>
		/// 创建背景 Image 子项，1024×720，中心对齐，启用射线检测。
		/// 如果同层级下已有同名的背景对象，跳过创建。
		/// </summary>
		private static GameObject CreateBackgroundImage(Transform parent)
		{
			// 不重复创建
			var existing = parent.Find(BackgroundGoName);
			if (existing != null)
				return existing.gameObject;

			var bgGo = new GameObject(BackgroundGoName, typeof(Image), typeof(Mask));
			var bgRt = bgGo.GetComponent<RectTransform>();
			bgRt.SetParent(parent, false);

			// 撑满父节点
			bgRt.anchorMin = Vector2.zero;
			bgRt.anchorMax = Vector2.one;
			bgRt.offsetMin = Vector2.zero;
			bgRt.offsetMax = Vector2.zero;
			bgRt.pivot = new Vector2(0.5f, 0.5f);

			var image = bgGo.GetComponent<Image>();
			image.raycastTarget = true;
			image.color = new Color(0.15f, 0.15f, 0.17f, 1f);
			bgGo.transform.SetAsFirstSibling();
			return bgGo;
		}

		private static GameObject CreateChild(Transform parent, string name)
		{
			var go = new GameObject(name, typeof(RectTransform));
			var rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			rt.SetAsLastSibling();
			return go;
		}

		private static GameObject CreateBlueprintGameObject(string name)
		{
			var go = new GameObject(name);
			if (Selection.activeTransform != null)
				go.transform.SetParent(Selection.activeTransform, false);
			go.transform.SetAsLastSibling();
			return go;
		}

		private static void Finish(GameObject go, string undoName)
		{
			Selection.activeGameObject = go;
			Undo.RegisterCreatedObjectUndo(go, undoName);
			// 也注册父级 Canvas 的撤销
			var canvas = go.GetComponentInParent<Canvas>();
			if (canvas != null)
				Undo.RegisterCreatedObjectUndo(canvas.gameObject, undoName);
		}
	}
}
