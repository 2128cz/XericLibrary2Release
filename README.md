<h1 align="center">Xeric Library</h1>

<a href="https://xeric.zicp.fun/LiRuochen_WorkFlow/XericLibrary-Publish">个人发布页面(最新)</a>　
<a href="https://gitee.com/none_9_0/xeric-library2">Gitee发布页面</a>　
<a href="https://github.com/2128cz/XericLibrary">Github库</a>

更多内容请翻阅[百科](https://xeric.zicp.fun/LiRuochen_WorkFlow/XericLibrary-Publish/wiki)

---

## 兼容性

| 版本 | SRP | URP | HDRP |
| --- | --- | --- | --- |
| 2021.3.2f1 | ✔️ | ✔️ | ✔️ |
| 2022.1.24f1 | ✔️ | ✔️ | ✔️ |
| 6000.0.24f1 | ✔️ | ✔️ | ✔️ |

> 插件更名历史：Xeric Library → Aetherial Deconstructionism Paradigm (XericLibrary)

---

## 概述

Xeric Library 是一个专注代码的 Unity 扩展库。

**基础扩展**：针对 Unity 成员语法，扩展多种基本类型的 Linq 语法及语法糖，提供函数平滑、多项式计算、权重拟合、过程分级、工业控制、曲线绘制、路径设置、对象控制、迭代扩展、空间变换、独特结构、文本格式、类型转换、机器编码、开发调试、动态生成、几何创建、快速池化、导航寻路、隔离控制、隔离输入、语义化委托、反射超驰、数学常数、单位换算、排序算法、程序调用、网络连接、Windows 窗口控制等快捷用法。

**特殊类型**：多维布尔、样条曲线、超级单例、多例系统、邻居网络、四叉树、八叉树、字典树、大顶堆、双生哈希表、比特图、软引用封装器、卡尔曼滤波器等。

**常用脚本**：游戏交互、界面适配、弱扩展、绘制工厂、SQL 扩展。

**兼容不规范程序**：针对无设计规则的代码，提供基于反射、CIL 特性等底层语法的程序扩展：
- **超级单例**：无需声明继承，径直调用即为单例。
- **软接口 (SoftInterface)**：快速反射获取/设置，方法委托与字段属性一行调用。
- **联合对象池 (MacroPool.UnionSet)**：Inspector 面板直接配置，代码中只需 Get 和 Release。
- **菜单/查找/命名标记特性**：无视内容查找字段属性方法，类传入即可程序化生成菜单界面。
- **按键宏 (MacroKey)**：状态表处理多按键，识别短按、长按、双击、连击、拖拽及其按下释放状态，支持新输入系统。
- **XericLogger**：精简日志调用栈信息。
- **Gizmos 调试**：媲美原生调试显示功能。

建议添加 Odin 插件，便于呈现更多界面功能。

---

## 蓝图 (Blueprint)

蓝图系统，用于创建运行时轻量蓝图渲染框架。
通过内置的渲染工具实现分层绘制。

## 样式表 (XSSS)

自研 Xeric Super Style Sheet (XSSS) 系统，使用 `.xsss` 自定义文本格式，类 CSS/USS 语法，专为 Unity 组件样式设计。基于字典树进行样式路径查找与匹配，支持命名空间隔离、动态监听热重载。提供完整的编辑器导入、Inspector 编辑及 PropertyDrawer 支持。

---

## 扩展库结构

核心运行时位于 `Developer/XericLibrary/`，主要模块：

| 模块 | 说明 |
| --- | --- |
| `MacroLibrary/` | 核心工具集，含 ~60 个分类宏文件，覆盖数学、几何、天文、排序、编码等 |
| `Type/` | 特殊数据类型：四叉树/八叉树、字典树、多维布尔、软引用、卡尔曼滤波器等 |
| `AI/Chat/` | DeepSeek AI 聊天接口封装 |
| `PlayerController/` | 第一/三人称及上帝视角控制器 |
| `SuperStyleSheet/` | XSSS 样式表运行时 |
| `Debugger/` | Gizmos 调试绘制 |
| `Nav/` | A* 寻路 |
| `Net/` | LAN 局域网通信 |
| `Generation/` | 程序化生成（楼梯、平面开孔等） |
| `Security/` | 程序流安全控制 |
| `CollisionLOD/` | 碰撞体 LOD 系统 |
| `XericComponent/` | 组件系统（样条曲线、网格布局、生命周期核心） |
| `RegistrationActivation/` | 注册激活码校验 |
| `MachineID/` | 机器码验证授权 |

---

## 编辑器界面

- **太阳系模拟界面**：注册激活窗口内嵌的日心说太阳系动画，在编辑器上也能看到漂亮的天体（地球、月球、大气等）。
- **预编译宏管理**：`XericLibrary/Define Symbols/预设宏管理` 菜单入口，勾选框式增删编译标记。
- **DeepSeek AI 对话**：编辑器内 AI 聊天窗口。
- **生命周期性能分析**：`XLifeCycleProfile` 窗口，分析组件生命周期耗时。
- **Hierarchy 面板定制**：增强的 Hierarchy 显示。
- **颜色渐变编辑器**：线性颜色渐变编辑窗口。
- **2D 纹理数组工具**：批量管理纹理数组。

---

## 示例文件

- **Roslyn 检查器**：位于 `Release/Samples~/XericUnityAnalyzer/`，包含编译后的分析器 DLL 及安装说明。提供组件搜索、命名规范、对象关系、富文本检测等代码分析规则，源码见 `Developer/XericUnityAnalyzer/`。
- **配置界面 UI**：`Release/Samples~/ConfigUIElement/`，自动化配置界面的预制体与脚本示例。
- **轨迹线路**：`Release/Samples~/HorizonLineOrbit/`，平面线路绘制与导航示例。

---

## 依赖此插件的扩展

| 插件 | 说明 |
| --- | --- |
| XericUIActionVessel | UI 动作容器插件，提供 UI 界面、UIToolkit 扩展及高性能表格实现 |
| XericCICD | Unity CI/CD 工作流配置界面及 Git 界面 |
| XericWebRequest | Unity WebRequest 封装，提供请求时间轴检查器及 AI 对话示例 |
| DigitalTwinTool | Unity 数字孪生常用功能集合 |
| Xeric_Editor | Unity 编辑器美化和功能集合扩展 |
| ScreenDetectionLayerPicker | 高性能屏幕空间模型数据组提取，支持 WebGL |
| XericMeshTool | 可视化蓝图连线处理模型及模型生成 |
| MaterialLibrary | Shader 库及后处理库，包含 BIRP、URP、HDRP |

---

## 编译标记

使用编译标记启用的特殊功能：

- `XericLibrary`：通用启用特殊功能。

---

## 注意事项

- 插件涉及多平台切换时，可能提供多种自动或手动切换方案，或默认使用 Windows 平台，请注意辨别。
- 如发现版本兼容性、计算错误、调用错误、使用问题，可通过发布页 Issues 或其他联系方式获得帮助。
