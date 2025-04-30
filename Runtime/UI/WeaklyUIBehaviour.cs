using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.MacroLibrary;

namespace Deconstruction.Runtime.UI
{
    /// <summary>
    /// 提供ui界面上的脚本
    /// </summary>
    public class WeaklyUIBehaviour : MonoBehaviour
    {
        #region 字段属性

        /// <summary>
        /// 此处的矩形变换组件
        /// </summary>
        public RectTransform rectTransform { get; private set; }

        /// <summary>
        /// 此组件所在的Canvas
        /// </summary>
        public Canvas CanvasRoot { get; private set; }

        /// <summary>
        /// 此组件所在的Canvas下的CanvasScaler
        /// </summary>
        public CanvasScaler CanvasRootScaler { get; private set; }

        private bool isFirstOnEnable = false;
        private bool isFirstOnEnableStart = false;
        
        #endregion
        
        /// <summary>
        /// 在加载脚本实例时调用
        /// </summary>
        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;
            CanvasRoot = transform.GetParents()
                .Select(t => t.GetComponent<Canvas>())
                .FirstOrDefault(a => a != null);
            if (CanvasRoot != null)
                CanvasRootScaler = CanvasRoot.GetComponent<CanvasScaler>();
            else
            {
                Debug.LogError($"成员{name}不是一个有效的ui成员，或没有放置在canvas上，致使其无法找到ui根组件");
            }

            isFirstOnEnable = true;
            isFirstOnEnableStart = true;
        }

        protected virtual void OnEnable()
        {
            if (isFirstOnEnable)
            {
                isFirstOnEnable = false;
                OnFirstEnable();
            }
        }

        protected virtual void OnDisable()
        {
            
        }

        /// <summary>
        /// 首次行为激活时调用（用于界面类型元素在运行期间首次初始化）
        /// </summary>
        protected virtual void OnFirstEnable()
        {
            
        }

        protected virtual void Start()
        {
            if (isFirstOnEnableStart)
            {
                isFirstOnEnableStart = false;
                OnFirstEnableStart();
            }
        }
        
        /// <summary>
        /// 首次行为激活开始时调用（用于界面类型元素在运行期间首次初始化）
        /// </summary>
        protected virtual void OnFirstEnableStart()
        {
            
        }
    }
}
