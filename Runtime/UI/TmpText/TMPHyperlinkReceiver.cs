using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using XericLibrary.Runtime.MacroLibrary;

namespace Deconstruction.UI.TmpText
{
    /// <summary>
    /// 附加到TMP文本对象的组件，用于捕获点击事件并转发给管理器
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMPHyperlinkReceiver : MonoBehaviour, IPointerClickHandler
    {
        private TextMeshProUGUI _tmpText;
        
#if ODIN_INSPECTOR
        [LabelText("超链接管理器*")]
#endif
        [Tooltip("组件可为空，将自动向上查找")]
        public TMPHyperlinkManager manager;

#if ODIN_INSPECTOR
        [LabelText("点击超链接时回调")]
#endif
        [SerializeField]
        public UnityEvent<string> onClickLink = new UnityEvent<string>();

        /// <summary>
        /// 获取所有超链接id
        /// </summary>
        public IEnumerable<string> AllLinkID
        {
            get
            {
                if (_tmpText == null || string.IsNullOrEmpty(_tmpText.text))
                    yield break;
                foreach (var link in _tmpText.text.MatchesRichTextLinkID())
                    yield return link.id;
            }
        }
        
        /// <summary>
        /// 获取所有超链接
        /// </summary>
        public IEnumerable<(string id, string block)> AllLinkIDWithContext
        {
            get
            {
                if (_tmpText == null || string.IsNullOrEmpty(_tmpText.text))
                    yield break;
                foreach (var link in _tmpText.text.MatchesRichTextLinkID())
                    yield return link;
            }
        }
        
        
        private void Awake()
        {
            _tmpText = GetComponent<TextMeshProUGUI>();
            if (manager == null)
                transform.GetParents().Startup(transform).GetComponent(out manager);
            if (manager == null)
                throw new Exception("TMP文本超链接集成捕获组件必须放在具有管理器的父级下");
        }
    
        public void OnPointerClick(PointerEventData eventData)
        {
            ClickPoint(eventData.position);
        }

        /// <summary>
        /// 在坐标处点击超链接文本
        /// </summary>
        /// <param name="position"></param>
        public void ClickPoint(Vector3 position)
        {
            if (!manager.CheckLinkID(_tmpText, position, out var linkID)) 
                return;
            try
            {
                onClickLink?.Invoke(linkID);
                manager.OnAnyLinkClick(linkID);
            }
            catch (Exception e)
            {
                Debug.LogError($"点击 {_tmpText.text.LimitStringLength(32)} 的超链接 {linkID} 时出错：\n{e.Message}\n{e.Data}", this);
            }
        }
    }

}