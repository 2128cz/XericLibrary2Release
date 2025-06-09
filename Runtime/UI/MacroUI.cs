using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using XericLibrary.Runtime.CustomEditor;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif

using Object = UnityEngine.Object;

namespace XericLibrary.Runtime.MacroLibrary
{
    /// <summary>
    /// UI相关的扩展
    /// </summary>
    public static class MacroUI
    {
        #region toggle 扩展

        private static FieldInfo togglesFieldInfo = typeof(ToggleGroup).GetField("m_Toggles",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static List<Toggle> GetToggles(this ToggleGroup toggleGroup)
        {
            if (toggleGroup == null)
            {
                throw new ArgumentNullException(nameof(toggleGroup));
            }

            if (togglesFieldInfo == null)
            {
                throw new InvalidOperationException("Unable to access the 'm_Toggles' field.");
            }

            var toggles = togglesFieldInfo.GetValue(toggleGroup) as List<Toggle>;
            if (toggles == null)
            {
                throw new InvalidCastException("The 'm_Toggles' field is not of type List<Toggle>.");
            }

            return toggles;
            // return (List<Toggle>)togglesFieldInfo.GetValue(toggleGroup);
        }

        /// <summary>
        /// 获取当前单选项组中激活的索引
        /// </summary>
        /// <param name="toggleGroup"></param>
        /// <returns></returns>
        [Obsolete("索引依赖单选项自身在大纲中的顺序，在运行时顺序可能与编辑时不一致，请使用ToggleMapping")]
        public static int GetActiveToggleIndex(this ToggleGroup toggleGroup)
        {
            var index = -1;
            foreach (var toggle in toggleGroup.GetToggles())
            {
                index++;
                if (toggle.isOn)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// 获取当前单选项组的数量
        /// </summary>
        /// <param name="toggleGroup"></param>
        /// <returns></returns>
        public static int GetToggleCount(this ToggleGroup toggleGroup)
        {
            return toggleGroup.GetToggles().Count;
        }


        /// <summary>
        /// 在单选项组上注册一个事件，当组中的任意成员变成激活状态时调用（其他的不会发生调用）。
        /// </summary>
        /// <param name="toggleGroup"></param>
        /// <param name="onToggleChange"></param>
        public static void OnToggleGroupChangeEvent(this ToggleGroup toggleGroup, Action<Toggle> onToggleChange)
        {
            foreach (var toggle in toggleGroup.GetToggles())
            {
                toggle.onValueChanged.AddListener(a =>
                {
                    if (!a) return;
                    onToggleChange?.Invoke(toggle);
                });
            }
        }

        /// <summary>
        /// 清空单选项组中的所有事件（与注册所有事件对应，但那个事件没法单独注销）
        /// </summary>
        /// <param name="toggleGroup"></param>
        public static void RemoveToggleGroupChangeEvent(this ToggleGroup toggleGroup)
        {
            foreach (var toggle in toggleGroup.GetToggles())
            {
                toggle.onValueChanged.RemoveAllListeners();
            }
        }

        #region toggle 索引

        /// <summary>
        /// toggle映射集
        /// <code>
        /// toggle映射集必须使用unity序列化管理，否则和直接使用toggleGroup没区别，目的是解决toggleGroup在打包后大纲视图的索引可能发生错位的问题。
        /// </code>
        /// </summary>
        [Serializable]
        public class ToggleMapping : IEnumerable<Toggle>
        {
            #region 事件委托

            /// <summary>
            /// 在单选项目切换时产生回调，返回选中的单选项目在组中的引用
            /// </summary>
            public Action<Toggle> OnAnyToggleSwitchOn;

            /// <summary>
            /// 在单选项目切换时产生回调，返回选中的单选项目在组中的索引
            /// </summary>
            public Action<int> OnAnyToggleIndexSwitchOn;

            #endregion

            #region 字段属性
#if ODIN_INSPECTOR
            [LabelText("单选组")] 
#endif
            public ToggleGroup ToggleGroup;
#if ODIN_INSPECTOR  
            [SerializeField, LabelText("编辑单选项目顺序")] [ListDrawerSettings(OnTitleBarGUI = "GetAndSortToggle")]
#endif
            private List<Toggle> toggleList = new List<Toggle>();

            // 当前选中的项目
            private int nowSelectToggleIndex = 0;
            private Toggle nowSelectToggle = null;


            public Transform TogglesContext => ToggleGroup.transform;

            /// <summary>
            /// 当前选中的toggle索引
            /// </summary>
            public int NowSelectToggleIndex => nowSelectToggleIndex;

            /// <summary>
            /// 当前选中的toggle
            /// </summary>
            public Toggle NowSelectToggle => nowSelectToggle;


            /// <summary>
            /// 获取并给列表排序(顺序不一定与拼音有关)
            /// </summary>
            public void GetAndSortToggle()
            {
#if UNITY_EDITOR && ODIN_INSPECTOR
                // 自动获取并排序
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
                {
                    GetSortToggle();
                }
                // 反转顺序
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.TriangleDown))
                {
                    toggleList.Reverse();
                }
#else
                 GetSortToggle();
#endif
                void GetSortToggle()
                {
                    var newToggleList = MacroSort.FullCharacterOrderSort(ToggleGroup.GetToggles(), a => a.name)
                        .ToList();
                    if (newToggleList.Count <= 0 || newToggleList == null)
                        Debug.LogError("如果无法更新获取自动排序toggle，可能是因为toggleGroup被隐藏了，手动将其激活后再获取即可。");
                    else
                        toggleList = newToggleList;
                }
            }

            #endregion

            #region 接口

            /// <summary>
            /// 获取索引下的单选项组件
            /// </summary>
            /// <param name="index"></param>
            public Toggle this[int index] => toggleList[index];

            public int Count => toggleList.Count;

            public IEnumerator<Toggle> GetEnumerator()
            {
                return toggleList.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region 方法

            /// <summary>
            /// 初始化
            /// <code>
            /// 自动将所有的按键绑定回调事件；如果作为刷新函数调用，请确保已经完全组索引。
            /// 或者可以直接清空旧项目(CleanToggle)，然后逐个添加(AddToggle)
            /// </code>
            /// </summary>
            public void Initialize()
            {
                // 未指定组时，说明压根没用这部分功能，用不着初始化。
                if (ToggleGroup == null)
                    return;

                // 防呆警告
                if (toggleList.Count <= 0)
                {
                    var toggles = ToggleGroup.GetToggles();
                    if (toggles.Count != toggleList.Count)
                    {
                        toggleList = toggles;
                        Debug.LogWarning($"在初始化单选项组时，{ToggleGroup.name}并未预先指定索引顺序，将默认使用大纲顺序。");
                    }
                }

                // 事件初始化
                for (int i = 0; i < toggleList.Count; i++)
                {
                    var toggle = toggleList[i];

                    ToggleAddEvent(toggle);

                    if (toggle.isOn)
                    {
                        nowSelectToggleIndex = i;
                        nowSelectToggle = toggle;
                    }
                }

                if (nowSelectToggle == null)
                    SetToggleOn(0);
            }

            /// <summary>
            /// 添加一个toggle
            /// </summary>
            /// <param name="t"></param>
            /// <returns>返回toggle的索引</returns>
            public int AddToggle(Toggle t)
            {
                ToggleAddEvent(t);

                var resultIndex = toggleList.Count;
                toggleList.Add(t);
                return resultIndex;
            }

            /// <summary>
            /// 移除一个toggle，这不会影响其他toggle的索引，但此处移除的位置会为空。
            /// <code>
            /// 注 ：这不会销毁toggle。
            /// </code>
            /// </summary>
            /// <param name="t"></param>
            /// <returns>是否成功移除toggle</returns>
            public bool RemoveToggle(Toggle t)
            {
                var index = toggleList.IndexOf(t);
                if (index < 0)
                    return false;

                t.onValueChanged.RemoveAllListeners();
                toggleList[index] = null;
                return true;
            }

            /// <summary>
            /// 清除toggle
            /// </summary>
            /// <param name="allowDestroy">是否同时销毁所有toggle</param>
            public void CleanToggle(bool allowDestroy)
            {
                foreach (var t in toggleList)
                {
                    t.onValueChanged.RemoveAllListeners();
                    if (allowDestroy)
                        Object.Destroy(t);
                }

                toggleList.Clear();
            }


            /// <summary>
            /// toggle注册的事件，只有当按下时才需要调用此事件。
            /// </summary>
            /// <param name="t"></param>
            private void ToggleAddEvent(Toggle t)
            {
                t.onValueChanged.AddListener(a =>
                {
                    if (a) ToggleRegister(t);
                });
            }

            /// <summary>
            /// toggle注册的事件，只有当按下时才需要调用此事件。
            /// </summary>
            /// <param name="t"></param>
            private void ToggleRegister(Toggle t)
            {
                nowSelectToggle = t;
                nowSelectToggleIndex = GetIndex(t);
                OnAnyToggleSwitchOn?.Invoke(t);
                OnAnyToggleIndexSwitchOn?.Invoke(nowSelectToggleIndex);
            }


            /// <summary>
            /// 获取toggle代表的索引
            /// </summary>
            /// <param name="target"></param>
            /// <returns>如果这个toggle不存在于当前的单选项组中，返回-1</returns>
            public int GetIndex(Toggle target)
            {
                return toggleList.IndexOf(target);
            }

            /// <summary>
            /// 获取toggle代表的索引， 如果toggle不存在于这个映射集中，将返回否
            /// </summary>
            /// <param name="target"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            public bool TryGetIndex(Toggle target, out int index)
            {
                index = toggleList.IndexOf(target);
                return index >= 0;
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="target"></param>
            public void SetToggleOn(Toggle target)
            {
                target.isOn = true;
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="index"></param>
            public void SetToggleOn(int index)
            {
                if (0 < index && index < toggleList.Count)
                {
                    SetToggleOn(toggleList[index]);
                }
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="target"></param>
            public void SetToggleOnWithoutNotify(Toggle target)
            {
                target.SetIsOnWithoutNotify(true);
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="index"></param>
            public void SetToggleOnWithoutNotify(int index)
            {
                if (0 < index && index < toggleList.Count)
                {
                    SetToggleOnWithoutNotify(toggleList[index]);
                }
            }


            /// <summary>
            /// 清除映射结构(不会清除toggle实例)
            /// </summary>
            public void Clear()
            {
                ToggleGroup.RemoveToggleGroupChangeEvent();
                toggleList.Clear();
            }

            /// <summary>
            /// 清除映射结构，并销毁所有toggle组件 
            /// </summary>
            public void RemoveAllToggle()
            {
                for (int i = toggleList.Count - 1; i >= 0; i--)
                {
                    Object.Destroy(toggleList[i]);
                }

                Clear();
            }

            #endregion
        }

        #endregion

        #endregion

        #region 按钮扩展

        /// <summary>
        /// 所有按钮注册一个事件
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="targetEvent"></param>
        public static void RegisterOnClickEvent(this IEnumerable<Button> buttons, UnityAction targetEvent)
        {
            foreach (var button in buttons)
            {
                button.onClick.AddListener(targetEvent);
            }
        }

        /// <summary>
        /// 所有按钮注销一个事件
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="targetEvent"></param>
        public static void LogoutOnClickEvent(this IEnumerable<Button> buttons, UnityAction targetEvent)
        {
            foreach (var button in buttons)
            {
                button.onClick.RemoveListener(targetEvent);
            }
        }

        /// <summary>
        /// 设置所有按钮的可交互性
        /// </summary>
        /// <param name="buttons"></param>
        /// <param name="interactable"></param>
        public static void SetButtonInteractable(this IEnumerable<Button> buttons, bool interactable)
        {
            foreach (var button in buttons)
            {
                button.interactable = interactable;
            }
        }

        #endregion

        #region 文本组件扩展

        public static void SetText(this IEnumerable<TextMeshProUGUI> texts, string text)
        {
            foreach (var t in texts)
            {
                t.text = text;
            }
        }

        #endregion

        #region UI变换获取扩展

        /// <summary>
        /// 获取一个组件的矩形变换组件
        /// </summary>
        /// <param name="target"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static RectTransform RectTransform<T>(this T target)
            where T : Component
            => target.transform as RectTransform;

        #endregion
    }
}