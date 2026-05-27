using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.Type;

namespace Deconstruction.UI.TmpText
{
    /// <summary>
    /// TMP超链接点击管理组件
    /// </summary>
    /// <remarks>
    /// 在tmp文本中使用id块创建超链接区域，鼠标点击后，该tmp文本会发出事件。
    /// 超链接管理器可以快速管理这个事件，在事件根节点上挂载这个组件后，执行刷新操作将会自动绑定所有带有超链接的文本到此，然后由管理器发送统一的触发事件。
    /// 超链接事件由id块的id决定，当用户触发超链接后，管理器上的 onClickLink 委托会被触发。
    ///
    /// 当超链接管理器的层级下包含另一个超链接管理器时，默认不会触发这个管理器下的超链接。
    /// 你可以在使用 GetChildrenTmpText2Hyperlink 追踪时手动设置 includeRepeatedMarking 为 ture 来强制包含。
    /// GetChildrenTmpText2Hyperlink是主要的用来绑定所有文本到管理器的方法，如果你只是为了刷新文本，可以直接调用 RefreshHyperLink 。
    ///
    /// 注意在使用Xchart，XchartUI等动态创建tmp文本组件的功能时，需要频繁进行刷新。
    /// 特别是xchartui在刷新数据后，tmp文本会换成新的，这时必须重新刷新，否则表格中的超链接点击会没有反应。
    /// 
    /// 疑难解答：
    /// * 文本超链接点击没有反应。
    ///     首先检查格式是否为 <link=xxx></link> ，注意末尾一定要带上结束标记，不然tmp不识别。
    ///     其次检查是否开启了射线检测，文本上方有没有其他也处于射线检测的对象遮挡。
    ///     可以开启每个文本上的 自动解决射线检测问题 来解决。
    /// 
    /// * 文本没有被管理器识别。
    ///     检查文本中是否带有<link=>的标记，只要有一个标记就会被识别为可能的链接，然后挂载超链接脚本。
    ///     被挂载超链接脚本的文本后续无论如何都会被识别为超链接。
    /// </remarks>
    public class TMPHyperlinkManager : MonoBehaviour
    {
        /// <summary>
        /// 画布组件
        /// </summary>
#if ODIN_INSPECTOR
        [LabelText("画布*")]
#endif
        [Tooltip("组件可为空，将自动向上查找")]
        [SerializeField]
        public Canvas canvas;

#if ODIN_INSPECTOR
        [LabelText("点击超链接时回调")]
#endif
        [SerializeField]
        public UnityEvent<string> onClickLink = new UnityEvent<string>();

        /// <summary>
        /// 当前超链接管理器下的所有超链接
        /// </summary>
        [ShowInInspector]
        public List<TMPHyperlinkReceiver> Children { get; internal set; }

        /// <summary>
        /// 子项发生变更
        /// </summary>
        private bool _dirty;
        /// <summary>
        /// 当前摄像机
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// 存储链接ID与对应回调函数的映射
        /// </summary>
        private Dictionary<string, Action> _linkCallbacks = new Dictionary<string, Action>();

#if UNITY_EDITOR
        protected void OnValidate()
        {
            canvas = GetComponentInParent<Canvas>();
        }
#endif
        
        protected void Awake()
        {
            if (canvas == null)
                canvas = transform.GetParents().Startup(transform).GetComponent<Canvas>();
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

        protected void LateUpdate()
        {
            if (Children == null || Children.Count <= 0)
                return;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].autoFixRayCastTargetState)
                    Children[i].SetRayCastVisible();
            }
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
            // Debug.Log($"click link{linkID}");
            // 这里不需要处理报错，应当由发送消息的成员处理
        }

        // todo 主动查找所有的tmp组件，或是等组件唤醒后区管理它们，用来获得所有的带有标记的超链接组件

        /// <summary>
        /// 主动查找所有具有link属性的tmp组件
        /// </summary>
        /// <param name="requirementComponent">自动查找</param>
        /// <param name="forceRefesh"></param>
        public void FindAllID(bool requirementComponent = true, bool forceRefesh = false)
        {
            transform.GetChildrenBFS().GetComponentsOTON<TMPHyperlinkReceiver>();
        }

        private List<TMPHyperlinkReceiver> _tempGetChildrenHyperlink = null;

        /// <summary>
        /// 获取所有链接子项
        /// </summary>
        /// <param name="forceRefesh">强制刷新，如果刷新过一次，之后不论设定如何都将直接从缓存中返回对象，置位将跳过这个缓存</param>
        /// <param name="includeRepeatedMarking">包括重复标记（也就是是否包含另一个超链接管理器作用域下的超链接对象）</param>
        public List<TMPHyperlinkReceiver> GetChildrenHyperlink(bool forceRefesh = false,
            bool includeRepeatedMarking = false)
        {
            if (!forceRefesh && _tempGetChildrenHyperlink != null && _tempGetChildrenHyperlink.Count > 0)
                return _tempGetChildrenHyperlink;
            
            _tempGetChildrenHyperlink = transform.GetChildrenBFS()
                .GetComponentsOTON<TMPHyperlinkReceiver>().ToList();
            
            return _tempGetChildrenHyperlink;
        }

        private List<TMPHyperlinkReceiver> _tempGetChildrenTmpText2Hyperlink = null;

        /// <summary>
        /// 获取所有tmp_text，然后根据规则返回链接
        /// </summary>
        /// <param name="forceRefesh">强制刷新，如果刷新过一次，之后不论设定如何都将直接从缓存中返回对象，置位将跳过这个缓存</param>
        /// <param name="requirementHyperlink">强制给带有link标记的对象加上超链接组件</param>
        /// <param name="includeRepeatedMarking">是否包含重复引用的链接（是否包含当前超链接管理器下其他超链接管理器作用域下的超链接对象）</param>
        /// <returns></returns>
        /// <remarks>
        /// 这个操作会扫描所有对象，然后在具有超链接特征的文本组件上生成超链接管理器。  
        /// 超链接响应的前提是开启 Raycast Target 选项。
        /// </remarks>
        public List<TMPHyperlinkReceiver> GetChildrenTmpText2Hyperlink(
            bool forceRefesh = false,
            bool requirementHyperlink = true,
            bool includeRepeatedMarking = false)
        {
            if (!forceRefesh && 
                _tempGetChildrenTmpText2Hyperlink != null && 
                _tempGetChildrenTmpText2Hyperlink.Count > 0)
                return _tempGetChildrenTmpText2Hyperlink;
            
            var children = (includeRepeatedMarking
                ? transform.GetChildrenBFS()
                : transform.GetChildrenBFS<TMPHyperlinkManager>()).ToList();
    
            var result = ListPool<TMPHyperlinkReceiver>.Get();
            var hyperlinkType = typeof(TMPHyperlinkReceiver);
            var tmpTextType = typeof(TMP_Text);
    
            // 预先分配足够的容量，减少扩容
            result.Capacity = Mathf.Min(children.Count(), 10);
    
            foreach (var child in children)
            {
                // 尝试从缓存获取组件
                var hyperlink = child.GetComponent(hyperlinkType) as TMPHyperlinkReceiver;
                if (hyperlink != null)
                {
                    result.Add(hyperlink);
                    continue;
                }

                if (!requirementHyperlink)
                    continue;
                
                // 尝试获取TMP_Text组件
                var tmp = child.GetComponent(tmpTextType) as TMP_Text;
                if (tmp != null && tmp.text.MatchRichTextLinkID())
                {
                    result.Add(child.gameObject.AddComponent<TMPHyperlinkReceiver>());
                }
            }

            _dirty = true;
            if (Children != null)
                ListPool<TMPHyperlinkReceiver>.Release(Children);
            Children = result;
            return result;
        }

        /// <summary>
        /// 直接刷新所有超链接
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void RefreshHyperLink()
            => GetChildrenTmpText2Hyperlink(true);
    }
}