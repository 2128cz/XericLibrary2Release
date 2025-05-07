using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.MacroLibrary;

namespace DigitalTwinTool
{
    public static class MacroLayout
    {
        /// <summary>
        /// 将布局组标记为脏，以触发重算。
        /// </summary>
        /// <param name="layoutGroup"></param>
        public static void SetDirty(this LayoutGroup layoutGroup)
        {
            var type = typeof(LayoutGroup);
            var method = type.GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(layoutGroup, null);
        }

        private static HashSet<RectTransform> _rebuildSet = new HashSet<RectTransform>();
        public static void WaitParentLayoutRebuild(this MonoBehaviour script)
        {
            script.StartCoroutine(waitForRebuild());
            
            return;
            IEnumerator waitForRebuild()
            {
                yield return null;
                LayoutRebuilder.MarkLayoutForRebuild(script.RectTransform());
            }
        }
    }
}