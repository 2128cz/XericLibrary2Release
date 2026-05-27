#define _DEBUG_
// #undef _DEBUG_

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Deconstruction.Element;
using UnityEngine;

using Deconstruction.Manager;
using Deconstruction.Tool;
using Deconstruction.Type.Area;
using Deconstruction.Type.Serialize;
using SerializerHelper.Type;
using UnityEngine.UI;
using XericLibrary.Runtime.Debuger;
using XericLibrary.Runtime.MacroLibrary;

namespace SesothoLine
{
    /// <summary>
    /// 主要管理器
    /// <code>
    /// 不建议继续封装管理器类
    /// </code>
    /// </summary>
    public sealed class SesothoPeManager : PlaceElementManager
    {
        #region 静态成员

        /// <summary>
        /// 此处提供的单例与父类单例是独立的
        /// </summary>
        public new static SesothoPeManager Inst => _inst;
        private static SesothoPeManager _inst;

        /// <summary>
        /// 线路绘制工具
        /// </summary>
        public static SesothoArrangementWiresTool WiresTool => _wiresTool;
        private static SesothoArrangementWiresTool _wiresTool;

        /// <summary>
        /// 光标下最近的线上交点
        /// </summary>
        public static Vector3 NearestLineTangentPoint => _lineTangentTraceTool.NearsetPoint;
        /// <summary>
        /// 光标下最近的线
        /// </summary>
        public static PlacementBase NearestLineTangentObject => _lineTangentTraceTool.NearsetObject;
        /// <summary>
        /// 光标下最近的对象原点
        /// </summary>
        public static Vector3 NearestPoint => TrackingTool.TrackingCurrentPosition;
        /// <summary>
        /// 光标下最近的对象
        /// </summary>
        public static PlacementBase NearestObject => TrackingTool.TrackingCurrentTarget;
        
        private static TrajectoryTrackingTool.TrackPersistentCommunicate _lineTangentTraceTool;
        
        #endregion

        #region 调试功能

#if UNITY_EDITOR
        
        public bool EnableGridDebugDraw = false;


        public void DebugFunc()
        {
            if (EnableGridDebugDraw)
            {
                PlacementNeighbor.DebugDrawGrid(); 
            }
        }
        
#endif
        
        #endregion
        
        #region 生命周期
        
        protected override void Awake()
        {
            base.Awake();
            _inst = this;
            
            if (LineMaterial == null)
                Debug.LogError("需要给管理器提供一个线段材质");
            if (PointMaterial == null)
                Debug.LogError("需要给管理器提供一个点材质");
            
            // 初始化画线工具
            _wiresTool = new SesothoArrangementWiresTool();
            _wiresTool.InitializeParent(transform);

            _lineTangentTraceTool = TrackingTool.InstantiationTrackCaculate(PlaceholdersAreaType.LinearContinuity);
            
            // 手动开启代理更新
            EnableUpdate = true;
            
            
        }

        protected override void Start()
        {
            base.Start();
        }

        private NeighborGrid<PlacementBase>.NeighborGridIndex index;
        protected override void Update()
        {
            base.Update();
#if UNITY_EDITOR    
            DebugFunc();
#endif      
            if (Input.GetMouseButtonDown(2))
            {
                index = PlacementNeighbor.GetNeighborIndex(null);
                index.SetDrivenAsWorld(InputHelper.CurrentPosition);
                Debug.Log($"{index.GetDrivenAsLinear()} => {index.GetNeighbor().FirstOrDefault()}");
            }

            // new SerializeUnion();
        }

        private string serializeContext = null;
        protected override void LateUpdate()
        {
            base.LateUpdate();
            // if (DecisionMakerToolBase.ActiveDecisionMaker == null || 
            //     !DecisionMakerToolBase.ActiveDecisionMaker.EnableTool)
#if !UNITY_EDITOR || !_DEBUG_
            return;
#endif
            // 工具功能测试
            if (Input.GetKeyDown(KeyCode.L))
            {
                _wiresTool.EnableTool = !_wiresTool.EnableTool;
                Debug.Log($"{(_wiresTool.EnableTool ? "绘制工具已激活" : "工具已取消激活")}");
            }
            
            // 存档功能测试
            if (Input.GetKeyDown(KeyCode.S))
            {
                var obj = _wiresTool.ToolSerializeDispost();
                serializeContext = MacroFile.JsonSerializerFormatter.formatter.Serializer(obj);
                Debug.Log(obj.ToString());
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (serializeContext == null)
                {
                    Debug.Log("反序列化内容为空");
                    return;
                }

                var obj = MacroFile.JsonSerializerFormatter.formatter.Deserializer<SerializeUnion>(serializeContext);
                _wiresTool.ToolDiserializDispost(obj);
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                _wiresTool.EnableCompress = !_wiresTool.EnableCompress;
                if (_wiresTool.EnableCompress )
                    Debug.Log("开启压缩");
                else
                    Debug.Log("关闭压缩");
            }
            
            
            // 绘制一下吸附的目标位置
            if (TrackingTool.EnableTool || TrackingTool._enableAuxiliaryTool)
            {
                MacroDebugDraw.DrawDownArrow(NearestPoint, Quaternion.identity, Color.magenta);
                MacroDebugDraw.DrawDownArrow(NearestLineTangentPoint, Quaternion.identity, Color.green);
            }
        }

        #endregion

    }
}
