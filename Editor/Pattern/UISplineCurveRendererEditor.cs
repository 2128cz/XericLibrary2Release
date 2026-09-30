using UnityEditor;
using UnityEngine;

using XericLibrary.Runtime.UIGraph;

namespace XericLibraryEditor.XericComponent
{
	/// <summary>
	/// UISplineCurveRenderer 的 Inspector：在默认字段上面加一条烘焙工具栏。
	/// 烘焙系数（采样数、触发时机）本来就是公开字段，这里只补"手动烘一次"和状态读数。
	/// </summary>
	[CustomEditor(typeof(UISplineCurveRenderer))]
	public class UISplineCurveRendererEditor : Editor
	{
		private UISplineCurveRenderer script;

		private void OnEnable()
		{
			script = (UISplineCurveRenderer)target;
		}

		public override void OnInspectorGUI()
		{
			DrawBakeBar();

			serializedObject.Update();
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// 烘焙状态条：读数 + 手动烘焙按钮。
		/// 烘焙结果落在 XericRectSpline 的序列化字段里，所以按完要标脏，
		/// 否则存不进场景，下次打开还得重烘。
		/// </summary>
		private void DrawBakeBar()
		{
			if (script == null)
				return;

			var path = script.splinePath;
			var spline = path != null ? path.xericSpline : null;

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			EditorGUILayout.LabelField("弧长烘焙", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("采样点",
				spline != null ? spline.BakeCount.ToString() : "未绑定样条");
			EditorGUILayout.LabelField("曲线长度",
				spline != null ? spline.SplineLength.ToString("0.###") : "-");
			EditorGUILayout.LabelField("已烘焙次数", script.BakeRunCount.ToString());
			EditorGUILayout.LabelField("校正状态",
				script.progressMapping == SplineProgressMapping.BakedDistance
					? (spline != null && spline.BakeCount >= script.bakeSubdivide
						? "按烘焙距离校正"
						: "烘焙数据不够，暂时退回参数映射")
					: "未启用（直接按参数）");

			using (new EditorGUI.DisabledScope(spline == null))
			{
				if (GUILayout.Button("烘焙一次", GUILayout.Height(24)))
				{
					// XericRectSpline 是普通 [Serializable] 类，不是 Object，
					// 能登记撤销/标脏的只有承载它的那个路径组件
					Undo.RecordObject(path, "烘焙样条");
					script.BakeNow();
					EditorUtility.SetDirty(path);
					EditorUtility.SetDirty(script);
					SceneView.RepaintAll();
				}
			}

			if (script.bakeTrigger == SplineBakeTrigger.Manual)
				EditorGUILayout.HelpBox(
					"触发时机是 Manual：几何怎么改都不会自动烘，进度贴不贴得准就看上面这个按钮按没按。",
					MessageType.Info);

			EditorGUILayout.EndVertical();
		}
	}
}
