namespace DrawXXL
{
    using UnityEngine;

    [HelpURL("https://www.symphonygames.net/drawxxldocumentation/index.html")]
    [AddComponentMenu("Xeric Library/DebugDrawLibrary/Dot Product Visualizer")]
    [DefaultExecutionOrder(31000)] //negative numers are early, positive numbers are late. Range is till 32000 to both negative and positive direction.
    public class DotProductVisualizer : VisualizerParent
    {
        [SerializeField] [Range(0.0f, 0.1f)] float linesWidth = 0.0025f;
        [SerializeField] public bool colorSection_isOutfolded = false;
        [SerializeField] Color colorOfVector1_forDotProduct = DrawEngineBasics.colorOfVector1_forDotProduct;
        [SerializeField] Color colorOfVector2_forDotProduct = DrawEngineBasics.colorOfVector2_forDotProduct;
        [SerializeField] Color colorOfAngle_forDotProduct = DrawEngineBasics.colorOfAngle_forDotProduct;
        [SerializeField] Color colorOfResult_forDotProduct = DrawEngineBasics.colorOfResult_forDotProduct;

        public override void InitializeValues_onceInComponentLifetime()
        {
            TrySetTextToGameobjectName();

            customVector3Configs[0].picker_isOutfolded = true;
            customVector3Configs[0].source = CustomVector3Source.transformsForward;
            customVector3Configs[0].clipboardForManualInput = Vector3.forward;
            customVector3Configs[0].vectorInterpretation = VectorInterpretation.globalSpace;

            customVector3Configs[1].picker_isOutfolded = true;
            customVector3Configs[1].source = CustomVector3Source.manualInput;
            customVector3Configs[1].clipboardForManualInput = Vector3.right;
            customVector3Configs[1].vectorInterpretation = VectorInterpretation.globalSpace;
        }

        public override void DrawVisualizedObject()
        {
            Vector3 vector1_lhs = Get_customVector3_1_inGlobalSpaceUnits();
            Vector3 vector2_rhs = Get_customVector3_2_inGlobalSpaceUnits();

            UtilitiesDXXL_EngineBasics.Set_colorOfVector1_forDotProduct_reversible(colorOfVector1_forDotProduct);
            UtilitiesDXXL_EngineBasics.Set_colorOfVector2_forDotProduct_reversible(colorOfVector2_forDotProduct);
            UtilitiesDXXL_EngineBasics.Set_colorOfAngle_forDotProduct_reversible(colorOfAngle_forDotProduct);
            UtilitiesDXXL_EngineBasics.Set_colorOfResult_forDotProduct_reversible(colorOfResult_forDotProduct);

            DrawEngineBasics.DotProduct(vector1_lhs, vector2_rhs, GetDrawPos3D_global(), linesWidth, 0.0f, hiddenByNearerObjects);

            UtilitiesDXXL_EngineBasics.Reverse_colorOfVector1_forDotProduct();
            UtilitiesDXXL_EngineBasics.Reverse_colorOfVector2_forDotProduct();
            UtilitiesDXXL_EngineBasics.Reverse_colorOfAngle_forDotProduct();
            UtilitiesDXXL_EngineBasics.Reverse_colorOfResult_forDotProduct();
        }

    }

}
