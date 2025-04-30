using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace SesothoLine
{
    /// <summary>
    /// 线路绘制编辑器
    /// </summary>
    [CustomEditor(typeof(SesothoPeManager))]
    public class SesothoPeManagerEditor : Editor
    {
        #region 委托事件

        /// <summary>
        /// 当前主要的鼠标动作
        /// </summary>
        private event Action CurrenMouseAction;

        #endregion
        
        #region 字段属性

        /// <summary>
        /// 是编辑绘制模式
        /// </summary>
        public bool IsDebugDrawMode;

        private Vector3 _currentPosition;

        private bool OpKey_ForceStr;
        private bool OpKey_90Arc;
        private bool OpKey_180Arc;
        private bool OpKey_RevCisoid;
        
        #endregion
        
        #region 生命周期

        // private void OnEnable()
        // {
        //     throw new NotImplementedException();
        // }
        //
        // private void OnDisable()
        // {
        //     throw new NotImplementedException();
        // }

        private void OnSceneGUI()
        {
            if (!IsDebugDrawMode) return;
            
            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
            if(GUIUtility.hotControl == 0)
                HandleUtility.AddDefaultControl(controlID);
            
            OpKey_ForceStr = false; 
            OpKey_90Arc = false;
            OpKey_180Arc = false;
            OpKey_RevCisoid = false;
            
            if (Event.current.GetTypeForControl(controlID) == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Escape:
                        IsDebugDrawMode = false;
                        break;
                    case KeyCode.LeftControl:
                        OpKey_ForceStr = true;
                        break;
                    case KeyCode.LeftAlt:
                        OpKey_90Arc = true;
                        break;
                    case KeyCode.LeftShift:
                        OpKey_180Arc = true;
                        break;
                    case KeyCode.R:
                        OpKey_RevCisoid = true;
                        break;
                    default:
                        break;
                }
            }
            
            var mousePos = Event.current.mousePosition;
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(mousePos), out var hit, float.MaxValue, 0xffff))
            {
                _currentPosition = hit.point;
            }

            if (Event.current.type == EventType.MouseUp && GUIUtility.hotControl == 0)
            {
                if (Event.current.button == 0)
                {
                    CurrenMouseAction?.Invoke();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(IsDebugDrawMode ? "退出绘制模式" : "进入绘制模式"))
                IsDebugDrawMode = !IsDebugDrawMode;

            base.OnInspectorGUI();
        }

        #endregion

        #region 画线

        private TerminalLine2T3 _line;
        private TerminalPoint _startPoint;
        private TerminalPoint _endPoint;
        
        private void DarwLine()
        {
            _endPoint.transform.position = _currentPosition;
            
            // _line.SetLineSegment(
            //     _lastPointPosition, _currentPosition, 
            //     _lastPointNormal, _helperNormal
            // );
            
            
            if (OpKey_ForceStr)
                _line.DrawStraightLine();
            else
            {
                var angle = -1;
                if (OpKey_90Arc || OpKey_180Arc)
                {
                    angle = 0;
                    if (OpKey_90Arc)
                        angle += 90;
                    if (OpKey_180Arc)
                        angle += 180;
                }
                _line.AutomaticDrawCircularLine(angle, OpKey_RevCisoid);
            }
            
            // 计算完毕后执行渲染，这里调用有可能会带来空引用问题
            // 但在此之前的阶段已经对此进行过了处理所以没有关系，如果要照抄的话需要进行一些修改。
            _line.UpdateRender();
        }

        #endregion
    }
}
