using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using XericLibrary.Runtime.MacroLibrary;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XericLibrary.Runtime.Type.Base
{
    /// <summary>
    /// 游戏对象与软引用 (gameobject with soft refrence)。
    /// 将游戏对象引用保存为软引用，这对于经常替换模型的场景来说比较有用。
    /// </summary>
    [Serializable]
    public class GowSoftRef
    {
        public readonly string interval = "/";

        /// <summary>
        /// 对象
        /// </summary>
        [HorizontalGroup("SoftRef")]
        [HideLabel]
        public GameObject targetObejct;

        /// <summary>
        /// 路径
        /// </summary>
        [HorizontalGroup("SoftRef")]
        [HideLabel]
        public string targetPath;

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <returns></returns>
        public GameObject GetObject()
        {
            if (targetObejct == null)
                UpdateGameobjectByPath();
            return targetObejct;
        }

        /// <summary>
        /// 将对象制作成路径
        /// </summary>
        public void UpdatePathByGameobject()
        {
            if (targetObejct == null)
            {
                Debug.LogError("对象引用为空，无法获取软引用");
                return;
            }

            if (IsPrefab(targetObejct))
            {
                targetPath = "I Dont Know"; // todo 获取预制体路径
                return;
            }

            if (targetObejct.transform == null)
            {
                Debug.LogError("对象引用为空，无法获取软引用");
                return;
            }

            targetPath = targetObejct.transform.GetFamilyName().Join(a => a, interval: interval);
        }

        /// <summary>
        /// 将路径制作成对象
        /// </summary>
        /// <returns></returns>
        public bool UpdateGameobjectByPath()
        {
            if (MacroObject.FindFamilyMemberExact(null, targetPath.Split(interval), out var target))
            {
                targetObejct = target.gameObject;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 当前的对象是预制体吗
        /// </summary>
        public bool IsPrefab(Object target)
#if UNITY_EDITOR
        {
#if UNITY_6000_0_OR_NEWER
            var type = PrefabUtility.GetPrefabAssetType(target);
            return type is PrefabAssetType.Regular or PrefabAssetType.Variant;
#else
            return PrefabUtility.GetPrefabType(target) == PrefabType.PrefabInstance;
#endif
        }
#else
			=> false;
#endif
    }
}