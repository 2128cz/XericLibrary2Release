using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using XericLibrary.Runtime.MacroLibrary;

namespace Deconstruction.Runtime
{
    public static class MacroLayout
    {
        /// <summary>
        /// 将布局组标记为脏，以触发重算布局。
        /// </summary>
        /// <param name="layoutGroup"></param>
        public static void SetLayoutDirty(this LayoutGroup layoutGroup)
        {
            // var type = typeof(LayoutGroup);
            // var method = type.GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.NonPublic);
            // method?.Invoke(layoutGroup, null);
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.RectTransform());
        }

        /// <summary>
        /// 将canvas组件白哦及为脏，以触发canvas充算布局
        /// </summary>
        /// <param name="rectTrans"></param>
        public static void SetLayoutDirty(this RectTransform rectTrans)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTrans);
        }
        
        private static HashSet<RectTransform> _rebuildSet = new HashSet<RectTransform>();
        /// <summary>
        /// 等待这帧过去之后再触发重建
        /// </summary>
        /// <param name="script"></param>
        public static void WaitParentLayoutRebuildThenRebuildAgen(this MonoBehaviour script)
        {
            if (!script.RectTransform())
                return;
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