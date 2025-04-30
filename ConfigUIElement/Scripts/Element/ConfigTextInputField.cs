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
    /// 一种普通文本输入框
    /// </summary>
    public class ConfigTextInputField : ConfigSelectableItem
    {
        #region 属性字段
        
#if UNITY_EDITOR
        public string editorInput;
#endif
        
        public TMP_InputField targetInput;

        
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
            targetInput.onValueChanged.AddListener(o =>
            {
                if(double.TryParse(o, out var value))
                    SetValue(value);
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
