using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LRC
{
    /// <summary>
    /// 一种数值增减输入框，将使用浮点值作为默认
    /// </summary>
    public class ConfigDigitalInputField : ConfigSelectableItem
    {
        #region 属性字段
        
#if UNITY_EDITOR
        public float editorInput;
#endif

        public TMP_InputField targetInput;

        public Button digitalAddButton;
        public Button digitalSubButton;

        public double maxValue = 114514;
        public double minValue = -114515;
        public int digits = 6;

        /// <summary>
        /// 增量
        /// </summary>
        public double delta = 1;
        
        public override bool AllowInput
        {
            get => base.AllowInput;
            set
            {
                if (targetInput != null)
                    targetInput.readOnly = !value;
                if (digitalAddButton != null)
                    digitalAddButton.interactable = value;
                if (digitalSubButton != null)
                    digitalSubButton.interactable = value;
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
                targetInput.text = editorInput.ToString();
#endif
        }
        
        protected override void Awake()
        {
            base.Awake();
            if (ReferenceEquals(targetInput, null))
            {
                Debug.LogError($"{name}配置项目条目的基本元素不存在");
                return;
            }
            targetInput.onSelect.AddListener(a =>
            {
                WhenStartEdit();
            });
            targetInput.onValueChanged.AddListener(o =>
            {
                double value = 0;
                if (double.TryParse(o, out value))
                {
                    SetValue(value);
                }
            });
            targetInput.onEndEdit.AddListener(delegate
            {
                WhenEndEdit();
            }); 
            if (!ReferenceEquals(digitalAddButton, null))
                digitalAddButton.onClick.AddListener(delegate
            {
                SetValue(CastDigitalNumber(Value) + delta);
                RefreshFormatValue(Value);
            });
            if (!ReferenceEquals(digitalSubButton, null))
                digitalSubButton.onClick.AddListener(delegate
            {
                SetValue(CastDigitalNumber(Value) - delta);
                RefreshFormatValue(Value);
            });
        }

        #endregion
        
        #region 实现

        protected override void SetValue(object newValue)
        {
            base.SetValue(CastDigitalNumber(newValue, minValue, maxValue, digits));
        }

        public override void RefreshValueWithoutEvent(object newValue, bool forceSet = false)
        {
            base.RefreshValueWithoutEvent(CastDigitalNumber(newValue, minValue, maxValue, digits), forceSet);
        }
        
        protected override void Initialization_ChildConstruction(UIBehaviour component)
        {
            base.Initialization_ChildConstruction(component);
            
            
            if (component is TMP_InputField inputField)
            {
                if (targetInput != null)
                    targetInput = inputField;
            }
            else if(component is Button button)
            {
                var name = component.transform.name;
                switch (name)
                {
                    case "subbutton":
                    case "SubButton":
                        if (digitalSubButton != null)
                            digitalSubButton = button;
                        break;
                    case "addbutton":
                    case "AddButton":
                        if (digitalAddButton != null)
                            digitalAddButton = button;
                        break;
                }
            }
        }

        protected override void RefreshFormatValue(object newValue)
        {
            base.RefreshFormatValue(newValue);
            targetInput.text = CastDigitalNumber(newValue).ToString(numberFormat);
        }
        
        #endregion
    }
}
