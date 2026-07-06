namespace DrawXXL
{
    using UnityEngine;

    /// <summary>
    /// 可序列化的 CustomVector3 配置项，替代 VisualizerParent 中重复的 4 组独立字段。
    /// </summary>
    [System.Serializable]
    public struct CustomVector3Config
    {
        public VisualizerParent.CustomVector3Source source;
        public Vector3 clipboardForManualInput;
        public GameObject targetGameObject;
        public bool hasForcedAbsLength;
        public bool picker_isOutfolded;
        public float forcedAbsLength;
        [Range(0.1f, 10.0f)] public float lengthRelScaleFactor;
        public VisualizerParent.VectorInterpretation vectorInterpretation;
        public DrawBasics.CameraForAutomaticOrientation observerCamera;

        public CustomVector3Config(VisualizerParent.CustomVector3Source source, Vector3 clipboard, GameObject target, bool hasForcedAbs, bool pickerOutfolded, float forcedLen, float relScale, VisualizerParent.VectorInterpretation vecInterpretation, DrawBasics.CameraForAutomaticOrientation cam)
        {
            this.source = source;
            this.clipboardForManualInput = clipboard;
            this.targetGameObject = target;
            this.hasForcedAbsLength = hasForcedAbs;
            this.picker_isOutfolded = pickerOutfolded;
            this.forcedAbsLength = forcedLen;
            this.lengthRelScaleFactor = relScale;
            this.vectorInterpretation = vecInterpretation;
            this.observerCamera = cam;
        }

        public static CustomVector3Config Default()
        {
            return new CustomVector3Config
            {
                source = (VisualizerParent.CustomVector3Source)(-1),
                clipboardForManualInput = Vector3.zero,
                targetGameObject = null,
                hasForcedAbsLength = false,
                picker_isOutfolded = false,
                forcedAbsLength = 1.0f,
                lengthRelScaleFactor = 1.0f,
                vectorInterpretation = VisualizerParent.VectorInterpretation.globalSpace,
                observerCamera = DrawBasics.CameraForAutomaticOrientation.sceneViewCamera
            };
        }
    }

    /// <summary>
    /// 可序列化的 CustomVector2 配置项，替代 VisualizerParent 中重复的 4 组独立字段。
    /// </summary>
    [System.Serializable]
    public struct CustomVector2Config
    {
        public VisualizerParent.CustomVector2Source source;
        public Vector2 clipboardForManualInput;
        public GameObject targetGameObject;
        public bool hasForcedAbsLength;
        public bool picker_isOutfolded;
        [Range(-360.0f, 360.0f)] public float rotationFromRight;
        public float forcedAbsLength;
        [Range(0.1f, 10.0f)] public float lengthRelScaleFactor;
        public VisualizerParent.VectorInterpretation vectorInterpretation;

        public static CustomVector2Config Default()
        {
            return new CustomVector2Config
            {
                source = (VisualizerParent.CustomVector2Source)(-1),
                clipboardForManualInput = Vector2.zero,
                targetGameObject = null,
                hasForcedAbsLength = false,
                picker_isOutfolded = false,
                rotationFromRight = 0.0f,
                forcedAbsLength = 1.0f,
                lengthRelScaleFactor = 1.0f,
                vectorInterpretation = VisualizerParent.VectorInterpretation.globalSpace
            };
        }
    }
}
