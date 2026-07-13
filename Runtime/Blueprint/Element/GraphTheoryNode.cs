using UnityEngine;
using XericLibrary.Runtime.Blueprint;

namespace XericLibrary.Runtime.Blueprint.Element
{
	/// <summary>
	/// 图论测试节点 —— 用于图论蓝图测试的简单节点。
	/// 包含一个输入端口和一个输出端口，节点颜色由外部指定。
	/// </summary>
	public class GraphTheoryNode : BlueprintNode
	{
		private readonly string _title;
		private readonly string _category;

		public override string NodeTitle
		{
			get { return _title; }
		}

		public override string Category
		{
			get { return _category; }
		}

		/// <summary>
		/// 创建一个图论测试节点。
		/// </summary>
		/// <param name="title">节点标题</param>
		/// <param name="color">节点边框颜色</param>
		public GraphTheoryNode(string title, Color color)
		{
			_title = title;
			_category = "Test";
			NodeColor = color;
			NodeBGColor = new Color(0.15f, 0.15f, 0.15f, 1f);
			NodeTextColor = Color.gray;

			AddInputPort("In", "any");
			AddOutputPort("Out", "any");
		}

		/// <summary>
		/// 创建一个带指定颜色的图论测试节点。
		/// </summary>
		/// <param name="title">节点标题</param>
		/// <param name="color">节点边框颜色</param>
		/// <param name="bgColor">节点背景颜色</param>
		public GraphTheoryNode(string title, Color color, Color bgColor)
		{
			_title = title;
			_category = "Test";
			NodeColor = color;
			NodeBGColor = bgColor;
			NodeTextColor = Color.gray;

			AddInputPort("In", "any");
			AddOutputPort("Out", "any");
		}

		public override void Execute(BlueprintExecutionContext context)
		{
			// 测试节点，无执行逻辑
		}

		public override System.Collections.Generic.IReadOnlyList<System.Type> GetRequiredToolTypes()
		{
			return System.Array.Empty<System.Type>();
		}
	}
}
