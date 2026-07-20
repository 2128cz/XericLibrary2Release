using System.Collections.Generic;
using UnityEngine;

namespace XericLibrary.Runtime.Chart
{
    /// <summary>
    /// 折线图图表组件。
    ///
    /// 继承 ChartMainBehaviour，是"开箱即用"的折线图表。
    /// 功能上相当于一个完整的折线图控件，集成背景网格和折线渲染。  
    ///
    /// 自动执行以下初始化：
    ///   1. 创建 LineChartDataModel 数据模型
    ///   2. 添加 ChartToolAxisBgUgui（背景网格，order=-100）
    ///   3. 添加 ChartToolLineUgui（折线渲染，order=0）
    ///   4. 数据模型绑定到所有工具
    /// </summary>
    [AddComponentMenu("Xeric Library/Chart/Line Chart", 11)]
    public class XLineChart : ChartMainBehaviour
    {
        [Header("Line Chart 设置")]
        [SerializeField] private Color m_LineColor = new Color(0.33f, 0.67f, 1f, 1f);
        [SerializeField] private float m_LineThickness = 4f;
        [SerializeField] private Vector4 m_Margin = new Vector4(50f, 40f, 20f, 20f);
        [SerializeField] private int m_VisibleItemCount = 100;
        [SerializeField] private bool m_SmoothLine;

        public ChartToolAxisBgUgui AxisTool { get; private set; }
        public ChartToolLineUgui LineTool { get; private set; }

        private void Start()
        {
            DataModel = new LineChartDataModel();

            AxisTool = AddTool<ChartToolAxisBgUgui>("ChartTool_AxisBgUgui", -100);
            if (AxisTool != null)
            {
                AxisTool.DataModel = DataModel;
                AxisTool.SetViewRange(0, ChartViewRange.Recent(m_VisibleItemCount));
                AxisTool.SetViewRange(1, ChartViewRange.ValueMode(-100f, 100f));

                var bgImg = AxisTool.BackgroundImage;
                if (bgImg != null)
                {
                    var rt = bgImg.rectTransform;
                    rt.offsetMin = new Vector2(m_Margin.x, m_Margin.y);
                    rt.offsetMax = new Vector2(-m_Margin.z, -m_Margin.w);
                }
            }

            LineTool = AddTool<ChartToolLineUgui>("ChartTool_LineUgui", 0);
            if (LineTool != null)
            {
                LineTool.DataModel = DataModel;
                LineTool.SetViewRange(0, ChartViewRange.Recent(m_VisibleItemCount));
                LineTool.SetViewRange(1, ChartViewRange.ValueMode(-100f, 100f));
                LineTool.LineColor = m_LineColor;
                LineTool.LineThickness = m_LineThickness;
                LineTool.Margin = m_Margin;
                LineTool.SmoothLine = m_SmoothLine;
            }
        }

        private void OnValidate()
        {
            if (LineTool != null)
            {
                LineTool.LineColor = m_LineColor;
                LineTool.LineThickness = m_LineThickness;
                LineTool.Margin = m_Margin;
                LineTool.SmoothLine = m_SmoothLine;
            }
        }

        public void AddPoint(double x, double y)
            => (DataModel as LineChartDataModel)?.AddPoint(x, y);

        public void SetPoints(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues)
            => (DataModel as LineChartDataModel)?.SetPoints(xValues, yValues);

        public void SetYValues(IReadOnlyList<double> yValues)
            => (DataModel as LineChartDataModel)?.SetYValues(yValues);

        public void ClearData()
            => DataModel?.ClearAll();
    }
}
