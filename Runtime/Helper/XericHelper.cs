using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.XericComponent;

namespace Deconstruction.Runtime
{
    /// <summary>
    /// 程序运行辅助
    /// </summary>
    public static class XericHelper
    {
        private static object _pusher = new object();
        private static bool _isMinimized = false;
        private static bool _isPrased = false;

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

        /// <summary>
        /// 最小化窗口
        /// </summary>
        public static void MinimizedWindow()
        {
            _isMinimized = true;
            MacroForm.MinimizeWindow();
        }
        
        /// <summary>
        /// 从最小化恢复窗口
        /// </summary>
        /// <returns></returns>
        public static bool RestoreWindow()
        {
            if (!_isMinimized) return false;
            _isMinimized = false;
            MacroForm.RestoreWindow();
            return true;
        }

        /// <summary>
        /// 全屏
        /// </summary>
        public static void FullScreen()
        {
            if (RestoreWindow())
                XericLifeCycleCore.SubmitTask(_pusher, 
                    f => Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen, 
                    phase: LifecyclePhase.Coroutine);
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }

        /// <summary>
        /// 全屏无边框窗口
        /// </summary>
        public static void FullScreenWindow()
        {
            if (RestoreWindow())
                XericLifeCycleCore.SubmitTask(_pusher, 
                    f => Screen.fullScreenMode = FullScreenMode.FullScreenWindow, 
                    phase: LifecyclePhase.Coroutine);
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        /// <summary>
        /// 最大化窗口
        /// </summary>
        public static void MaximizedWindow()
        {
            if (RestoreWindow())
                XericLifeCycleCore.SubmitTask(_pusher, 
                    f => Screen.fullScreenMode = FullScreenMode.MaximizedWindow, 
                    phase: LifecyclePhase.Coroutine);
            Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
        }

        /// <summary>
        /// 当前是否最小化
        /// </summary>
        /// <returns></returns>
        public static bool IsMinimized()
        {
            // return UnityEditor.EditorApplication.isPaused;
            return _isMinimized;
        }
    }
}