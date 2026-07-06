namespace DrawXXL
{
    using UnityEngine;

    public class BezierSplineDrawerBase : VisualizerParent
    {
        [SerializeField] protected Color color = DrawBasics.defaultColor;
        [SerializeField] protected float lineWidth = 0.0f;
        [SerializeField] protected int straightSubDivisionsPerSegment = 50;
        protected float textSize = 0.1f;
    }
}
