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
	public class WebGLHandle : CrossPlatformFileHandle
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

		[DllImport("__Internal")]
		private static extern void Download(string base64str, string fileName);

		//[DllImport("__Internal")]
		//private static extern void OpenFileDialog(string gameObjectName, string callbackMethodName);

		//[DllImport("__Internal")]
		//private static extern void OpenFileDialogBinary(string gameObjectName, string callbackMethodName);

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
