using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 快速 UGUI 图论网格背景渲染工具。
	/// 在蓝图画布最底层创建一个全屏 Image 组件绘制网格背景，
	/// 每帧将画布的 zoom / pan 同步到 Shader 的 _Transform 属性。
	/// 全部可调参数由 QuickGraphGridBackgroundConfig 配置资产提供。
	/// </summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 0)]
	[BlueprintTheme("QuickGraph")]
	public class QuickUGUIGraphGridBackgroundTool : BlueprintTool
	{
		private const string BackgroundGoName = "__Bp_GridBackground";

		private QuickGraphGridBackgroundConfig _defaultConfig;

		private QuickGraphGridBackgroundConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphGridBackgroundConfig external)
					return external;
				if (_defaultConfig == null)
					_defaultConfig = ScriptableObject.CreateInstance<QuickGraphGridBackgroundConfig>();
				return _defaultConfig;
			}
		}

		private Image _backgroundImage;
		private Transform _renderRoot;

		public override void OnInitialize()
		{
			EnsureBackground();
		}

		public override void OnRender()
		{
			if (Graph == null) return;
			EnsureBackground();
			SyncMaterialProperties();
		}

		private void EnsureBackground()
		{
			if (_backgroundImage != null) return;

			EnsureRenderRoot();
			if (_renderRoot == null) return;

			var existing = _renderRoot.Find(BackgroundGoName);
			if (existing != null)
			{
				_backgroundImage = existing.GetComponent<Image>();
				if (_backgroundImage == null)
				{
					Object.DestroyImmediate(existing.gameObject);
				}
				else
				{
					ApplyMaterial();
					return;
				}
			}

			var bgGo = new GameObject(BackgroundGoName, typeof(RectTransform));
			bgGo.hideFlags = HideFlags.HideAndDontSave;
			bgGo.transform.SetParent(_renderRoot, false);
			bgGo.transform.SetAsFirstSibling();

			var rt = bgGo.GetComponent<RectTransform>();
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;

			_backgroundImage = bgGo.AddComponent<Image>();
			_backgroundImage.raycastTarget = true;

			ApplyMaterial();
		}

		/// <summary>
		/// 从配置获取材质并设置到 Image（仅首次执行）。
		/// </summary>
		private void ApplyMaterial()
		{
			if (_backgroundImage == null) return;
			var mat = Config.GetOrCreateMaterial();
			if (mat != null)
				_backgroundImage.material = mat;
		}

		/// <summary>
		/// 每帧将当前画布 zoom / pan 同步到 Shader，
		/// 同时写入配置中所有 Shader 属性。
		/// </summary>
		private void SyncMaterialProperties()
		{
			if (_backgroundImage == null) return;
			if (Graph == null || Graph.Canvas == null) return;

			var mat = _backgroundImage.material;
			if (mat == null) return;

			var rt = _backgroundImage.rectTransform;
			float zoom = Graph.Canvas.ZoomLevel;
			Vector2 pan = Graph.Canvas.PanOffset;

			Config.ApplyToMaterial(mat, rt.rect.size, zoom, pan);
		}

		private void EnsureRenderRoot()
		{
			if (_renderRoot != null) return;
			if (Graph == null || Graph.Canvas == null) return;
			_renderRoot = Graph.Canvas.GetRootTransform();
		}

		public override void OnDestroy()
		{
			if (_backgroundImage != null && _backgroundImage.gameObject != null)
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
					Object.DestroyImmediate(_backgroundImage.gameObject);
				else
#endif
					Object.Destroy(_backgroundImage.gameObject);
				_backgroundImage = null;
			}

			Config.ReleaseMaterial();
			_defaultConfig = null;
			_renderRoot = null;
		}
	}
}
