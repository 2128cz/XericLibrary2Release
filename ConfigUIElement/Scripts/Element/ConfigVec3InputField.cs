using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LRC
{

    public class ConfigVec3InputField : ConfigSelectableItem
    {
        #region 属性字段
        
#if UNITY_EDITOR
        public Vector3 editorInput;
#endif

        public TMP_InputField targetInputX;
        public TMP_InputField targetInputY;
        public TMP_InputField targetInputZ;
        
        public override bool AllowInput
        {
            get => base.AllowInput;
            set
            {
                if (targetInputX != null)
                    targetInputX.readOnly = !value;
                if (targetInputY != null)
                    targetInputY.readOnly = value;
                if (targetInputZ != null)
                    targetInputZ.readOnly = value;
                base.AllowInput = value;
            }
        }

        #endregion

        #region 生命周期
        
        protected override void OnValidate()
        {
            base.OnValidate();
#if UNITY_EDITOR
            if (targetInputX != null)
                targetInputX.text = editorInput.x.ToString();
            if (targetInputY != null)
                targetInputY.text = editorInput.y.ToString();
            if (targetInputZ != null)
                targetInputZ.text = editorInput.z.ToString();
#endif
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            targetInputX.onSelect.AddListener(a =>
            {
                WhenStartEdit();
            });
            targetInputY.onSelect.AddListener(a =>
            {
                WhenStartEdit();
            });
            targetInputZ.onSelect.AddListener(a =>
            {
                WhenStartEdit();
            });
            targetInputX.onValueChanged.AddListener(a =>
            {
                var value = CastVector3(Value);
                value.x = float.Parse(a);
                SetValue(value);
            });
            targetInputY.onValueChanged.AddListener(a =>
            {
                var value = CastVector3(Value);
                value.y = float.Parse(a);
                SetValue(value);
            });
            targetInputZ.onValueChanged.AddListener(a =>
            {
                var value = CastVector3(Value);
                value.z = float.Parse(a);
                SetValue(value);
            });
            targetInputX.onEndEdit.AddListener(delegate
            {
                WhenEndEdit();
            });
            targetInputY.onEndEdit.AddListener(delegate
            {
                WhenEndEdit();
            });
            targetInputZ.onEndEdit.AddListener(delegate
            {
                WhenEndEdit();
            });
        }

        #endregion

        #region 实现

        protected override void SetValue(object newValue)
        {
            base.SetValue(CastVector3(newValue));
        }

        public override void RefreshValueWithoutEvent(object newValue, bool forceSet = false)
        {
            base.RefreshValueWithoutEvent(CastVector3(newValue), forceSet);
        }
        
        protected override void Initialization_ChildConstruction(UIBehaviour component)
        {
            base.Initialization_ChildConstruction(component);
            
            var name = component.transform.name;
            if (component is not TMP_InputField inputField) 
                return;
            switch (name)
            {
                case "inputx":
                case "inputX":
                    if (targetInputX != null)
                        targetInputX = inputField;
                    break;
                case "inputy":
                case "inputY":
                    if (targetInputY != null)
                        targetInputY = inputField;
                    break;
                case "inputz":
                case "inputZ":
                    if (targetInputZ != null)
                        targetInputZ = inputField;
                    break;
                default:
                    break;
            }
        }

        protected override void RefreshFormatValue(object newValue)
        {
            base.RefreshFormatValue(newValue);
            var value = CastVector3(newValue);
            targetInputX.text = value.x.ToString(numberFormat);
            targetInputY.text = value.y.ToString(numberFormat);
            targetInputZ.text = value.z.ToString(numberFormat);
        }

        #endregion
    }
}