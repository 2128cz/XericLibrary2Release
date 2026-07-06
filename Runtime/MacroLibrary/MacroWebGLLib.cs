#if UNITY_WEBGL
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.Type;
using XericLibrary.Runtime.Type.Base;

namespace XericLibrary.Runtime.MacroLibrary
{
	/// <summary>
	/// Web 平台文件句柄。
	/// </summary>
	public class WebGLHandle : MacroFile.CrossPlatformFileHandle
	{
		public override string PlatformName => "WebGL";
		public static WebGLHandle handle = new WebGLHandle();
		public static float GlobalTimeout = 10f;
		public float Timeout = -1f;

		private float timeout => Timeout >= 0 ? Timeout : GlobalTimeout;

		public WebGLHandle() : base(RuntimePlatform.WebGLPlayer)
		{
			if (handle == null)
				handle = this;
		}

#if XERIC_WEBGL_DOWNLOAD
		[DllImport("__Internal")]
		private static extern void Download(string base64str, string fileName);
#endif

#if XERIC_WEBGL_FILEBROWSER
		[DllImport("__Internal")]
		private static extern void OpenFileDialog(string gameObjectName, string callbackMethodName);

		[DllImport("__Internal")]
		private static extern void OpenFileDialogBinary(string gameObjectName, string callbackMethodName);
#endif

#if XERIC_WEBGL_JSBRIDGE
		/// <summary>
		/// 从 JS 端发送消息到 Unity（对应 xeric-jsbridge.js 中的 SendToUnity）
		/// </summary>
		public static void SendToUnity(string gameObject, string method, string message)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			SendMessageToUnity(gameObject, method, message);
#else
			// Editor 回退：尝试直接查找并调用
			var go = GameObject.Find(gameObject);
			if (go != null)
				go.SendMessage(method, message, SendMessageOptions.DontRequireReceiver);
#endif
		}

		[DllImport("__Internal")]
		private static extern void SendMessageToUnity(string gameObject, string method, string message);

		/// <summary>
		/// 通知 JS 端 Canvas 获得焦点（Unity 可捕获输入）
		/// </summary>
		public static void NotifyCanvasFocus()
		{
			// JS 端 OnCanvasFocus() 会反向调用 Unity.SendMessage
		}

		/// <summary>
		/// 通知 JS 端 Canvas 失去焦点（前端可处理输入）
		/// </summary>
		public static void NotifyCanvasBlur()
		{
			// JS 端 OnCanvasBlur() 会反向调用 Unity.SendMessage
		}
#endif

		public override bool ReadTextFromFile(string absolutePathname, out string content)
		{
			var waitFlag = false;
			var result = string.Empty;

			SingleMonoBase<WebFileLoader>.GlobalInstance.DirectlyLoadFile(absolutePathname, s => result = s, s =>
			{
				XericLogger.CCError($"WebGl文件同步加载失败：{s}");
			}, () => { waitFlag = true; });

			// 等待文件加载完成
			while (!waitFlag)
			{
				// 让出控制权，避免完全阻塞
				Debug.LogWarning("不建议在WebGl端使用同步文件读取方式，当前已让渡主线程控制权");
				Thread.Sleep(0);
			}
			content = result;
			return waitFlag;
		}

#if XERIC_WEBGL_DOWNLOAD
		public override bool WriteTextIntoFile(string absolutePathname, string content)
		{
			try
			{
				Download(content, absolutePathname);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError($"无法下载文件{absolutePathname}:{e.Message}");
				return false;
			}
		}
#endif

		public override async Task<bool> AsyncReadTextFromFile(string absolutePathname, Action<string> complete, Action<string> loadError)
		{
			var waitFlag = false;
			var result = false;
			SingleMonoBase<WebFileLoader>.GlobalInstance.DirectlyLoadFile(absolutePathname,
			s =>
			{
				result = true;
				complete?.Invoke(s);
			}, loadError, () => { waitFlag = true; });
			await MacroAsync.WaitUntil(() => waitFlag, () => "WebGl文件读取失败", timeout);
			return result;
		}

#if XERIC_WEBGL_DOWNLOAD
		public override async Task<bool> AsyncWriteTextIntoFile(string absolutePathname, string content, Action complete, Action<string> loadError)
		{
			await Task.Yield();
			if (WriteTextIntoFile(absolutePathname, content))
			{
				complete?.Invoke();
			}
			else
			{
				loadError?.Invoke("WebGL文件写入失败");
				XericLogger.CCError("WebGL文件写入失败");
			}
			await Task.CompletedTask;
			return false;
		}
#endif

		public override string CombinePath(string pathA, string pathB)
		{
			try
			{
				if (!pathA.EndsWith("/")) // 确保路径以斜杠结尾
					pathA += "/";
				var uri = new Uri(new Uri(pathA, UriKind.Absolute), new Uri(pathB, UriKind.RelativeOrAbsolute));
				/* 不能使用uri自己的
				 * 比如 uri.IsAbsoluteUri ? uri.AbsolutePath : uri.LocalPath;
				 * 在这种情况时：
				 * http://192.168.0.39:8080/DGJ/StreamingAssets/ + UriConfig.json
				 * 结果是 => /DGJ/StreamingAssets/UriConfig.json  用是AbsolutePath
				 * 所以要么直接A + B
				 * 要么直接Tostring。
				 */
				Debug.Log($"Web路径拼接处理结果浏览：{pathA}\n{pathB}\n{uri}\n");
				return uri.ToString();
			}
			catch (UriFormatException ex)
			{
				// 处理URI格式错误
				XericLogger.CCError($"Web路径拼接失败: {ex.Message}");
				return string.Empty;
			}
		}
	}
}
#endif
