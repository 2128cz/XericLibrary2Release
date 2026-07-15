using UnityEngine;
using UnityEngine.UI;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 快速 UGUI 图论网格背景渲染工具。
	/// 在蓝图画布最底层创建一个 Image 组件绘制网格背景，
	/// 每帧将画布的 zoom / pan 同步到 Shader 的 _Transform 属性。
	/// <para>
	/// 背景 Image 在 <see cref="__Bp_RenderRoot"/> 下锚点撑满，
	/// 只响应画布属性和 dirty flag，不会因画布重建而销毁（见 <see cref="OnInitialize"/>）。
	/// Shader 参数（<c>_Transform</c> 的 scale/offset）在 <see cref="OnRender"/> 中随
	/// <c>ZoomLevel / PanOffset</c> 更新，与画布拖拽共享同一属性源。
	/// </para>
	/// <para>
	/// <b>材质引用注意：</b>
	/// 必须缓存材质实例（<see cref="_backgroundMaterial"/>）并直接写入该实例。
	/// UGUI 的 <c>Graphic.material</c> getter 可能返回 <c>materialForRendering</c>
	/// 的副本（与 setter 存入的 <c>m_Material</c> 不同），因此 <c>_backgroundImage.material</c>
	/// 不适合用于写 shader 属性。见 <c>Graphic.material</c> Unity 源码。
	/// </para>
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

		/// <summary>
		/// 缓存的背景材质实例。
		/// 由 <see cref="Config.GetOrCreateMaterial"/> 创建，终身保留。
		/// shader 属性始终写入此实例，避免 UGUI <c>materialForRendering</c> 副本问题。
		/// </summary>
		private Material _backgroundMaterial;

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

		/// <summary>
		/// 在 RenderRoot 下查找或创建背景 Image。
		/// 创建后终身保留，不随画布重建销毁。
		/// </summary>
		private void EnsureBackground()
		{
			if (_backgroundImage != null) return;
			if (Graph?.Canvas?.GetRootTransform() == null) return;

			var root = Graph.Canvas.GetRootTransform();

			var existing = root.Find(BackgroundGoName);
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
			bgGo.transform.SetParent(root, false);
			bgGo.transform.SetAsFirstSibling();

			var rt = bgGo.GetComponent<RectTransform>();
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;

			_backgroundImage = bgGo.AddComponent<Image>();
			_backgroundImage.raycastTarget = true;
			// 关闭 maskable：运行时背景 Image 不参与 UGUI Mask 裁剪，
			// 避免父级 Mask 组件触发 materialForRendering 克隆材质实例。
			// 材质写入（_Transform 等 shader 属性）直通 GPU，不受 IMaterialModifier 干扰。
			_backgroundImage.maskable = false;

			ApplyMaterial();
		}

		/// <summary>
		/// 从配置获取材质并缓存到 <see cref="_backgroundMaterial"/>，
		/// 然后赋值给 Image。
		/// 此方法仅执行一次，后续材质写入直接使用缓存的引用。
		/// </summary>
		private void ApplyMaterial()
		{
			if (_backgroundImage == null) return;
			if (_backgroundMaterial != null) return;

			_backgroundMaterial = Config.GetOrCreateMaterial();
			if (_backgroundMaterial != null)
				_backgroundImage.material = _backgroundMaterial;
		}

		/// <summary>
		/// 每帧将当前画布 zoom / pan 同步到 Shader，
		/// 使用 <see cref="_backgroundMaterial"/> 缓存写入，确保与 GPU 使用的材质实例一致。
		/// </summary>
		private void SyncMaterialProperties()
		{
			if (_backgroundImage == null) return;
			if (_backgroundMaterial == null) return;
			if (Graph == null || Graph.Canvas == null) return;

			var rt = _backgroundImage.rectTransform;
			float zoom = Graph.Canvas.ZoomLevel;
			Vector2 pan = Graph.Canvas.PanOffset;

			Config.ApplyToMaterial(_backgroundMaterial, rt.rect.size, zoom, pan);
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

			_backgroundMaterial = null;
			Config.ReleaseMaterial();
			_defaultConfig = null;
		}
	}
}
