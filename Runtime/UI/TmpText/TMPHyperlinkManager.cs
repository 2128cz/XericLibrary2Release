using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.Type;

namespace Deconstruction.UI.TmpText
{   
    /// <summary>
    /// TMP超链接点击管理组件
    /// </summary>
    public class TMPHyperlinkManager : MonoBehaviour
    {
        /// <summary>
        /// 画布组件
        /// </summary>
#if ODIN_INSPECTOR
        [LabelText("画布*")]
#endif
        [Tooltip("组件可为空，将自动向上查找")]
        [SerializeField] public Canvas canvas;

#if ODIN_INSPECTOR
        [LabelText("点击超链接时回调")]
#endif
        [SerializeField]
        private UnityEvent<string> _onClickLink;
        public Action<string> onClickLink;
        
        /// <summary>
        /// 当前摄像机
        /// </summary>
        private Camera _camera; 
        /// <summary>
        /// 存储链接ID与对应回调函数的映射
        /// </summary>
        private Dictionary<string, Action> _linkCallbacks = new Dictionary<string, Action>();

        protected void Awake()
        {
            if (canvas == null)
                transform.GetParents().Startup(transform).GetComponent(out canvas);
            if (canvas == null)
            {
                Debug.LogError("TMP文本对象必须在Canvas下才能使用超链接功能");
                return;
            }
        
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                _camera = null;
            else
                _camera = canvas.worldCamera;
            
            
        }

        /// <summary>
        /// 注册链接回调函数
        /// </summary>
        public void RegisterLinkCallback(string linkID, Action callback)
        {
            if (_linkCallbacks.ContainsKey(linkID))
            {
                Debug.LogWarning($"链接ID '{linkID}' 已存在，将覆盖之前的回调函数");
                _linkCallbacks[linkID] = callback;
            }
            else
            {
                _linkCallbacks.Add(linkID, callback);
            }
        }
    
        /// <summary>
        /// 注销链接回调函数
        /// </summary>
        public void UnregisterLinkCallback(string linkID)
        {
            if (_linkCallbacks.ContainsKey(linkID))
            {
                _linkCallbacks.Remove(linkID);
            }
        }
    
        /// <summary>
        /// 处理链接点击事件
        /// </summary>
        public void HandleLinkClick(string linkID)
        {
            if (_linkCallbacks.TryGetValue(linkID, out Action callback))
            {
                callback?.Invoke();
                Debug.Log($"执行链接 '{linkID}' 的回调函数");
            }
            else
            {
                Debug.LogWarning($"未找到链接ID '{linkID}' 的回调函数");
            }
        }

        
        /// <summary>
        /// 检查坐标下的超链接索引
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        internal int CheckLinkIndex(TMP_Text text, Vector3 position)
        {
            return TMP_TextUtilities.FindIntersectingLink(text, position, _camera);
        }
        /// <summary>
        /// 检查坐标下的超链接id信息
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="linkInfo"></param>
        /// <returns></returns>
        internal bool CheckLinkInfo(TMP_Text text, Vector3 position, out TMP_LinkInfo linkInfo)
        {
            var index = CheckLinkIndex(text, position);
            if (index != -1)
            {
                linkInfo = text.textInfo.linkInfo[index];
                return true;
            }
            linkInfo = default;
            return false;
        }
        /// <summary>
        /// 检查坐标下的超链接id
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="linkID"></param>
        /// <returns></returns>
        internal bool CheckLinkID(TMP_Text text, Vector3 position, out string linkID)
        {
            if (CheckLinkInfo(text, position, out var info))
            {
                linkID = info.GetLinkID();
                return true;
            }
            linkID = string.Empty;
            return false;
        }

        /// <summary>
        /// 当任意tmp对象超链接点击时调用，从这里发送事件
        /// </summary>
        /// <param name="linkID"></param>
        internal void OnAnyLinkClick(string linkID)
        {
            onClickLink?.Invoke(linkID);
            _onClickLink.Invoke(linkID);
            Debug.Log($"click link{linkID}");
            // 这里不需要处理报错，应当由发送消息的成员处理
        }
        
        // todo 主动查找所有的tmp组件，或是等组件唤醒后区管理它们，用来获得所有的带有标记的超链接组件
    }
}