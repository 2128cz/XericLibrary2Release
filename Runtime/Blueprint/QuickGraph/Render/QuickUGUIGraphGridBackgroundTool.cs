using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint.QuickGraph
{
	/// <summary>QuickGraph 网格背景；样式资源材质只绑定不拥有，运行时创建材质才由本工具释放。</summary>
	[BlueprintTool(phase: ToolPhase.Render, order: 0)]
	[BlueprintTheme("QuickGraph")]
	public class QuickUGUIGraphGridBackgroundTool : BlueprintTool
	{
		private const string BackgroundGoName = "__Bp_GridBackground";
		private QuickGraphGridBackgroundConfig _defaultConfig;
		private QuickGraphRenderStyleResolver _styleResolver;
		private Image _backgroundImage;
		private Material _backgroundMaterial;
		private QuickGraphGridBackgroundConfig _ownedMaterialConfig;
		private string _ownedShaderName;
		private QuickGraphGridBackgroundConfig Config
		{
			get
			{
				if (ToolConfig is QuickGraphGridBackgroundConfig external) return external;
				if (_defaultConfig == null) _defaultConfig = ScriptableObject.CreateInstance<QuickGraphGridBackgroundConfig>();
				return _defaultConfig;
			}
		}
		public override void OnInitialize()
		{
			_styleResolver = new QuickGraphRenderStyleResolver(Graph, gridFallback: ConfigStyle);
			_styleResolver.Changed += OnStyleChanged;
			EnsureBackground(); RebindMaterial(_styleResolver.Snapshot.Grid, true);
		}
		public override void OnRender()
		{
			if (Graph == null) return;
			EnsureBackground();
			var style = _styleResolver != null ? _styleResolver.Snapshot.Grid : ConfigStyle();
			RebindMaterial(style, false);
			ApplyStyle(style);
		}
		public override void OnConfigChanged()
		{
			_styleResolver?.Refresh();
			if (_styleResolver == null) RebindMaterial(ConfigStyle(), true);
			Graph?.MarkDirty();
		}
		private QuickGraphGridRenderStyle ConfigStyle()
		{
			var cfg = Config;
			return new QuickGraphGridRenderStyle { Material = cfg.OverrideMaterial, ShaderName = cfg.ShaderName, Size = cfg.GridSize, OverlayPower = cfg.GridOverlayPower, Threshold = cfg.GridLineThreshold, Exp = cfg.GridExp, Color = cfg.GridColor, Background = cfg.GridBackgroundColor };
		}
		private void OnStyleChanged(QuickGraphRenderStyleSnapshot snapshot)
		{
			if ((snapshot.Changes & QuickGraphRenderStyleChange.GridAppearance) == 0) return;
			RebindMaterial(snapshot.Grid, true);
			Graph?.MarkDirty();
		}
		private void EnsureBackground()
		{
			if (_backgroundImage != null || Graph?.Canvas?.GetRootTransform() == null) return;
			var root = Graph.Canvas.GetRootTransform(); var existing = root.Find(BackgroundGoName);
			if (existing != null) _backgroundImage = existing.GetComponent<Image>();
			if (_backgroundImage != null) return;
			if (existing != null) Object.DestroyImmediate(existing.gameObject);
			var go = new GameObject(BackgroundGoName, typeof(RectTransform)); go.hideFlags = HideFlags.HideAndDontSave; go.transform.SetParent(root, false); go.transform.SetAsFirstSibling();
			var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
			_backgroundImage = go.AddComponent<Image>(); _backgroundImage.raycastTarget = true; _backgroundImage.maskable = false;
		}
		private void RebindMaterial(QuickGraphGridRenderStyle style, bool force)
		{
			if (_backgroundImage == null) return;
			Material target = style.Material;
			QuickGraphGridBackgroundConfig owner = null;
			string ownedShader = null;
			if (target == null)
			{
				// 旧 Config 仅在 style 未提供共享材质时参与回退；其缓存材质为唯一可释放对象。
				if (style.ShaderName == Config.ShaderName) { target = Config.GetOrCreateMaterial(); owner = Config.OverrideMaterial == null ? Config : null; }
				else if (!string.IsNullOrEmpty(style.ShaderName))
				{
					if (_backgroundMaterial != null && _ownedShaderName == style.ShaderName) return;
					var shader = Shader.Find(style.ShaderName); if (shader != null) { target = new Material(shader) { name = "BpGridBackground_StyleMat" }; ownedShader = style.ShaderName; }
				}
			}
			if (target == _backgroundMaterial) return;
			ReleaseOwnedMaterial();
			_backgroundMaterial = target; _ownedMaterialConfig = owner; _ownedShaderName = ownedShader;
			if (_backgroundImage != null) _backgroundImage.material = _backgroundMaterial;
		}
		private void ApplyStyle(QuickGraphGridRenderStyle style)
		{
			if (_backgroundMaterial == null || Graph?.Canvas == null || _backgroundImage == null) return;
			var size = _backgroundImage.rectTransform.rect.size; var zoom = Graph.Canvas.ZoomLevel; var pan = Graph.Canvas.PanOffset; var gridSize = Mathf.Max(.0001f, style.Size);
			_backgroundMaterial.SetVector("_Transform", new Vector4(size.x / (zoom * gridSize), size.y / (zoom * gridSize), -(pan.x + .5f * size.x) / (zoom * gridSize), -(pan.y + .5f * size.y) / (zoom * gridSize)));
			_backgroundMaterial.SetFloat("_GridOverlayPower", style.OverlayPower); _backgroundMaterial.SetFloat("_GridLineThreshold", style.Threshold); _backgroundMaterial.SetFloat("_GridExp", style.Exp);
			_backgroundMaterial.SetColor("_GridColor", style.Color); _backgroundMaterial.SetColor("_GridBackgroundColor", style.Background);
		}
		private void ReleaseOwnedMaterial()
		{
			if (_backgroundImage != null) _backgroundImage.material = null;
			if (_ownedMaterialConfig != null) _ownedMaterialConfig.ReleaseMaterial();
			else if (_backgroundMaterial != null && !string.IsNullOrEmpty(_ownedShaderName)) { if (Application.isPlaying) Object.Destroy(_backgroundMaterial); else Object.DestroyImmediate(_backgroundMaterial); }
			_backgroundMaterial = null; _ownedMaterialConfig = null; _ownedShaderName = null;
		}
		public override void OnDestroy()
		{
			if (_styleResolver != null) _styleResolver.Changed -= OnStyleChanged;
			_styleResolver?.Dispose(); _styleResolver = null; ReleaseOwnedMaterial();
			if (_backgroundImage != null) { if (Application.isPlaying) Object.Destroy(_backgroundImage.gameObject); else Object.DestroyImmediate(_backgroundImage.gameObject); _backgroundImage = null; }
			_defaultConfig = null;
		}
	}
}
