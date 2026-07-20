using System.Collections.Generic;

namespace XericLibrary.Runtime.Chart
{
    /// <summary>
    /// 折线图数据模型的具体实现。
    ///
    /// 使用基类 ChartDataModel 的 RingBufferList&lt;double&gt; 存储 X/Y 维度数据，
    /// 提供便捷的批量写入接口（SetPoints / AddPoint）。
    /// 每次数据变更自动触发 MarkDirty 通知工具链。
    /// </summary>
    public class LineChartDataModel : ChartDataModel
    {
        public LineChartDataModel(int capacity = DefaultCapacity)
            : base(capacity)
        {
        }

        /// <summary>批量设定折线数据点（覆盖模式）</summary>
        public void SetPoints(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues)
            => SetXYValues(xValues, yValues);

        /// <summary>追加一个数据点</summary>
        public void AddPoint(double x, double y)
            => AddXYPoint(x, y);

        /// <summary>批量追加多个数据点</summary>
        public void AddPoints(IReadOnlyList<double> xValues, IReadOnlyList<double> yValues)
        {
            int min = System.Math.Min(xValues.Count, yValues.Count);
            for (int i = 0; i < min; i++)
                AddXYPoint(xValues[i], yValues[i]);
        }

        /// <summary>仅设定 Y 值（X 自动填充为自然序号）</summary>
        public void SetYValues(IReadOnlyList<double> yValues)
        {
            Y.Clear();
            for (int i = 0; i < yValues.Count; i++) Y.Add(yValues[i]);

            X.Clear();
            for (int i = 0; i < yValues.Count; i++) X.Add(i);

            MarkDirty(ChartDataFlags.DataChanged);
        }

        /// <summary>当前数据点数</summary>
        public int PointCount => System.Math.Min(X.Count, Y.Count);
    }
}
