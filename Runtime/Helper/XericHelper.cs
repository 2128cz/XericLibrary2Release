using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deconstruction.Runtime
{
    /// <summary>
    /// Xeric 库的运行时辅助类库
    /// </summary>
    public static class XericHelper
    {
        #region 程序运行

        /// <summary>
        /// 离开出游戏模式(在编辑模式时仅退出)
        /// </summary>
        public static void ExitGameMode()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.EditorApplication.ExitPlaymode();
            }
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
