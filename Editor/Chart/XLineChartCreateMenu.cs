using UnityEngine;
using UnityEditor;

/// <summary>
/// 在 Hierarchy 右键菜单中创建 XLineChart 图表对象。
/// 路径：GameObject / Xeric Library / Chart / Line Chart
///
/// 此文件直接放在 Release/Editor/ 下由 Unity 编译，
/// 不经过外部 DLL 管线，确保菜单立即生效。
/// </summary>
public static class XLineChartCreateMenu
{
    private const string k_MenuPath = "GameObject/Xeric Library/Chart/Line Chart";
    private const string k_ComponentTypeName = "XericLibrary.Runtime.Chart.XLineChart";

    [MenuItem(k_MenuPath, false, 11)]
    private static void CreateLineChart(MenuCommand menuCommand)
    {
        var go = new GameObject("XLineChart");

        go.AddComponent<RectTransform>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 400f);

        // 通过字符串名称添加 XLineChart 组件（运行时程序集）
        System.Type chartType = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            chartType = asm.GetType(k_ComponentTypeName);
            if (chartType != null) break;
        }

        if (chartType != null)
        {
            go.AddComponent(chartType);
        }
        else
        {
            Debug.LogWarning("[XLineChart] 未找到 XLineChart 组件，请确认 Release/Runtime/Chart 已编译");
        }

        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(go, "Create XLineChart");
        Selection.activeObject = go;
    }
}
