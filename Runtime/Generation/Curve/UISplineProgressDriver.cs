using UnityEngine;

namespace XericLibrary.Runtime.UIGraph
{
	/// <summary>
	/// 曲线进度条的播放驱动器：把时间推成 UISplineCurveRenderer 的进度。
	/// 只负责"进度从哪来"，几何与着色全在渲染器那一侧。
	/// </summary>
	[AddComponentMenu("Xeric Library/UI/UISplineProgressDriver", 18)]
	[RequireComponent(typeof(UISplineCurveRenderer))]
	public class UISplineProgressDriver : MonoBehaviour
	{
		/// <summary>自动推进（关掉就只认手动 SetProgress）</summary>
		public bool autoDrive = true;

		/// <summary>从 0 走到 1 所需秒数</summary>
		[Min(0.01f)]
		public float duration = 2.5f;

		/// <summary>用 scaledTime，随引擎暂停/缩放</summary>
		public bool useUnscaledTime = false;

		/// <summary>走到尽头后往返还是从头再来</summary>
		public bool pingPong = false;

		/// <summary>起始进度</summary>
		[Range(0f, 1f)]
		public float startProgress = 0f;

		private UISplineCurveRenderer m_Renderer;
		private float m_Elapsed;

		private void Awake()
		{
			m_Renderer = GetComponent<UISplineCurveRenderer>();
			m_Elapsed = startProgress * duration;
			if (m_Renderer != null)
				m_Renderer.SetProgress(startProgress);
		}

		private void Update()
		{
			if (!autoDrive || m_Renderer == null)
				return;

			m_Elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
			float cycle = m_Elapsed / duration;

			float progress = pingPong
				? Mathf.PingPong(cycle, 1f)
				: cycle - Mathf.Floor(cycle);

			m_Renderer.SetProgress(progress);
		}

		/// <summary>手动设进度，同时让自动推进从该处继续</summary>
		public void SetProgress(float value)
		{
			value = Mathf.Clamp01(value);
			m_Elapsed = value * duration;
			if (m_Renderer != null)
				m_Renderer.SetProgress(value);
		}
	}
}
