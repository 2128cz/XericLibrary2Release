using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using XericLibrary.Runtime.CustomEditor;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.Type;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShowMyNameInScene : MonoBehaviour
{
    [Rename("启用")] public bool Enable = false;
    [Rename("仅选中时显示")] public bool OnlyShowOnSelect = false;
    [Rename("目标是子项")] public bool ShowAllChindName = true;
    [Rename("显示名称")] public bool ShowGameObjectName = false;
    [Rename("显示模型名称")] public bool ShowMeshFilter_MeshName = false;

    public bool RandomEachObjectColor = false;
    public Color TextColor = Color.white;
    public RectOffset TextBorder = new RectOffset(0,0,0,0);
    public RectOffset TextMargin = new RectOffset(0,0,0,0);
    public RectOffset TextPadding = new RectOffset(0,0,0,0);
    public RectOffset TextOverflow = new RectOffset(0,0,0,0);
    public Font font;
    public int fontSize = 0;
    public FontStyle fontStyle = FontStyle.Normal;
    public TextClipping TextClipping = TextClipping.Overflow;
    public ImagePosition ImagePosition = ImagePosition.ImageLeft;
    public Vector2 ContentOffset = Vector2.zero;
    public Vector2 FixedSize = Vector2.zero;
    public Bool2 Stretch = new Bool2();
    
    private GUIStyle TextStyle;
    private StringBuilder _sb = new StringBuilder();
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Enable)
            return;
        if (OnlyShowOnSelect && !ReferenceEquals(Selection.activeGameObject, this))
            return;

        TextStyle = new GUIStyle()
        {
            normal = new GUIStyleState()
            {
                textColor = TextColor,
                // background = 
            },
            border = TextBorder,
            margin = TextMargin,
            padding = TextPadding,
            overflow = TextOverflow,
            font = font,
            fontSize = fontSize,
            fontStyle = fontStyle,
            richText = true,
            clipping = TextClipping,
            imagePosition = ImagePosition,
            contentOffset = ContentOffset,
            fixedHeight = FixedSize.y,
            fixedWidth = FixedSize.x,
            stretchHeight = Stretch.y,
            stretchWidth = Stretch.x,
        };
        
        if (ShowAllChindName)
        {
            foreach (var item in transform.GetChildren())
            {
                if (!item.gameObject.activeInHierarchy)
                    continue;
                SwitchFilter(item);
            }
        }
        else
        {
            DrawGameObjectNameInScene(transform, transform.name);
        }
    }

    private void SwitchFilter(Transform target)
    {
        if (_sb == null)
            _sb = new StringBuilder();
        _sb.Clear();
        
        if (RandomEachObjectColor)
            TextStyle.normal.textColor = MacroColor.RandomHueColor(target.gameObject.GetInstanceID());
        
        var position = target.position;
        var mf = target.GetComponent<MeshFilter>();
        if (mf != null)
        {
            var mesh = mf.mesh;
            if (mesh != null)
                position += target.rotation * mesh.bounds.center;
        }

        if (ShowGameObjectName)
            _sb.AppendLine($"Name: {target.name}");
        
        if (ShowMeshFilter_MeshName)
            _sb.AppendLine($"Mesh: {GetComponentName<MeshFilter>(target, a => a.mesh?.name ?? "unknown")}");

        DrawNameInScene(position, _sb.ToString());
    }

    private void DrawGameObjectNameInScene(Transform obj, string name)
    {
        Handles.Label(obj.position, name,TextStyle);
    }

    private void DrawNameInScene(Vector3 position, string name)
    {
        Handles.Label(position, name, TextStyle);
    }

    private string GetComponentName<T>(Transform target, Func<T, string> getName)
        where T : Component
    {
        return getName(target.GetComponent<T>());
    }
    
#endif
}