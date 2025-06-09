using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LRC
{
    /// <summary>
    /// 单选项控制组，将返回一个代表当前有效单选项索引作为值。
    /// </summary>
    public class ConfigToggleGroup : ConfigSelectableItem
    {
        #region 属性字段

        public ToggleGroup targetToggle;

        public List<Toggle> toggles;        
        
        #endregion

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();

            for (int i = 0; i < toggles.Count; i++)
            {
                void BuildInGroupInit()
                {
                    var index = i;
                    var toggle = toggles[i];
                    toggle.onValueChanged.AddListener(a =>
                    {
                        if (a)
                            SetValue(index);
                    });
                }
                BuildInGroupInit();
            } 
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
            
            if (component is Toggle toggle &&
                !toggles.Contains(toggle))
                toggles.Add(toggle);
        }
        
        protected override void RefreshFormatValue(object newValue)
        {
            base.RefreshFormatValue(newValue);
            var newIndex = CastIntNumber(newValue);
            // var index = 0;
            // foreach (var toggle in targetToggle.ActiveToggles())
            // {
            //     if (newIndex != index++) 
            //         continue;
            //     toggle.isOn = true;
            //     Debug.Log(TitleName +":设置布尔项目");
            // }
            toggles[newIndex].SetIsOnWithoutNotify(true);
        }
        
        #endregion
    }
}