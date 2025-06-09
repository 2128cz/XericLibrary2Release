using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XericLibrary.Runtime.CustomEditor;
using XericLibrary.Runtime.MacroLibrary;

namespace LRC
{
    /// <summary>
    /// 一种用于基础面板设置条目的类
    /// <code>
    /// 在此类中提供了双向的属性委托修改，并且拥有脏标记。
    /// 该类在初始化时以输入为优先。
    /// 脏标记仅当界面首次发生改变时进行记录，并不会影响值。
    /// </code>
    /// </summary>
    public abstract class ConfigSelectableItem : MonoBehaviour
    {
        #region 静态事件

        private static event Action<bool, bool> ForceUpdateAllConfigEvent;
        /// <summary>
        /// 强制所有激活的成员产生请求更新的事件（比如初始化时，外部更新时）
        /// </summary>
        /// <param name="resetDirty"></param>
        public static void ForceUpdateAll(bool resetDirty = false, bool forceSet = false)
        {
            ForceUpdateAllConfigEvent?.Invoke(resetDirty, forceSet);
        }

        private static event Action ForceSetAllSourceValueEvent;
        /// <summary>
        /// 强制所有激活的成员产生请求设置的事件（比如按下应用按钮时）
        /// </summary>
        public static void ForceSetAllSourceValue()
        {
            ForceSetAllSourceValueEvent?.Invoke();
        }

        /// <summary>
        /// 数值遭到修改时
        /// </summary>
        public static event Action<ConfigSelectableItem> OnAnyValueDirty;

        /// <summary>
        /// 当数值收到修改时，产生事件，其中值的含义是修改前的值
        /// </summary>
        public static event Action<ConfigSelectableItem, object> OnAnyValueChange; 
        
        #endregion
        
        #region 静态成员

        /// <summary>
        /// 所有有效的配置项目，当项目被隐藏（不处于激活状态时）就会从列表中删除。
        /// </summary>
        private static readonly List<ConfigSelectableItem> ConfigItemList = new List<ConfigSelectableItem>();

        /// <summary>
        /// 项目列表有效成员的数量
        /// </summary>
        public static int ConfigItemListCount => ConfigItemList.Count;
        
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static ConfigSelectableItem GetConfigItemByIndex(int index) => ConfigItemList[index];
        public static IEnumerable<ConfigSelectableItem> ConfigItemLists => ConfigItemList;
        
        private static bool autoUpdateValueAll;
        /// <summary>
        /// 自动更新所有数值
        /// </summary>
        public static bool AutoUpdateValueAll
        {
            get => autoUpdateValueAll;
            set
            {
                foreach (var item in ConfigItemList)
                    item.autoUpdateValue = value;
                autoUpdateValueAll = value;
            }
        }
        
        #endregion
        
        #region 状态

        /// <summary>
        /// 数值遭到修改时，标记脏，这样在后续的应用中有可以保存的变化
        /// <code>
        /// 建议：这是可选的
        /// 这会立刻发生在值修改时，且比OnValueChange更早，但在复位之前只会调用一次。
        /// </code>
        /// </summary>
        public event Action<ConfigSelectableItem> OnValueDirty;

        /// <summary>
        /// 当数值收到修改时，产生事件，其中值的含义是修改前的值
        /// <code>
        /// 建议：这是可选的
        /// 这会立刻发生在值修改时，所以如果需要进行页面刷新的话，建议进行等待
        /// </code>
        /// </summary>
        public event Action<ConfigSelectableItem, object> OnValueChange;

        /// <summary>
        /// 当数值结束修改时
        /// </summary>
        public event Action<ConfigSelectableItem, object> OnEndEdit;
        /// <summary>
        /// 当前属性请求获取属性值
        /// <code>
        /// 建议：这是必要的
        /// 当发生刷新事件时，将产生调用。
        /// 含义是将外部的值设定到此。
        /// </code>
        /// </summary>
        public event Func<ConfigSelectableItem, object> GetSourceValue;

        /// <summary>
        /// 当前属性请求设置属性值
        /// <code>
        /// 建议：这是必要的
        /// 当被应用时，比如调用了ForceSetAllSourceValue，将产生调用。
        /// 含义是将此处的界面值返回到属性中。
        /// 警告：不要在这里面调用ForceSetAllSourceValue，SetSourceValueRequest等方法，
        /// 这会造成死循环以及内存溢出等问题。
        /// </code>
        /// </summary>
        public event Action<ConfigSelectableItem, object> SetSourceValue;

        /// <summary>
        /// 当前属性的自动更新请求获取
        /// <code>
        /// 建议：这是条件可选的
        /// 当被管理的目标中存在Toggle时(对应autoUpdateValueToggle项目)
        /// 此处将用于从外部的值是否勾选值设定到此
        /// </code>
        /// </summary>
        public event Func<ConfigSelectableItem, bool> GetSourceValueAutoUpdate;
        
        /// <summary>
        /// 当前属性的自动更新请求设置
        /// <code>
        /// 建议：这是条件可选的
        /// 当被管理的目标中存在Toggle时(对应autoUpdateValueToggle项目)
        /// 此处将用于设置到外部的是否勾选值设定到此
        /// </code>
        /// </summary>
        public event Action<bool> SetSourceValueAutoUpdate;
        
        
        
        /// <summary>
        /// 请求此属性设置值(将值应用到寄存中)
        /// </summary>
        public void SetSourceValueRequest()
        {
            SetSourceValue?.Invoke(this, Value);
        }
        
        #endregion
        
        #region 属性字段

        /// <summary>
        /// 项目对应的标记tag
        /// </summary>
        [Rename("映射标记")]
        public string configTag;
        
        /// <summary>
        /// 设定是否有效。
        /// 如果无效，则不允许输入
        /// </summary>
        [SerializeField]
        [Rename("允许输入")]
        private bool allowInput = true;

        /// <summary>
        /// 自动更新数值
        /// </summary>
        [SerializeField]
        [Rename("主动更新")]
        private bool autoUpdateValue = true;

        /// <summary>
        /// 允许阻塞自动更新，表示此项目可以脱离自动更新状态选框的状态运行
        /// </summary>
        [SerializeField]
        [Rename("允许阻塞自动更新")]
        private bool allowBlockUpdate = true;
        
        /// <summary>
        /// 阻塞自动更新，一般由程序自动根据焦点更新
        /// </summary>
        [SerializeField]
        [Rename("阻塞自动更新")]
        private bool forceBlockUpdate = false;
        
        /// <summary>
        /// 格式化文本
        /// </summary>
        public string numberFormat = "0.##";

        /// <summary>
        /// 项目标题组件
        /// </summary>
        public TextMeshProUGUI titleLabel;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private string titleTextContext;

        /// <summary>
        /// 帮助文本组件
        /// </summary>
        public TextMeshProUGUI helpLabel;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private string helpTextContext;

        /// <summary>
        /// 单位文本组件
        /// </summary>
        public TextMeshProUGUI unitLabel;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private string unitTextContext;

        /// <summary>
        /// 条目前的选框，与autoUpdateValue对应
        /// </summary>
        [Rename("自动更新状态选框")]
        public Toggle autoUpdateValueToggle;

        /// <summary>
        /// 反转选框状态
        /// </summary>
        [Rename("反转自动更新按钮状态")]
        public bool autoUpdateValueToggleReversalState;

        [Rename("反转自动更新状态")]
        public bool autoUpdateValueReversalState;
        
        /// <summary>
        /// 脏属性自动复位。
        /// 当属性被重新开关时，将取消脏状态
        /// </summary>
        [Rename("脏标记自动复位")]
        public bool dirtyAutoReset = false;

        // ==== 属性 ==== //
        
        /// <summary>
        /// 设定是否输入有效
        /// </summary>
        public virtual bool AllowInput
        {
            get => allowInput;
            set => allowInput = value;
        }

        /// <summary>
        /// 此属性项目已经初始化
        /// </summary>
        [HideInInspector]
        public bool IsValueInitNull { get; private set; } = true;

        // [Obsolete("请使用访问器")]
        private bool _isDirty;
        /// <summary>
        /// 脏属性标记，首次脏会进行广播
        /// </summary>
        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty)
                {
                    if (!value)
                        _isDirty = false;
                    return;
                }
                if (!value) return;
                
                _isDirty = true;
                OnAnyValueDirty?.Invoke(this);
                OnValueDirty?.Invoke(this);
            }
        }
        
        /// <summary>
        /// 设定自动更新状态，并同步选框
        /// <code>
        /// 为了书写方便，保存的状态是和外部暂存值一致，
        /// 此访问器将作为本地状态的标准访问。
        ///
        /// 访问将自动进行转换，而设置则不进行转换，
        /// 所以应当使用此访问器进行设置或内部访问，而与外部通讯应当使用字段。
        /// 
        /// 如果需要将此内部判断状态转为界面状态：
        /// 应当先通过autoUpdateValueReversalState转为暂存值，
        /// 然后再通过autoUpdateValueToggleReversalState转为界面值。
        /// </code>
        /// </summary>
        public bool AutoUpdateValue
        {
            get => autoUpdateValueReversalState ^ autoUpdateValue;
            set
            {
                autoUpdateValue = value;
                if (autoUpdateValueToggle != null)
                    autoUpdateValueToggle.isOn = autoUpdateValueToggleReversalState ^ value;
            }
        }

        /// <summary>
        /// 设定阻塞自动更新
        /// </summary>
        public bool ForceBlockUpdate
        {
            get => forceBlockUpdate;
            set => forceBlockUpdate = value;    
        }
        
        /// <summary>
        /// 项目标题
        /// </summary>
        public string TitleName
        {
            get => titleLabel.text;
            set => titleLabel.text = value;
        }

        /// <summary>
        /// 帮助文本，再默认情况下应该是不显示的通过赋予空文本来关闭显示
        /// </summary>
        public string HelpName
        {
            get
            {
                if (helpLabel != null) return helpLabel.text;
                return null;
            }
            set
            {
                if (helpLabel != null)
                {
                    if (value != null)
                    {
                        helpLabel.text = value;
                        helpLabel.gameObject.SetActive(true);
                    }
                    else
                        helpLabel.gameObject.SetActive(false);
                }
            }
        }

        public string UnitName
        {
            get => unitLabel.text;
            set => unitLabel.text = value;
        }
        
        [SerializeField]
        // [Obsolete("请使用访问器")]
        private object _value;

        /// <summary>
        /// 项目值
        /// </summary>
        public object Value
        {
            get => _value; 
            protected set
            {
                if (IsValueInitNull && value != null)
                    IsValueInitNull = false;
                _value = value;
            }
        }

        // ==== 暂存 ==== //
        
        public FieldInfo FieldInfo;

        public Type FieldType;
        
        #endregion

        #region 生命周期

        protected virtual void OnValidate()
        {
            if (titleLabel != null)
                TitleName = titleTextContext;
            if (helpLabel != null)
                HelpName = helpTextContext;
            if (unitLabel != null)
                UnitName = unitTextContext;
        }

        protected virtual void Awake()
        {
            foreach (var child in transform.GetChildren())
            {
                var component = child.GetComponent<UIBehaviour>();
                if (component != null)
                    Initialization_ChildConstruction(component);
            }

            Initialization_EventBinding();
        }

        protected virtual void OnEnable()
        {
            ConfigItemList.Add(this);
            ForceUpdateAllConfigEvent += ForceUpdate;
            ForceSetAllSourceValueEvent += SetSourceValueRequest;
            
            if (dirtyAutoReset)
                IsDirty = false;
        }

        protected virtual void OnDisable()
        {
            ConfigItemList.Remove(this);
            ForceUpdateAllConfigEvent -= ForceUpdate;
            ForceSetAllSourceValueEvent -= SetSourceValueRequest;
        }

        protected virtual void Start()
        {
            ForceUpdate(true);
        }

        private void Update()
        {
            if (IsValueInitNull || (AutoUpdateValue && !(allowBlockUpdate && forceBlockUpdate)))
            {
                ForceUpdate();
            }
        }

        private void OnDestroy()
        {
            ForceUpdateAllConfigEvent -= ForceUpdate;
        }

        #endregion

        #region 换算与转换

        protected static double CastDigitalNumber(object newValue)
        {
            if (double.TryParse(newValue.ToString(), out var value))
            {
                return value;
            }
            return -1;
        }

        /// <summary>
        /// 转换到数值类型（数值钳制与四舍五入）
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        protected static double CastDigitalNumber(object newValue, double minValue, double maxValue, int digits)
        {
            // var resultValue = newValue switch
            // {
            //     bool boolValue => boolValue ? 1 : 0,
            //     short intValue => intValue,
            //     int intValue => intValue,
            //     long intValue => intValue,
            //     float floatValue => Math.Round(floatValue, digits),
            //     double doubleValue => Math.Round(doubleValue, digits),
            //     _ => 0
            // };
#if UNITY_EDITOR
            if (newValue == null)
            {
                Debug.LogError($"参数转换错误:目标是一个空值，请在发生空值时提前退出");
                return -1;
            }
#endif
            if (double.TryParse(newValue.ToString(), out var value))
            {
                var resultValue = Math.Round(value, digits);
                return Math.Clamp(resultValue, minValue, maxValue);
            }
            Debug.LogError($"参数转换错误:{newValue}可能不是一个有效的数值");
            return -1;
        }

        protected static int CastIntNumber(object newValue)
        {
            var resultValue = Convert.ToInt32(newValue);
            return resultValue;
        }

        protected static Vector2 CastVector2(object newValue)
        {
            return newValue switch
            {
                Vector2 vec2Value => vec2Value,
                System.Numerics.Vector2 sysVec2Value => new Vector2(sysVec2Value.X, sysVec2Value.Y),
                _ => Vector2.zero
            };
        }

        protected static Vector3 CastVector3(object newValue)
        {
            return newValue switch
            {
                Vector3 vec3Value => vec3Value,
                System.Numerics.Vector3 sysVec3Value => new Vector3(sysVec3Value.X, sysVec3Value.Y, sysVec3Value.Z),
                _ => Vector3.zero
            };
        }
        
        #endregion
        
        #region 初始化方法

        /// <summary>
        /// 可选的基本初始化
        /// </summary>
        /// <param name="autoUpdateValue">设定自动更新</param>
        protected void Initialization(bool autoUpdateValue = true)
        {
            AutoUpdateValue = autoUpdateValue;
        }

        private bool _doOnce = false;
        /// <summary>
        /// 构建属性，在初始化过程中，通过识别项目来构建域
        /// </summary>
        protected virtual void Initialization_ChildConstruction(UIBehaviour component)
        {
            // 跟踪节点的名称
            var name = component.gameObject.name;
            // 初始化自动更新对勾
            if (name == "toggle" && component is Toggle toggle)
            {
                if (autoUpdateValueToggle == null)
                    autoUpdateValueToggle = toggle;
                return;
            }
            if (name is not ("title" or "name")) return;
            if (_doOnce)
            {
                Debug.LogError("存在重复命名的属性模块：" + name);
                return;
            }
            if (component is TextMeshProUGUI textMeshProUGUI)
            {
                _doOnce = true;
                if (titleLabel != null)
                    titleLabel = textMeshProUGUI;
            }
        }

        /// <summary>
        /// 构建事件，在初始化结束前，需要对所有动态绑定的按键进行事件绑定
        /// </summary>
        protected virtual void Initialization_EventBinding()
        {
            // 无论如何，事件需要被注册
            if (autoUpdateValueToggle != null)
            {
                autoUpdateValueToggle.onValueChanged.AddListener(a =>
                {
                    autoUpdateValue = autoUpdateValueToggleReversalState ^ a;
                    // 访问器将会自动转换，所以使用字段
                    SetSourceValueAutoUpdate?.Invoke(autoUpdateValue);
                    // 可能不会脏
                    // OnValueDirty?.Invoke(this);
                });
            }
        }

        #endregion

        #region 更新方法
        
        /// <summary>
        /// 更新函数
        /// <code>
        /// 在其他任意时刻调用时，将强制刷新其中的功能，如果已经是脏属性则不会有任何行为。
        /// 更新是指从本地数据中更新，这会立刻产生值获取回调。
        /// </code>
        /// </summary>
        public virtual void ForceUpdate(bool resetDirty = false, bool forceSet = false)
        {
            if (resetDirty)
                IsDirty = false;
            var value = GetSourceValue?.Invoke(this);
            if (value == null)
                return ;
            RefreshValueWithoutEvent(value, forceSet);
            RefreshAutoUpdateValue();
        }

        /// <summary>
        /// 设置值，并触发更改事件，通常由界面发起所以不会主动更新界面
        /// </summary>
        /// <param name="newValue"></param>
        protected virtual void SetValue(object newValue)
        {
            if (Value == newValue)
                return;
            var lastValue = Value;
            // 如果不允许输入，将复位为现在的值
            if (!allowInput)
                RefreshValueWithoutEvent(Value);
            else
            {
                Value = newValue;
                IsDirty = true;

                OnAnyValueChange?.Invoke(this, lastValue);
                OnValueChange?.Invoke(this, lastValue);
            }
        }

        /// <summary>
        /// 设置值，不触发更改时的事件，但应该进行界面数值更新。
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="forceSet"></param>
        public virtual void RefreshValueWithoutEvent(object newValue, bool forceSet = false)
        {
            if (!AutoUpdateValue && !forceSet)
                return;
            if (newValue == null)
                return;
            if (Value != null && (Value.ToString() == newValue.ToString()))
                return;
            Value = newValue;
            RefreshFormatValue(newValue);
        }

        /// <summary>
        /// 更新自动更新状态
        /// </summary>
        protected void RefreshAutoUpdateValue()
        {
            var isSourceValueAutoUpdate = GetSourceValueAutoUpdate?.Invoke(this);
            if (isSourceValueAutoUpdate != null)
                AutoUpdateValue = isSourceValueAutoUpdate.Value; 
        }
        
        /// <summary>
        /// 刷新格式化文本，在触发刷新时将产生更新 
        /// </summary>
        protected virtual void RefreshFormatValue(object newValue)
        {
            
        }

        protected void WhenStartEdit()
        {
            // Debug.Log("开始编辑");
            ForceBlockUpdate = true;
        }
        
        /// <summary>
        /// 数值输入完毕，或焦点移除时调用。
        /// </summary>
        protected void WhenEndEdit()
        {
            // Debug.Log("结束编辑");
            ForceBlockUpdate = false;
            OnEndEdit?.Invoke(this, Value);
        }
        
        #endregion

        #region 外观设定

        /// <summary>
        /// 销毁自动更新复选框，并设定当前是否允许自动更新
        /// </summary>
        /// <param name="autoUpdateValue"></param>
        public void DestroyAutoUpdateValue(bool autoUpdateValue)
        {
            Destroy(autoUpdateValueToggle.gameObject);
            this.autoUpdateValue = autoUpdateValueReversalState ^ autoUpdateValue;
        }
        
        #endregion

    }
}