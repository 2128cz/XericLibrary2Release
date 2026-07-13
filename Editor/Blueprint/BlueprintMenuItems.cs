using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.Blueprint;

namespace XericLibraryEditor.Bluprint.Editor
{
	/// <summary>
	/// 蓝图右键菜单项 —— 通过 GameObject 菜单快捷创建蓝图示例。
	/// </summary>
	public static class BlueprintMenuItems
	{
		private const string MenuRoot = "GameObject/Xeric Library/Blueprint/";

		[MenuItem(MenuRoot + "UI Graph Bp", false, 10)]
		private static void CreateUIGraphBlueprint()
		{
			var go = CreateBlueprintGameObject("Blueprint Graph (UI)");

			var canvas = go.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			go.AddComponent<CanvasScaler>();
			go.AddComponent<GraphicRaycaster>();

			go.AddComponent<GraphTheoryBlueprintComponent>();

			Selection.activeGameObject = go;
			Undo.RegisterCreatedObjectUndo(go, "Create UI Graph Blueprint");
		}

		[MenuItem(MenuRoot + "UI Graph Bp (QuickGraph)", false, 11)]
		private static void CreateUIQuickGraphBlueprint()
		{
			var go = CreateBlueprintGameObject("Blueprint Graph - QuickGraph (UI)");

			var canvas = go.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			go.AddComponent<CanvasScaler>();
			go.AddComponent<GraphicRaycaster>();

			var comp = go.AddComponent<GraphTheoryBlueprintComponent>();
			comp.ThemeName = "QuickGraph";

			Selection.activeGameObject = go;
			Undo.RegisterCreatedObjectUndo(go, "Create UI QuickGraph Blueprint");
		}

		[MenuItem(MenuRoot + "World Space Graph Bp", false, 12)]
		private static void CreateWorldSpaceGraphBlueprint()
		{
			var go = CreateBlueprintGameObject("Blueprint Graph (World)");
			go.AddComponent<GraphTheoryBlueprintComponent>();

			Selection.activeGameObject = go;
			Undo.RegisterCreatedObjectUndo(go, "Create World Space Graph Blueprint");
		}

		private static GameObject CreateBlueprintGameObject(string name)
		{
			var go = new GameObject(name);

			if (Selection.activeTransform != null)
			{
				go.transform.SetParent(Selection.activeTransform, false);
			}

			go.transform.SetAsLastSibling();
			return go;
		}
	}
}
