using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.CustomEditor;

namespace Deconstruction.Runtime.UI
{
    /// <summary>
    /// 获取屏幕缩放
    /// </summary>
    public class AutoCanvasScale : MonoBehaviour
    {
        #region 字段属性
        
        public CanvasScaler canvasScaler;
        
        [Rename("设计尺寸")]
        public Vector2 StandardSize = new Vector2(1920,1080);
        
        public Vector2 ScaleMinMax = new Vector2(0.5f, 2);

        #endregion

        #region 生命周期
        
        private void OnValidate()
        {
            FindCanvasScaler();
            GetScreenScale();
        }

        private void Awake()
        {
            FindCanvasScaler();
        }

        private void LateUpdate()
        {
            canvasScaler.scaleFactor = GetScreenScale();
        }

        #endregion

        #region 方法

        private void FindCanvasScaler()
        {
            if (canvasScaler == null)
                canvasScaler = transform.GetComponent<CanvasScaler>();
        }
        
        private float GetScreenScale()
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            // Debug.Log(canvasScaler.referenceResolution);
            var sf = screenSize / StandardSize;
            return Mathf.Clamp(Mathf.Min(sf.x, sf.y), ScaleMinMax.x, ScaleMinMax.y);
        }
        
        #endregion
    }
}