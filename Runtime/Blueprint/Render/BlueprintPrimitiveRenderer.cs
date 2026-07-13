using UnityEngine;
using XericLibrary.Runtime.UIGraph;

namespace XericLibrary.Runtime.Blueprint.Render
{
	/// <summary>
	/// 最简图元渲染器 —— 继承 PrimitiveCacheRendererBase，无任何额外设定。
	/// 供蓝图渲染工具使用，避免 UICurveRenderer 等自带的默认配置干扰。
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class BlueprintPrimitiveRenderer : PrimitiveCacheRendererBase
	{
	}
}
