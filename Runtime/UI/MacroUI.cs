using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deconstruction.UI.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using UnityEngine.Serialization;
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

        /// <summary>
        /// 获取给定单选项组中的所有单选项目
        /// </summary>
        /// <code>
        /// 注意：操作具有一定的危险性，你可以自行制作这个列表对象的拷贝，但注意不要直接对返回的列表对象进行操作。
        /// </code>
        /// <param name="toggleGroup"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidCastException"></exception>
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

        [Serializable]
        public class ToggleValueMapping<T> : ToggleMapping
        {
            #region 事件委托

            public Action<Toggle, T> OnAnyToggleValueSwitchOn;

            #endregion

            #region 字段属性

#if ODIN_INSPECTOR
            [SerializeField, LabelText("编辑单选项目值")]
#endif
            private List<T> toggleValue;

            #endregion

            public override void BakeToggleGroupItems()
            {
                base.BakeToggleGroupItems();
                if (toggleValue is not { Count: > 0 } || toggleValue.Count != toggleList.Count)
                {
                    toggleValue = new List<T>();
                    for (var i = 0; i < toggleList.Count; i++)
                        toggleValue.Add(default(T));
                }
            }

            public T GetValueByIndex(int index)
            {
                if (toggleValue is not { Count: > 0 } || index < 0 || index >= toggleValue.Count)
                    return default;
                return toggleValue[index];
            }

            protected override void ToggleRegister(Toggle t)
            {
                base.ToggleRegister(t);
                OnAnyToggleValueSwitchOn?.Invoke(t, GetValueByIndex(GetIndex(t)));
            }
        }


        /// <summary>
        /// toggle映射集
        /// <code>
        /// toggle映射集必须使用unity序列化管理，否则和直接使用toggleGroup没区别，目的是解决toggleGroup在打包后大纲视图的索引可能发生错位的问题。
        /// </code>
        /// </summary>
        [Serializable]
        public class ToggleMapping : IEnumerable<Toggle>, IHierarchyControl
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
            [LabelText("编辑单选项目顺序")]
#endif
            [SerializeField]
            protected List<Toggle> toggleList = new List<Toggle>();

            protected bool ToggleListInvalid => toggleList is not { Count: > 0 };

            public List<Toggle> ToggleList
            {
                get
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        return toggleList;
#endif
                    if (_mappingDirty || ToggleListInvalid)
                    {
                        BakeToggleGroupItems();
                        _mappingDirty = false;
                    }

                    if (_noInit)
                    {
                        Initialize();
                        _noInit = false;
                    }

                    return toggleList;
                }
            }


            /// <summary>
            /// 获取索引下的单选项组件
            /// </summary>
            /// <param name="index"></param>
            public Toggle this[int index] => ToggleList[index];

            public int Count => ToggleList.Count;

            /// <summary>
            /// 直接获取缓存选中索引
            /// </summary>
            public int CurrentSelectIndex => _nowSelectToggleIndex;

            /// <summary>
            /// 选中项目实例
            /// </summary>
            public Toggle CurrentSelectToggle => _nowSelectToggleIndex < 0 || _nowSelectToggleIndex > ToggleList.Count 
                ? null 
                : ToggleList[_nowSelectToggleIndex];

            /// <summary>
            /// 允许清空选项的选中状态
            /// </summary>
            public bool AllowSwitchOff
            {
                get => ToggleGroup.allowSwitchOff;
                set => ToggleGroup.allowSwitchOff = value;
            }

            // 映射关系脏
            private bool _mappingDirty = false;

            // 未初始化
            private bool _noInit = true;

            // 当前选中的项目
            private int _nowSelectToggleIndex = -1;

            // 在任意选项选中时复位允许反选toggle的功能
            private bool _resetAllowToggleOffAtAnyIsOn;

            #endregion

            #region 结构更新

            /// <summary>
            /// 烘焙单选项组
            /// </summary>
            /// <code>
            /// 注意不要再这里面使用ToggleList
            /// </code>
#if ODIN_INSPECTOR
            [HorizontalGroup("GetGroup"), Button("GetGroup")]
#endif
            public virtual void BakeToggleGroupItems()
            {
                if (ToggleGroup == null)
                {
                    if (ToggleListInvalid)
                    {
                        Debug.LogError("未指定单选项目组中的任何引用成员，无法初始化");
                        return;
                    }

                    var validToggle = toggleList.FirstOrDefault(a => a.group != null);
                    if (validToggle != null)
                    {
                        ToggleGroup = validToggle.group;
                        Debug.LogWarning("未指定单选项目组中的任何引用成员，但使用成员代偿");
                    }
                    else
                    {
                        Debug.LogError("未指定单选项目组中的任何引用成员，且无法利用成员代偿");
                        return;
                    }
                }

                List<Toggle> tempToggleList = null;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (!ToggleGroup.gameObject.activeInHierarchy)
                    {
                        var tempParents = ToggleGroup.transform.GetParents()
                            .Select(a => a.GetActivity()).ToList();
                        ToggleGroup.gameObject.SetActivityInHierarchy(true);
                        tempToggleList = ToggleGroup.GetToggles();
                        ToggleGroup.transform.GetParents()
                            .Zip(tempParents, (go, act) => (go, act))
                            .ForEachDo(a => a.go.SetActivity(a.act));
                        if (tempToggleList is not { Count: > 0 })
                            Debug.LogError("无法获取ToggleGroup中的成员，可能由于toggleGroup被隐藏导致其无法初始化");
                    }
                    else
                        tempToggleList = ToggleGroup.GetToggles();
                }
                else
#endif
                {
                    if (!ToggleGroup.gameObject.activeInHierarchy && ToggleListInvalid)
                        Debug.LogError(
                            $"当前运行状态导致无法直接获取ToggleGroup中的成员，且运行时我无法自行决定目标ToggleGroup（{ToggleGroup.name}）所属生命周期，请提前在编辑器中对相关状态进行烘焙");
                    else
                        tempToggleList = ToggleGroup.GetToggles();
                }

                // 需要注意的是，这里不能随意释放掉原来的选项列表
                if (tempToggleList is { Count: > 0 })
                    toggleList = tempToggleList;
            }
#if ODIN_INSPECTOR
            [HorizontalGroup("GetGroup"), Button("GetGroup(Sort)")]
#endif
            public void BakeSortToggelGroupItems()
            {
                BakeToggleGroupItems();
                if (ToggleListInvalid)
                    toggleList = MacroSort.FullCharacterOrderSort(toggleList, a => a.name)
                        .ToList();
            }
#if ODIN_INSPECTOR
            [HorizontalGroup("GetGroup"), Button("GetGroup(Reverse Sort)")]
#endif
            public void BakeReverseSortToggelGroupItems()
            {
                BakeToggleGroupItems();
                if (ToggleListInvalid)
                    toggleList = MacroSort.FullCharacterOrderSort(toggleList, a => a.name)
                        .Reverse()
                        .ToList();
            }
#if ODIN_INSPECTOR
            [HorizontalGroup("GetGroup"), Button("SaveGroup")] [DisableInEditorMode]
#endif
            [Obsolete("正常流程单选项组中的标签已经呈现了必要对象，无需重新设置")]
            public void SetToggelGroupItems()
            {
                var realToggleGroup = ToggleGroup.GetToggles();
                if (ToggleListInvalid || toggleList.Count != realToggleGroup.Count)
                {
                    Debug.LogError("编组无效，或编组成员与实际不符");
                    return;
                }
                Debug.Log("编组设置成功");
            }

            public void Initialize()
            {
                // 啥也没有，压根没用这部分功能，用不着初始化。
                if (ToggleGroup == null && toggleList.Count <= 0)
                    return;
                // 防呆
                if (toggleList.Count <= 0)
                {
                    var toggles = ToggleGroup.GetToggles();
                    if (toggles.Count != toggleList.Count)
                    {
                        toggleList = toggles;
                        Debug.LogWarning($"在初始化单选项组时，{ToggleGroup.name}并未预先指定索引顺序，将默认使用大纲顺序。");
                    }

                    if (toggles.Count <= 0)
                    {
                        Debug.LogWarning("在初始化单选项组时，目标单选项组为空");
                        return;
                    }
                }

                // 防空
                toggleList = toggleList.Where(a => a != null).ToList();

                // 防傻
                if (ToggleGroup == null)
                    ToggleGroup = toggleList.FirstOrDefault(a => a.group != null)?.group;
                if (ToggleGroup == null)
                {
                    ToggleGroup = toggleList[0].transform.parent.gameObject.AddComponent<ToggleGroup>();
                    foreach (var toggle in toggleList)
                        toggle.group = ToggleGroup;
                }

                // 事件初始化
                for (var i = 0; i < toggleList.Count; i++)
                {
                    var toggle = toggleList[i];

                    ToggleAddEvent(toggle);

                    if (_nowSelectToggleIndex < 0 && toggle.isOn)
                        _nowSelectToggleIndex = i;
                }

                // 如果不允许为空的情况下还为空，那就默认标记一个
                if (!ToggleGroup.allowSwitchOff && _nowSelectToggleIndex < 0)
                    SetToggleOnWithoutNotify(0);

                if (_noInit)
                    SetToggelGroupItems();

                _mappingDirty = false;
                _noInit = false;
            }

            #endregion

            #region 初始化 和 增删改查

            /// <summary>
            /// 添加一个toggle
            /// </summary>
            /// <param name="t"></param>
            /// <returns>返回toggle的索引</returns>
            public int AddToggle(Toggle t)
            {
                ToggleAddEvent(t);

                var resultIndex = ToggleList.Count;
                ToggleList.Add(t);
                return resultIndex;
            }

            /// <summary>
            /// 移除一个toggle，这不会影响其他toggle的索引，但此处移除的位置会为空。
            /// <code>
            /// 注意mapping管理的toggle在移除后会被清空事件
            /// 此举这不会销毁toggle。
            /// </code>
            /// </summary>
            /// <param name="t"></param>
            /// <returns>是否成功移除toggle</returns>
            public bool RemoveToggle(Toggle t)
            {
                var index = ToggleList.IndexOf(t);
                if (index < 0)
                    return false;

                t.onValueChanged.RemoveAllListeners();
                ToggleList[index] = null;
                return true;
            }

            /// <summary>
            /// 清除toggle
            /// </summary>
            /// <param name="allowDestroy">是否同时销毁所有toggle</param>
            public void CleanToggle(bool allowDestroy)
            {
                foreach (var t in ToggleList)
                {
                    t.onValueChanged.RemoveAllListeners();
                    if (allowDestroy)
                        Object.Destroy(t);
                }

                ToggleList.Clear();
            }

            #endregion

            #region 事件流程

            /// <summary>
            /// 强制标记映射关系脏，在下次运行时将自动按需更新
            /// </summary>
            public void SetDirty()
            {
                _mappingDirty = true;
            }


            /// <summary>
            /// 查找当前选中实例在列表中的索引位置
            /// </summary>
            public int CurrentSelectToggleIndex()
            {
                for (var i = 0; i < ToggleList.Count; i++)
                {
                    if (!ToggleList[i].isOn) continue;
                    _nowSelectToggleIndex = i;
                    return _nowSelectToggleIndex;
                }

                return -1;
            }


            /// <summary>
            /// toggle注册的事件，只有当按下时才需要调用此事件。
            /// </summary>
            /// <param name="t"></param>
            private void ToggleAddEvent(Toggle t)
            {
                t.onValueChanged.RemoveListener(Listener);
                t.onValueChanged.AddListener(Listener);
                return;

                void Listener(bool b)
                {
                    if (b) ToggleRegister(t);
                }
            }

            /// <summary>
            /// toggle注册的事件，只有当按下时才需要调用此事件。
            /// </summary>
            /// <param name="t"></param>
            protected virtual void ToggleRegister(Toggle t)
            {
                TryResetToggleGroupAutoOff();
                _nowSelectToggleIndex = GetIndex(t);
                OnAnyToggleSwitchOn?.Invoke(t);
                OnAnyToggleIndexSwitchOn?.Invoke(_nowSelectToggleIndex);
            }


            /// <summary>
            /// 获取toggle代表的索引
            /// </summary>
            /// <param name="target"></param>
            /// <returns>如果这个toggle不存在于当前的单选项组中，返回-1</returns>
            public int GetIndex(Toggle target)
            {
                if (target == null)
                {
                    Debug.LogError("无法查询空toggle的索引");
                    return 0;
                }

                var index = ToggleList.IndexOf(target);
                if (index >= 0)
                    return index;
                Debug.LogError($"无法查询 {target.name} 在当前单选项组中的索引。");
                return 0;
            }

            /// <summary>
            /// 获取toggle代表的索引， 如果toggle不存在于这个映射集中，将返回否
            /// </summary>
            /// <param name="target"></param>
            /// <param name="index"></param>
            /// <returns></returns>
            public bool TryGetIndex(Toggle target, out int index)
            {
                index = ToggleList.IndexOf(target);
                return index >= 0;
            }


            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="target"></param>
            public void SetToggleOn(Toggle target)
            {
                target.isOn = true;
                TryResetToggleGroupAutoOff();
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="index"></param>
            public void SetToggleOn(int index)
            {
                if (0 <= index && index < ToggleList.Count)
                {
                    SetToggleOn(ToggleList[index]);
                }
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="target"></param>
            public void SetToggleOnWithoutNotify(Toggle target)
            {
                _nowSelectToggleIndex = GetIndex(target);
                TryResetToggleGroupAutoOff();
                target.SetIsOnWithoutNotify(true);
            }

            /// <summary>
            /// 设置单选项激活
            /// </summary>
            /// <param name="index"></param>
            public void SetToggleOnWithoutNotify(int index)
            {
                if (0 < index && index < ToggleList.Count)
                {
                    SetToggleOnWithoutNotify(ToggleList[index]);
                }
            }

            
            /// <summary>
            /// 将整个单选项组复位，这同时会标记单选项组上的允许取消操作。
            /// 在下次选中标签时，会自动复位这个标记，确保全部反选的状态仅出现一次。
            /// </summary>
#if ODIN_INSPECTOR
            [Button("ResetAllToggleOff")]
#endif
            public void ResetGroupAllToggleOff()
            {
                _nowSelectToggleIndex = -1;
                if (ToggleGroup == null)
                {
                    Debug.LogError("单选项组不存在...");
                    if (!ToggleGroup.allowSwitchOff)
                    {
                        _resetAllowToggleOffAtAnyIsOn = true;
                        ToggleGroup.allowSwitchOff = true;
                    }
                }
                ToggleList.ForEachDo(a => a.SetIsOnWithoutNotify(false));
            }

            /// <summary>
            /// 如果此次按下是发生在全部反选之后的首次操作，那么关闭允许关闭的操作。
            /// </summary>
            private void TryResetToggleGroupAutoOff()
            {
                if (_resetAllowToggleOffAtAnyIsOn)
                {
                    _resetAllowToggleOffAtAnyIsOn = false;
                    ToggleGroup.allowSwitchOff = false;
                }
            }

            
            /// <summary>
            /// 清除映射结构(不会清除toggle实例)
            /// </summary>
            public void Clear()
            {
                ToggleGroup.RemoveToggleGroupChangeEvent();
                ToggleList.Clear();
            }

            /// <summary>
            /// 清除映射结构，并销毁所有toggle组件 
            /// </summary>
            public void DestroyAllToggle()
            {
                for (int i = ToggleList.Count - 1; i >= 0; i--)
                {
                    Object.Destroy(ToggleList[i]);
                }

                Clear();
            }


            public IEnumerator<Toggle> GetEnumerator()
            {
                return ToggleList.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region 额外方法

            public bool GetActive() => ToggleList.Any(a => a.gameObject.activeSelf);

            public bool GetActiveInHierarchy() => ToggleList.Any(a => a.gameObject.activeInHierarchy);

            public void SetActive(bool active)
            {
                ToggleGroup.gameObject.SetActivity(active);
                ToggleList.ForEachDo(a => a.gameObject.SetActive(active));
            }

            public void SetActiveInHierarchy(bool active)
            {
                ToggleGroup.gameObject.SetActivity(active);
                ToggleList.ForEachDo(a => a.gameObject.SetActivityInHierarchy(active));
            }

            #endregion

            #region 过时

            /// <summary>
            /// 清除映射结构，并销毁所有toggle组件 
            /// </summary>
            [Obsolete("方法命名不规范，改为使用DestroyAllToggle")]
            public void RemoveAllToggle()
                => DestroyAllToggle();

            /// <summary>
            /// 当前选中的toggle索引，依赖缓存，对于绕过该容器的单选项控制行为可能存在追踪不准确的问题。
            /// </summary>
            [Obsolete("命名过时")]
            public int NowSelectToggleIndex => CurrentSelectIndex;

            /// <summary>
            /// 当前选中的toggle
            /// </summary>
            [Obsolete("命名规范过时")]
            public Toggle NowSelectToggle => CurrentSelectToggle;

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