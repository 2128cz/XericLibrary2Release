# XericUnityAnalyzer 规则文档

## 所有诊断规则一览

本文档列出 `com.lrss3.deconstruction` 包中 `XericUnityAnalyzer` Roslyn 分析器的所有诊断规则，
涵盖 MacroObject（Unity 层级/组件操作）和 MacroString（TextBlockBuilder 富文本）的优化建议。

---

### XRX0001 — 重复访问 Transform.parent 属性

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 对 Transform 对象的 `.parent` 属性进行重复链式访问（直接链式、变量中转、赋值链）。

**示例：**
```csharp
// 触发警告
transform.parent.parent;
Transform p = transform.parent;
p.parent;  // ← 这里报警
```

**建议方案：** `MacroObject.GetParents()` 扩展方法，返回 `IEnumerable<Transform>`。

---

### XRX0002 — 重复调用 GetParents()

| 属性 | 值 |
|------|---|
| 分类 | Performance |
| 严重性 | Warning |

**触发条件：** 在同一方法中对同一对象调用 `GetParents()` 超过 1 次。

**示例：**
```csharp
// 触发警告
transform.GetParents();
transform.GetParents();  // ← 第二次调用报警
```

**建议方案：** `MacroObject.GetParentsSafety()` 扩展方法，返回 `IList<Transform>` 可复用。

---

### XRX0003 — 重复调用 GetChild()

| 属性 | 值 |
|------|---|
| 分类 | Performance |
| 严重性 | Warning |

**触发条件：** 在同一方法中对同一对象调用 `Transform.GetChild()` 超过 1 次。

**示例：**
```csharp
// 触发警告
transform.GetChild(0);
transform.GetChild(1);  // ← 第二次调用报警
```

**建议方案：** `MacroObject.GetChildren()` 扩展方法，一次性获取所有子节点。

---

### XRX0004 — for 循环 + GetChild 遍历子节点

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 使用 `for (int i = 0; i < xxx.childCount; i++)` 配合 `GetChild(i)` 遍历子节点。

**示例：**
```csharp
// 触发警告
for (int i = 0; i < transform.childCount; i++)
{
    transform.GetChild(i);  // ← 建议替换为 foreach
}
```

**建议方案：** `MacroObject.GetChildren()` 扩展方法，配合 `foreach` 更简洁。

---

### XRX0005 — 重复调用 GetChildren()

| 属性 | 值 |
|------|---|
| 分类 | Performance |
| 严重性 | Warning |

**触发条件：** 在同一方法中对同一对象调用 `GetChildren()` 超过 1 次。

**示例：**
```csharp
// 触发警告
transform.GetChildren();
transform.GetChildren();  // ← 第二次调用报警
```

**建议方案：** `MacroObject.GetChildrenSafety()` 扩展方法，返回 `IList<Transform>` 可复用。

---

### XRX0006 — GetChildren 后提取 gameObject

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 对 `GetChildren()` 结果使用 `.Select(x => x.gameObject)` 或 LINQ 查询提取 gameObject。

**示例：**
```csharp
// 触发警告
transform.GetChildren().Select(c => c.gameObject);
// 或
from c in transform.GetChildren() select c.gameObject;
```

**建议方案：** `MacroObject.GetChildrenGameObject()` 扩展方法，直接返回 `IEnumerable<GameObject>`。

---

### XRX0007 — 嵌套遍历子节点层级

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 嵌套调用 `GetChildren()` 或嵌套使用 for + GetChild 遍历多层子节点。

**示例：**
```csharp
// 触发警告
foreach (var child in transform.GetChildren())
{
    foreach (var grandchild in child.GetChildren())  // ← 嵌套报警
    {
        // ...
    }
}
```

**建议方案：** `MacroObject.GetChildrenBFS()` 扩展方法，BFS 一步到位返回所有层级子节点。

---

### XRX0008 — 在 parent 和 GetChild 之间跳跃访问

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 在同一方法中对同一对象同时访问 `.parent` 属性和调用 `GetChild()` 方法。

**示例：**
```csharp
// 触发警告
var p = transform.parent;      // ← 访问父级
var c = transform.GetChild(0); // ← 访问子级
// 可能在寻找兄弟节点
```

**建议方案：** `MacroObject.GetBrother()` 扩展方法，返回所有兄弟节点。

---

### XRX0009 — GetComponent 判空后 AddComponent

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 先调用 `GetComponent<T>()`，判空后再调用 `AddComponent<T>()` 的"获取或添加"模式。

**示例：**
```csharp
// 触发警告
var comp = obj.GetComponent<Image>();
if (comp == null)
    comp = obj.AddComponent<Image>();
```

**建议方案：** `MacroObject.GetComponentAnyway<T>()` 扩展方法，一步完成获取或添加。

---

### XRX0010 — 对枚举器逐项 GetComponent

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 在 `.Select()` / `foreach` / `for` 循环中对成员逐项调用 `GetComponent`。

**示例：**
```csharp
// 触发警告
transform.GetChildren().Select(c => c.GetComponent<Image>());
// 或
foreach (var child in transform.GetChildren())
    child.GetComponent<Image>();  // ← 逐项报警
```

**建议方案：** `MacroObject.IEnumerable.GetComponent<T>()` 扩展方法，针对枚举器直接提取组件。

---

### XRX0011 — 循环中查找特定组件后跳出

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 在循环中逐项 GetComponent 并在找到后判空 break 的"搜索组件"模式。

**示例：**
```csharp
// 触发警告
Image found = null;
foreach (var child in transform.GetChildren())
{
    found = child.GetComponent<Image>();
    if (found != null) break;  // ← 搜索模式报警
}
```

**建议方案：** `MacroObject.IEnumerable.GetComponent<T>(out T comp)` 扩展方法，一次完成搜索。

---

### XRX0012 — 通过名称查找返回对象

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 遍历子节点并比较 `.name` 来查找特定名称的对象（foreach + name == "xxx" + return）。

**示例：**
```csharp
// 触发警告
foreach (var child in transform.GetChildren())
{
    if (child.name == "Target")      // ← 名称比较
        return child;                 // ← 找到返回
}
// 或 LINQ:
transform.GetChildren().FirstOrDefault(c => c.name == "Target");
```

**建议方案：** `MacroObject.FindFamilyMember()` / `FindFamilyMemberExact()` / `FindFamilyMemberFuzzy()` 扩展方法。

---

### XRX0101 — 富文本字符串字面量

| 属性 | 值 |
|------|---|
| 分类 | Usage |
| 严重性 | Warning |

**触发条件：** 检测到字符串字面量中包含类似 XML 的 Unity 富文本标签（如 `<b>`, `<color=...>`, `<line-height=...>` 等）。

**示例：**
```csharp
// 触发警告
var text = "<b>abc</b>";
```

**建议方案：** 使用 **TextBlockBuilder 包裹器语法** 或 **BlockUtilKit 框架语法** 进行类型安全构建：

<!-- 代码修复 1: TextBlockBuilder 语法 -->
```csharp
// 优化为 TextBlockBuilder 语法
new TextBlockBuilder()
{
    new BBlock("abc")
}
```

<!-- 代码修复 2: 框架语法 -->
```csharp
// 优化为框架语法（配合 using static BlockUtilKit）
Pure(
    B(Label("abc"))
).BuildBlockByAgentOnce()
```

**注意：** 此规则提供两个 CodeFix 选项——"转换为 TextBlockBuilder 语法"和"转换为框架语法"，可通过 IDE 的自动修复功能直接应用。

---

## 规则索引

| ID | 规则标题 | 文件 |
|----|---------|------|
| XRX0001 | 重复访问 parent 属性 | ObjectRelation |
| XRX0002 | 重复调用 GetParents() | ObjectRelation |
| XRX0003 | 重复调用 GetChild() | ObjectRelation |
| XRX0004 | for 循环 + GetChild 遍历 | ObjectRelation |
| XRX0005 | 重复调用 GetChildren() | ObjectRelation |
| XRX0006 | GetChildren + Select gameObject | ObjectRelation |
| XRX0007 | 嵌套遍历子节点层级 | ObjectRelation |
| XRX0008 | parent 和 GetChild 跳跃访问 | ObjectRelation |
| XRX0009 | GetComponent + AddComponent | ComponentSearch |
| XRX0010 | 枚举器逐项 GetComponent | ComponentSearch |
| XRX0011 | 循环查找特定组件 | ComponentSearch |
| XRX0012 | 名称查找返回对象 | FamilyNaming |
| XRX0101 | 富文本字符串字面量 | RichTextDetection |
