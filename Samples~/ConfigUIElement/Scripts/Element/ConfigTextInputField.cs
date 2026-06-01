using TMPro;
using UnityEngine.EventSystems;

namespace LRC
{
    /// <summary>
    /// 一种普通文本输入框
    /// </summary>
    public class ConfigTextInputField : ConfigSelectableItem
    {
        #region 属性字段

#if UNITY_EDITOR
        public string editorInput;
#endif

        public TMP_InputField targetInput;


        // 定义一个委托，接受字符串参数并返回布尔值
        public delegate bool StringEventHandler(string input);
        // 声明事件
        public static event StringEventHandler 判断是否重名;

        public override bool AllowInput
        {
            get => base.AllowInput;
            set
            {
                if (targetInput != null)
                    targetInput.readOnly = !value;
                base.AllowInput = value;
            }
        }

        #endregion

        #region 生命周期

        protected override void OnValidate()
        {
            base.OnValidate();
#if UNITY_EDITOR
            if (targetInput != null)
                targetInput.text = editorInput;
#endif
        }

        protected override void Awake()
        {
            base.Awake();
            targetInput.onSelect.AddListener(a =>
            {
                WhenStartEdit();
            });
            targetInput.onEndEdit.AddListener(o =>
            {
                if (判断是否重名 != null)
                {
                    if (判断是否重名.Invoke(o))
                    {
                        SetValue(o);
                    }
                }
            });
            targetInput.onEndEdit.AddListener(delegate
            {
                WhenEndEdit();
            });

        }

        #endregion

        #region 实现

        protected override void SetValue(object newValue)
        {
            base.SetValue(newValue);
        }

        public override void RefreshValueWithoutEvent(object newValue, bool forceSet = false)
        {
            base.RefreshValueWithoutEvent(newValue, forceSet);
        }

        protected override void Initialization_ChildConstruction(UIBehaviour component)
        {
            base.Initialization_ChildConstruction(component);

            if (component is TMP_InputField inputField)
            {
                if (targetInput != null)
                    targetInput = inputField;
            }
        }

        protected override void RefreshFormatValue(object newValue)
        {
            base.RefreshFormatValue(newValue);
            targetInput.text = newValue.ToString();
        }

        #endregion
    }
}
