using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LRC
{
    public class ConfigButton : ConfigSelectableItem
    {
        #region 属性字段

        public Button targetButton;

        public override bool AllowInput
        {
            get => base.AllowInput;
            set
            {
                if (targetButton != null)
                    targetButton.interactable = !value;
                base.AllowInput = value;
            }
        }

        #endregion

        #region 实现

        protected override void SetValue(object newValue)
        {
            throw new System.NotImplementedException();
        }

        public override void RefreshValueWithoutEvent(object newValue, bool forceSet = false)
        {
            base.RefreshValueWithoutEvent(newValue, forceSet);
        }
        
        private bool _doOnce = false;
        protected override void Initialization_ChildConstruction(UIBehaviour component)
        {
            base.Initialization_ChildConstruction(component);
            
            var name = component.transform.name;
            if (name is not "button") return;
            if (_doOnce)
            {
                Debug.LogError("存在重复命名的属性模块：" + name);
                return;
            }
            if (component is Button button)
            {
                _doOnce = true;
                if (targetButton != null)
                    targetButton = button;
            }
        }

        #endregion
    }
}