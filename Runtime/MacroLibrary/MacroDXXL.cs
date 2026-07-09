using DrawXXL;
using UnityEngine;

/// <summary>
/// MacroDXXL - DXXL (Draw XXL) 高频操作封装库
/// 
/// 功能：将 DXXL 最常用的调试绘制操作封装为一行式工具函数，
/// 省去每次查阅冗长参数列表的麻烦。
/// 
/// 用法：
///   using static MacroDXXL;
///   EnableRuntimeMode();  // 启动时调用一次
///   Line(a, b, Color.red);
///   Circle(center, 0.5f);
///   Grid(Vector3.zero, 10f);
/// </summary>
public static class MacroDXXL
{
    #region 初始化

    /// <summary>启用运行时 Mesh 渲染模式，使绘制在 Game 视图和 Build 中可见</summary>
    public static void EnableRuntimeMode()
    {
        DrawBasics.usedUnityLineDrawingMethod = DrawBasics.UsedUnityLineDrawingMethod.wireMesh;
    }

    #endregion

    #region 基础线段 / 射线 / 向量

    /// <summary>从 A 点到 B 点绘制线段</summary>
    public static void Line(Vector3 a, Vector3 b, Color? color = null, float width = 0f)
    {
        DrawBasics.Line(a, b, color ?? Color.white, width);
    }

    /// <summary>从起点沿方向绘制射线（无限延伸标识）</summary>
    public static void Ray(Vector3 origin, Vector3 direction, Color? color = null, float width = 0f)
    {
        DrawBasics.Ray(origin, direction, color ?? Color.white, width);
    }

    /// <summary>从 A 点到 B 点绘制带箭头向量</summary>
    public static void Vector(Vector3 from, Vector3 to, Color? color = null, float lineWidth = 0f)
    {
        DrawBasics.Vector(from, to, color ?? Color.white, lineWidth);
    }

    /// <summary>从起点沿方向绘制带箭头向量</summary>
    public static void VectorFrom(Vector3 start, Vector3 direction, Color? color = null, float lineWidth = 0f)
    {
        DrawBasics.VectorFrom(start, direction, color ?? Color.white, lineWidth);
    }

    /// <summary>从方向指向终点绘制带箭头向量</summary>
    public static void VectorTo(Vector3 direction, Vector3 end, Color? color = null, float lineWidth = 0f)
    {
        DrawBasics.VectorTo(direction, end, color ?? Color.white, lineWidth);
    }

    /// <summary>绘制点标记（十字 + 可选坐标文本）</summary>
    public static void Point(Vector3 position, Color? color = null, float crossSize = 1f, string text = null)
    {
        DrawBasics.Point(position, text, color ?? Color.white, crossSize);
    }

    /// <summary>绘制多点折线</summary>
    public static void LineString(Vector3[] points, Color? color = null, bool closeLoop = false)
    {
        if (points == null || points.Length < 2) return;
        DrawBasics.LineString(points, color ?? Color.white, closeLoop);
    }

    /// <summary>绘制圆弧（从 start 到 end 绕 circleCenter 的弧）</summary>
    public static void Arc(Vector3 circleCenter, Vector3 start, Vector3 end, Color? color = null, float width = 0f)
    {
        DrawBasics.LineCircled(circleCenter, start - circleCenter, end - circleCenter, color ?? Color.white, width: width);
    }

    /// <summary>绘制角度弧（给定圆心、半径、起始角度、结束角度）</summary>
    public static void AngleArc(Vector3 center, float radius, float startAngleDeg, float endAngleDeg, Color? color = null, Quaternion? orientation = null)
    {
        DrawBasics.LineCircled(center, orientation ?? Quaternion.identity, startAngleDeg, endAngleDeg, radius, color ?? Color.white);
    }

    #endregion

    #region 形状

    /// <summary>绘制圆环</summary>
    public static void Circle(Vector3 center, float radius, Color? color = null, Quaternion? rotation = null)
    {
        DrawShapes.Circle(center, radius, color ?? Color.white, rotation ?? Quaternion.identity);
    }

    /// <summary>绘制球体线框</summary>
    public static void Sphere(Vector3 center, float radius, Color? color = null)
    {
        DrawShapes.Sphere(center, radius, color ?? Color.white);
    }

    /// <summary>绘制立方体线框</summary>
    public static void Cube(Vector3 position, Vector3 scale, Color? color = null, Quaternion? rotation = null)
    {
        DrawShapes.Cube(position, scale, color ?? Color.white, rotation ?? Quaternion.identity);
    }

    /// <summary>绘制带填充面的立方体</summary>
    public static void CubeFilled(Vector3 position, Vector3 scale, Color? color = null, Quaternion? rotation = null)
    {
        DrawShapes.CubeFilled(position, scale, color ?? Color.white, 0.3f, rotation ?? Quaternion.identity);
    }

    /// <summary>绘制平面</summary>
    public static void Plane(Vector3 mountPoint, Vector3 normal, float width = 10f, float length = 10f, Color? color = null)
    {
        DrawShapes.Plane(mountPoint, normal, planeAreaExtentionPosition: mountPoint + normal * 0.5f, color: color ?? Color.white, width: width, length: length);
    }

    /// <summary>绘制圆柱体线框（两端点定义位置，radius 定义半径）</summary>
    public static void Cylinder(Vector3 start, Vector3 end, float radius, Color? color = null)
    {
        Vector3 dir = end - start;
        float height = dir.magnitude;
        if (height < 0.001f) return;
        Vector3 center = (start + end) * 0.5f;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir / height);
        DrawShapes.Cylinder(center, new Vector3(radius * 2f, height, radius * 2f), rot, color ?? Color.white);
    }

    /// <summary>绘制胶囊体线框</summary>
    public static void Capsule(Vector3 start, Vector3 end, float radius, Color? color = null)
    {
        DrawShapes.Capsule(start, end, radius, color ?? Color.white);
    }

    /// <summary>绘制圆锥体线框（basePos 为底面中心，topPos 为顶点，baseRadius 为底面半径）</summary>
    public static void Cone(Vector3 basePos, Vector3 topPos, float baseRadius, Color? color = null)
    {
        Vector3 dir = topPos - basePos;
        float height = dir.magnitude;
        if (height < 0.001f) return;
        Vector3 center = (basePos + topPos) * 0.5f;
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir / height);
        DrawShapes.Cone(center, new Vector3(baseRadius * 2f, height, baseRadius * 2f), rot, color ?? Color.white);
    }

    /// <summary>绘制 3D 箭头（从 start 到 end 的实心箭头）</summary>
    public static void Arrow3D(Vector3 start, Vector3 end, Color? color = null)
    {
        Color c = color ?? Color.white;
        Vector3 dir = end - start;
        float len = dir.magnitude;
        if (len < 0.001f) return;
        Vector3 dirN = dir / len;
        // 箭杆
        DrawBasics.Line(start, end, c);
        // 箭头头部：在终点处添加两条斜线
        float headLen = Mathf.Min(len * 0.25f, 0.5f);
        float headWidth = headLen * 0.5f;
        Vector3 perp = Vector3.Cross(dirN, Vector3.up).normalized;
        if (perp.sqrMagnitude < 0.01f)
            perp = Vector3.Cross(dirN, Vector3.forward).normalized;
        Vector3 perp2 = Vector3.Cross(dirN, perp).normalized;
        DrawBasics.Line(end, end - dirN * headLen + perp * headWidth, c);
        DrawBasics.Line(end, end - dirN * headLen - perp * headWidth, c);
        DrawBasics.Line(end, end - dirN * headLen + perp2 * headWidth, c);
        DrawBasics.Line(end, end - dirN * headLen - perp2 * headWidth, c);
    }

    /// <summary>绘制十字准星（三条轴线上的交叉线）</summary>
    public static void Crosshair(Vector3 position, float size = 1f, Color? color = null)
    {
        Color c = color ?? Color.white;
        float hs = size * 0.5f;
        DrawBasics.Line(position - Vector3.right * hs, position + Vector3.right * hs, c);
        DrawBasics.Line(position - Vector3.up * hs, position + Vector3.up * hs, c);
        DrawBasics.Line(position - Vector3.forward * hs, position + Vector3.forward * hs, c);
    }

    /// <summary>绘制正多边形</summary>
    public static void Polygon(int sides, Vector3 center, float radius, Color? color = null)
    {
        sides = Mathf.Max(3, sides);
        DrawShapes.RegularPolygon(sides, center, radius, color ?? Color.white);
    }

    #endregion

    #region 引擎工具（网格 / 坐标轴 / 旋转 / 缩放 / 包围盒）

    /// <summary>绘制三轴网格平面（X/Y/Z 三个方向）</summary>
    public static void Grid(Vector3 center, float extent = 10f, Color? colorX = null, Color? colorY = null, Color? colorZ = null)
    {
        DrawEngineBasics.GridPlanes(center, extent,
            overwriteColorForX: colorX ?? Color.white,
            overwriteColorForY: colorY ?? Color.white,
            overwriteColorForZ: colorZ ?? Color.white);
    }

    /// <summary>绘制 X 轴网格平面</summary>
    public static void GridX(Vector3 center, float extent = 10f, Color? color = null)
    {
        DrawEngineBasics.XGridPlanes(center, extent, overwriteColor: color ?? Color.white);
    }

    /// <summary>绘制 Y 轴网格平面</summary>
    public static void GridY(Vector3 center, float extent = 10f, Color? color = null)
    {
        DrawEngineBasics.YGridPlanes(center, extent, overwriteColor: color ?? Color.white);
    }

    /// <summary>绘制 Z 轴网格平面</summary>
    public static void GridZ(Vector3 center, float extent = 10f, Color? color = null)
    {
        DrawEngineBasics.ZGridPlanes(center, extent, overwriteColor: color ?? Color.white);
    }

    /// <summary>在指定位置绘制 XYZ 坐标轴（红 X / 绿 Y / 蓝 Z）</summary>
    public static void Axes(Vector3 position, float length = 1f, float lineWidth = 0f)
    {
        Vector3 half = Vector3.one * length * 0.5f;
        // X: 红色
        DrawBasics.Line(position - Vector3.right * half.x, position + Vector3.right * half.x, Color.red, lineWidth);
        // Y: 绿色
        DrawBasics.Line(position - Vector3.up * half.y, position + Vector3.up * half.y, Color.green, lineWidth);
        // Z: 蓝色
        DrawBasics.Line(position - Vector3.forward * half.z, position + Vector3.forward * half.z, Color.blue, lineWidth);
    }

    /// <summary>绘制 Transform 的位置标记</summary>
    public static void Position(Transform transform, Color? color = null)
    {
        DrawEngineBasics.Position(transform, color ?? Color.white);
    }

    /// <summary>绘制 Transform 的缩放指示</summary>
    public static void Scale(Transform transform, Color? color = null)
    {
        DrawEngineBasics.Scale(transform, overwriteColor: color ?? Color.white);
    }

    /// <summary>绘制四元数旋转轴（可视化旋转方向和轴向）</summary>
    public static void Rotation(Quaternion rotation, Vector3 position, float length = 1f, Color? color = null)
    {
        DrawEngineBasics.QuaternionRotation(rotation, position, length_ofUpAndForwardVectors: length, color_ofTurnAxis: color ?? Color.white);
    }

    /// <summary>绘制欧拉角（万向节可视化）</summary>
    public static void EulerAngles(Vector3 eulerAngles, Vector3 position, float gimbalSize = 1f)
    {
        DrawEngineBasics.EulerRotation(eulerAngles, position, gimbalSize: gimbalSize);
    }

    /// <summary>绘制 GameObject 的包围盒</summary>
    public static void Bounds(GameObject gameObject, Color? color = null)
    {
        DrawEngineBasics.Bounds(gameObject, color ?? Color.white);
    }

    /// <summary>绘制 Bounds 结构体</summary>
    public static void Bounds(Bounds bounds, Color? color = null)
    {
        DrawEngineBasics.Bounds(bounds, color ?? Color.white);
    }

    /// <summary>绘制两点间点积的可视化</summary>
    public static void DotProduct(Vector3 v1, Vector3 v2, Vector3 position)
    {
        DrawEngineBasics.DotProduct(v1, v2, position);
    }

    /// <summary>绘制两点间叉积的可视化</summary>
    public static void CrossProduct(Vector3 v1, Vector3 v2, Vector3 position)
    {
        DrawEngineBasics.CrossProduct(v1, v2, position);
    }

    /// <summary>绘制相机视锥</summary>
    public static void CameraFrustum(Vector3 position, Quaternion rotation, float nearClip = 0.3f, float farClip = 1000f, float fov = 60f, float aspect = 16f / 9f, Color? color = null)
    {
        DrawEngineBasics.CameraFrustum(position, rotation, color ?? Color.white, nearClip, farClip, fov, aspect);
    }

    #endregion

    #region 图标

    /// <summary>在 3D 位置绘制预设图标</summary>
    public static void Icon(Vector3 position, DrawBasics.IconType icon, Color? color = null, float size = 1f)
    {
        DrawBasics.Icon(position, icon, color ?? Color.white, size);
    }

    /// <summary>在屏幕上绘制所有图标的图集（快速查阅图标名称）</summary>
    public static void IconAtlas(Vector3 position = default)
    {
        DrawBasics.DrawAtlasOfAllIconsWithTheirNames(position);
    }

    #endregion

    #region 标注 / 标签 / 文本

    /// <summary>在 3D 位置绘制文本标签（rotation 控制朝向，默认 Quaternion.identity 即 xy 平面正向）</summary>
    public static void Label(Vector3 position, string text, Color? color = null, float size = 0.1f, Quaternion? rotation = null)
    {
        DrawText.Write(text, position, color ?? Color.white, size, rotation ?? Quaternion.identity);
    } 

    /// <summary>在 3D 位置绘制带框文本标签（rotation 控制朝向，默认 Quaternion.identity 即 xy 平面正向）</summary>
    public static void LabelFramed(Vector3 position, string text, Color? color = null, float size = 0.1f, Quaternion? rotation = null)
    {
        DrawText.WriteFramed(text, position, color ?? Color.white, size, rotation ?? Quaternion.identity);
    }

    /// <summary>给 GameObject 绘制屏幕空间标签（始终面向相机）</summary>
    public static void Tag(GameObject go, string text = null, Color? textColor = null, Color? boxColor = null)
    {
        DrawEngineBasics.TagGameObject(go, text ?? go.name, textColor ?? Color.white, boxColor ?? new Color(0, 0, 0, 0.5f));
    }

    /// <summary>给 GameObject 绘制屏幕空间标签（覆盖在屏幕上）</summary>
    public static void TagScreenspace(GameObject go, string text = null, Color? textColor = null)
    {
        DrawEngineBasics.TagGameObjectScreenspace(go, text ?? go.name, textColor ?? Color.white);
    }

    /// <summary>在屏幕空间（视口坐标）绘制文本</summary>
    public static void ScreenText(string text, Vector2 viewportPos, Color? color = null, float size = 0.025f)
    {
        DrawText.WriteScreenspace(text, viewportPos, color ?? Color.white, size, Vector2.up);
    }

    /// <summary>在 3D 位置绘制屏幕空间文本（自动投影到屏幕）</summary>
    public static void ScreenTextAtWorldPos(string text, Vector3 worldPos, Color? color = null, float size = 0.025f)
    {
        DrawText.WriteScreenspace(text, worldPos, color ?? Color.white, size, Vector2.up);
    }

    /// <summary>显示布尔值的可视化指示器</summary>
    public static void BoolDisplay(bool value, Vector3 position, string name = null, Color? color = null)
    {
        DrawEngineBasics.BoolDisplayer(value, position, name, color_forTextAndFrame: color ?? Color.white);
    }

    #endregion

    #region 物理

    /// <summary>绘制射线检测结果（命中时绿色，未命中时红色）</summary>
    public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hitInfo, Color? hitColor = null, Color? missColor = null, int layerMask = ~0)
    {
        bool hit = Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);
        Color color = hit ? (hitColor ?? Color.green) : (missColor ?? Color.red);
        DrawBasics.Ray(origin, direction, color, width: 0);
        if (hit)
        {
            DrawBasics.Point(hitInfo.point, color, 0.3f);
        }
        return hit;
    }

    /// <summary>绘制球体投射检测结果</summary>
    public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, Color? color = null, int layerMask = ~0)
    {
        bool hit = Physics.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask);
        Color c = color ?? (hit ? Color.green : Color.red);
        Vector3 endPos = hit ? hitInfo.point : origin + direction.normalized * maxDistance;
        DrawShapes.Sphere(endPos, radius, c);
        DrawBasics.Line(origin, endPos, c);
        return hit;
    }

    /// <summary>绘制球体重叠检测范围</summary>
    public static Collider[] OverlapSphere(Vector3 position, float radius, Color? color = null, int layerMask = ~0)
    {
        DrawShapes.Sphere(position, radius, color ?? new Color(0, 1, 0, 0.3f));
        return Physics.OverlapSphere(position, radius, layerMask);
    }

    /// <summary>绘制盒体重叠检测范围</summary>
    public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Color? color = null, Quaternion? rotation = null, int layerMask = ~0)
    {
        DrawShapes.Cube(center, halfExtents * 2f, color ?? new Color(0, 1, 0, 0.3f), rotation ?? Quaternion.identity);
        return Physics.OverlapBox(center, halfExtents, rotation ?? Quaternion.identity, layerMask);
    }

    #endregion

    #region 测量

    /// <summary>绘制两点间距离的阈值指示（超过阈值变样式）</summary>
    public static void DistanceThreshold(Vector3 from, Vector3 to, float threshold, Color? nearColor = null, Color? farColor = null)
    {
        DrawMeasurements.DistanceThreshold(from, to, threshold,
            displayDistanceAlsoAsText: true,
            overwriteColor_forNear: nearColor ?? Color.green,
            overwriteColor_forFar: farColor ?? Color.red);
    }

    /// <summary>绘制两点间双阈值指示（近/中/远三种样式）</summary>
    public static void DistanceThresholds(Vector3 from, Vector3 to, float smallThreshold, float bigThreshold)
    {
        DrawMeasurements.DistanceThresholds(from, to, smallThreshold, bigThreshold, displayDistanceAlsoAsText: true);
    }

    #endregion

    #region 2D 辅助

    /// <summary>在 2D 平面（X-Y 平面）绘制线段</summary>
    public static void Line2D(Vector2 a, Vector2 b, Color? color = null, float width = 0f, float zPos = float.PositiveInfinity)
    {
        DrawBasics2D.Line(a, b, color ?? Color.white, width, custom_zPos: zPos);
    }

    /// <summary>在 2D 平面绘制带箭头向量</summary>
    public static void Vector2D(Vector2 from, Vector2 to, Color? color = null, float zPos = float.PositiveInfinity)
    {
        DrawBasics2D.Vector(from, to, color ?? Color.white, custom_zPos: zPos);
    }

    /// <summary>在 2D 平面绘制圆形</summary>
    public static void Circle2D(Vector2 center, float radius, Color? color = null, float zPos = 0f)
    {
        DrawShapes.Circle(new Vector3(center.x, center.y, zPos), radius, color ?? Color.white);
    }

    #endregion
}
