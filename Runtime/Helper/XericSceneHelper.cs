using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deconstruction.Runtime
{
    public static class XericSceneHelper
    {
        public delegate void SceneOperationAsyncDelegate(TimeSpan elapsedTime, float process, bool isDone);
        
        /// <summary>
        /// 获取索引对应的场景名称
        /// </summary>
        /// <param name="sceneBuildIndex"></param>
        /// <returns></returns>
        public static string GetSceneBuildIndexName(int sceneBuildIndex)
        {
            return IsSceneBuildIndexValid(sceneBuildIndex) 
                ? SceneManager.GetSceneByBuildIndex(sceneBuildIndex).name 
                : string.Empty;
        }

        /// <summary>
        /// 协程加载场景
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadProcess"></param>
        /// <returns></returns>
        public static IEnumerator LoadScene(string sceneName, SceneOperationAsyncDelegate loadProcess = null)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("加载场景的名称为空或无效，请检查后重试");
                yield break;
            }
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return AsyncOperation(asyncLoad, loadProcess);
        }
        /// <summary>
        /// 协程加载场景
        /// </summary>
        /// <param name="sceneBuildIndex"></param>
        /// <param name="loadProcess"></param>
        /// <returns></returns>
        public static IEnumerator LoadScene(int sceneBuildIndex, SceneOperationAsyncDelegate loadProcess = null)
        {
            if (IsSceneBuildIndexValid(sceneBuildIndex))
            {
                Debug.LogError($"加载场景的索引（{sceneBuildIndex}）无效，请检查后重试");
                yield break;
            }
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
            yield return AsyncOperation(asyncLoad, loadProcess);
        }


        /// <summary>
        /// 协程卸载场景
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadProcess"></param>
        /// <param name="froceUnloadUnusedAssets"></param>
        /// <returns></returns>
        public static IEnumerator UnloadSceneAsync(string sceneName, SceneOperationAsyncDelegate loadProcess = null, bool froceUnloadUnusedAssets = false)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("加载场景的名称为空或无效，请检查后重试");
                yield break;
            }
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
            yield return AsyncOperation(asyncUnload, loadProcess);
            if (froceUnloadUnusedAssets)
                UnloadUnusedAssets();
        }
        /// <summary>
        /// 协程卸载场景
        /// </summary>
        /// <param name="sceneBuildIndex"></param>
        /// <param name="loadProcess"></param>
        /// <param name="froceUnloadUnusedAssets"></param>
        /// <returns></returns>
        public static IEnumerator UnloadSceneAsync(int sceneBuildIndex, SceneOperationAsyncDelegate loadProcess = null, bool froceUnloadUnusedAssets = false)
        {
            if (IsSceneBuildIndexValid(sceneBuildIndex))
            {
                Debug.LogError($"加载场景的索引（{sceneBuildIndex}）无效，请检查后重试");
                yield break;
            }
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneBuildIndex);
            yield return AsyncOperation(asyncUnload, loadProcess);
            if (froceUnloadUnusedAssets)
                UnloadUnusedAssets();
        }

        private static IEnumerator AsyncOperation(AsyncOperation asyncOperator, SceneOperationAsyncDelegate loadProcess)
        {
            DateTime startTime = DateTime.Now;
            do
            {
                loadProcess?.Invoke(DateTime.Now - startTime, asyncOperator.progress, false);
                yield return null;
            } while (!asyncOperator.isDone);
            loadProcess?.Invoke(DateTime.Now - startTime, asyncOperator.progress, true);
        }

        /// <summary>
        /// 设置为主场景
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static bool SetActiveScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("期望设置为主场景的名称为空或无效，请检查后重试");
                return false;
            }
            var scene = SceneManager.GetSceneByName(sceneName);
            return SceneManager.SetActiveScene(scene);
        }
        
        public static bool IsSceneBuildIndexValid(int sceneBuildIndex) 
            => sceneBuildIndex < 0 || sceneBuildIndex > SceneManager.sceneCountInBuildSettings;

        // 可选：强制进行垃圾回收，更彻底地释放内存
        public static void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}