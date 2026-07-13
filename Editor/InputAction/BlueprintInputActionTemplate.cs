#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM 
using System.IO;
using UnityEditor;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace XericLibrary.Runtime.Blueprint.Editor
{
	/// <summary>
	/// 蓝图 InputAction 模板生成器。
	/// 右键 Project 窗口 → Xeric Library / Input Action Template / BlueprintAction
	/// 即可创建预设 InputActionAsset，内含蓝图所有的 Actions 和绑定。
	/// </summary>
	public static class BlueprintInputActionTemplate
	{
		private const string MenuPath = "Assets/Create/Xeric Library/Input Action Template/BlueprintAction";
		private const string DefaultFileName = "BlueprintActions";

		[MenuItem(MenuPath, priority = 1100)]
		public static void CreateBlueprintInputActionAsset()
		{
			var asset = ScriptableObject.CreateInstance<InputActionAsset>();

			var map = asset.AddActionMap(BlueprintInputConstants.MapName);

			var binding = InputBinding.MaskByGroup("Keyboard&Mouse");
			var gamepadBinding = InputBinding.MaskByGroup("Gamepad");

			// ── 指针位置 ──
			map.AddAction(
				BlueprintInputConstants.Point,
				type: InputActionType.Value,
				binding: "<Mouse>/position",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── 滚轮 ──
			map.AddAction(
				BlueprintInputConstants.ScrollWheel,
				type: InputActionType.Value,
				binding: "<Mouse>/scroll",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── 左键 ──
			map.AddAction(
				BlueprintInputConstants.LeftClick,
				type: InputActionType.Button,
				binding: "<Mouse>/leftButton",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── 右键 ──
			map.AddAction(
				BlueprintInputConstants.RightClick,
				type: InputActionType.Button,
				binding: "<Mouse>/rightButton",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── 中键 ──
			map.AddAction(
				BlueprintInputConstants.MiddleClick,
				type: InputActionType.Button,
				binding: "<Mouse>/middleButton",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── Shift ──
			map.AddAction(
				BlueprintInputConstants.Shift,
				type: InputActionType.Button,
				binding: "<Keyboard>/leftShift",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── Ctrl ──
			map.AddAction(
				BlueprintInputConstants.Ctrl,
				type: InputActionType.Button,
				binding: "<Keyboard>/leftCtrl",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── Alt ──
			map.AddAction(
				BlueprintInputConstants.Alt,
				type: InputActionType.Button,
				binding: "<Keyboard>/leftAlt",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── Pan（平移：方向键 + Shift+滚轮 → 水平, Alt+滚轮 → 垂直）──
			var panAction = map.AddAction(
				BlueprintInputConstants.Pan,
				type: InputActionType.Value,
				binding: null); // 多个 composite，需手动绑定

			// 键盘方向键 Vector2
			var kbCompositeIndex = panAction.AddCompositeBinding("2DVector")
				.With("Up", "<Keyboard>/upArrow")
				.With("Down", "<Keyboard>/downArrow")
				.With("Left", "<Keyboard>/leftArrow")
				.With("Right", "<Keyboard>/rightArrow");
			ApplyGroupToLastBindings(panAction, binding.ToString());

			// 手柄左摇杆
			panAction.AddCompositeBinding("2DVector")
				.With("Up", "<Gamepad>/leftStick/up")
				.With("Down", "<Gamepad>/leftStick/down")
				.With("Left", "<Gamepad>/leftStick/left")
				.With("Right", "<Gamepad>/leftStick/right");
			ApplyGroupToLastBindings(panAction, gamepadBinding.ToString());

			// ── Zoom（缩放：滚轮 + Ctrl热键）──
			var zoomAction = map.AddAction(
				BlueprintInputConstants.Zoom,
				type: InputActionType.Value);

			zoomAction.AddCompositeBinding("1DAxis")
				.With("Positive", "<Mouse>/scroll/up")
				.With("Negative", "<Mouse>/scroll/down");
			ApplyGroupToLastBindings(zoomAction, binding.ToString());

			// Ctrl 组合（多一层）
			zoomAction.AddCompositeBinding("1DAxis")
				.With("Positive", "<Keyboard>/ctrl")
				.With("Negative", "<Keyboard>/ctrl");
			ApplyGroupToLastBindings(zoomAction, binding.ToString());

			// ── FocusHome（Ctrl + H）──
			map.AddAction(
				BlueprintInputConstants.FocusHome,
				type: InputActionType.Button,
				binding: "<Keyboard>/h",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── Undo（Ctrl + Z）──
			map.AddAction(
				BlueprintInputConstants.Undo,
				type: InputActionType.Button,
				binding: "<Keyboard>/z",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── Redo（Ctrl + Y）──
			map.AddAction(
				BlueprintInputConstants.Redo,
				type: InputActionType.Button,
				binding: "<Keyboard>/y",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// ── NavigateParent（Tab）──
			map.AddAction(
				BlueprintInputConstants.NavigateParent,
				type: InputActionType.Button,
				binding: "<Keyboard>/tab",
				interactions: null,
				processors: null,
				groups: binding.ToString());

			// 保存资产 —— .inputactions 文件需以 JSON 文本形式写入，
			// 再由 AssetDatabase.ImportAsset 触发 InputActionImporter 解析。
			string folderPath = GetSelectedFolderPath();
			string assetPath = AssetDatabase.GenerateUniqueAssetPath(
				Path.Combine(folderPath, $"{DefaultFileName}.inputactions"));

			string json = asset.ToJson();
			System.IO.File.WriteAllText(assetPath, json);

			AssetDatabase.ImportAsset(assetPath);
			AssetDatabase.SaveAssets();

			var imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
			EditorGUIUtility.PingObject(imported);
			Debug.Log($"[Blueprint] InputAction 模板已创建: {assetPath}");
		}

		/// <summary>
		/// 将 action 最后一个 binding（及所有 composite part）的 groups 设为指定值。
		/// </summary>
		private static void ApplyGroupToLastBindings(InputAction action, string groups)
		{
			int count = action.bindings.Count;
			int compositeStart = -1;
			for (int i = count - 1; i >= 0; i--)
			{
				if (action.bindings[i].isComposite)
				{
					compositeStart = i;
					break;
				}
			}
			if (compositeStart < 0) return;

			for (int i = compositeStart; i < count; i++)
				action.ChangeBinding(i).WithGroup(groups);
		}

		/// <summary>
		/// 获取当前选中的 Project 窗口文件夹路径。
		/// </summary>
		private static string GetSelectedFolderPath()
		{
			string path = "Assets";
			var selected = Selection.activeObject;
			if (selected != null)
			{
				string assetPath = AssetDatabase.GetAssetPath(selected);
				if (!string.IsNullOrEmpty(assetPath))
				{
					if (AssetDatabase.IsValidFolder(assetPath))
						return assetPath;
					return Path.GetDirectoryName(assetPath);
				}
			}
			return path;
		}
	}
}
#endif
