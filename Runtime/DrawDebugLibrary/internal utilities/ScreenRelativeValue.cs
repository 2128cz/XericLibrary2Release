namespace DrawXXL
{
    using UnityEngine;

    /// <summary>
    /// 替代 ShapeDrawer 中成对的 xxx / xxx_relToScreen 字段。
    /// 当 activeMode 为 absolute 时使用 absolute 值，为 relativeToScreen 时使用 relativeToScreen * screenHeightReference。
    /// </summary>
    [System.Serializable]
    public struct ScreenRelativeValue
    {
        public float absolute;
        [Range(0.0f, 0.5f)] public float relativeToScreen;
        public ScaleMode activeMode;

        public enum ScaleMode { absolute, relativeToScreen }

        public ScreenRelativeValue(float abs, float rel, ScaleMode mode)
        {
            absolute = abs;
            relativeToScreen = rel;
            activeMode = mode;
        }

        public float GetValue(float screenHeightRef)
        {
            return activeMode == ScaleMode.relativeToScreen ? relativeToScreen * screenHeightRef : absolute;
        }
    }
}
