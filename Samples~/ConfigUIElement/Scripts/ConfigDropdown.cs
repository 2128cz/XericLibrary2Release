using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LRC
{
    public class ConfigDropdown : ConfigSelectableItem
    {
        #region 属性字段

        public TMP_Dropdown targetDropdown;

        #endregion

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            targetDropdown.onValueChanged.AddListener(index =>
            {
                SetValue(index);
            });
        }

        public void InitializationDropdown(IEnumerable<string> options)
        {
            targetDropdown.options = options
                .Select(option => new TMP_Dropdown.OptionData(option)).ToList();
            targetDropdown.RefreshShownValue();
            if (IsValueInitNull)
                RefreshValueWithoutEvent(0);
        }

        #endregion
        
        #region 实现

        protected override void SetValue(object newValue)
        {
            base.SetValue(CastIntNumber(newValue));
        }

        public override void RefreshValueWithoutEvent(object newValue, bool forceSet = false)
        {
            base.RefreshValueWithoutEvent(CastIntNumber(newValue), forceSet);
        }
        
        protected override void Initialization_ChildConstruction(UIBehaviour component)
        {
            base.Initialization_ChildConstruction(component);
            
            var name = component.transform.name;
            if (name is not "dropdown") return;
            if (component is TMP_Dropdown dropdown)
            {
                if (targetDropdown != null)
                    targetDropdown = dropdown;
            }
        }
        
        protected override void RefreshFormatValue(object newValue)
        {
            base.RefreshFormatValue(newValue);
            if (newValue == null)
                return;
            targetDropdown.value = CastIntNumber(newValue);
            targetDropdown.RefreshShownValue();  
        }
        
        #endregion
    }
}