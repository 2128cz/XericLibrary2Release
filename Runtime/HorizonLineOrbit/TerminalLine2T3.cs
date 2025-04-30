using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;  
using System.Text;  
using Deconstruction.Element;
using Deconstruction.Interface;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Serialization;
using XericLibrary.Runtime.Debuger;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.Type;

namespace SesothoLine
{
    
    using LineSegment2 = Deconstruction.Element.PromptLine2.LineSegment2;
    
    /// <summary>
    /// 线终端执行脚本，使用二维线计算器，驱动三维线渲染器。
    /// </summary>
    public class TerminalLine2T3 : PromptLine2, 
        ILinkconfidentPe    // 链表跟踪
    {
        #region 字段属性

        private LinkedListNode<PlacementBase> _linkNode;
        
        /// <summary>
        /// 本地缓存
        /// </summary>
        private LineSegment2 _lineSegment = new LineSegment2();

        public bool DebugDrawGrid = false;
        
        #endregion
        
        #region 生命周期

        protected override void Start()
        {
            base.Start();
            InitPromptLine<LineRendererUnbodied>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SesothoPeManager.Inst.AddEmbedded(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SesothoPeManager.Inst.RemoveEmbedded(this);
        }

        protected virtual void OnDestroy()
        {
            
        }
        
        protected override void AgencyOnEnable()
        {
            base.AgencyOnEnable();
        }

        protected override void AgencyOnDisable()
        {
            base.AgencyOnDisable();
        }

        protected override void AgencyMainUpdate()
        {
            base.AgencyMainUpdate();
            
            // 绘制线路本身在网格中的索引位置
            if (DebugDrawGrid && 
                NeighborGridIndex.SafeGetAsMappingIndex(out var indexs))
            {
                foreach (var index in indexs.Indexs)
                {
                    MacroDebugDraw.DrawDownArrow(
                        index.GetCellWorldIndexPosition() + MacroMath.RandomVector3(0.1f, Identifier), 
                        Color.red);
                }
            }
            
        }

        protected override void AgencyAsyncBeforeUpdate()
        {
            base.AgencyAsyncBeforeUpdate();
        }

        protected override void AgencyAsyncUpdate()
        {
            base.AgencyAsyncUpdate();
        }

        protected override void AgencyAsyncAfterUpdate()
        {
            base.AgencyAsyncAfterUpdate();
        }

        #endregion

        #region 线路构建

        /// <summary>
        /// 创建合理坐标空间
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ah"></param>
        /// <param name="bh"></param>
        /// <returns></returns>
        public LineSegment2 SetLineSegment(Vector3 a, Vector3 b, Vector3 ah = default, Vector3 bh = default)
        {
            _lineSegment.SetValue(
                a.UpwardPlaneToVector2(),
                b.UpwardPlaneToVector2(),
                ah.UpwardPlaneToVector2(),
                bh.UpwardPlaneToVector2());
                // ah?.GetEndPointNormal ?? default,
                // bh?.GetStartPointNormal ?? default);
            return _lineSegment;
        }

        public void AutomaticDrawCircularLine(float constraintAngle = -1, bool fullArc = false)
        {
            AutomaticDrawCircularLine(_lineSegment, constraintAngle, fullArc);
        }

        private LineSegment2 straightLine = new LineSegment2();
        public void DrawStraightLine()
        {
            straightLine.SetValue(_lineSegment.pointA, _lineSegment.pointB);
            AutomaticDrawCircularLine(straightLine);
        }

        
        /// <summary>
        /// 重新计算原点
        /// <code>
        /// 如果此轨迹的原点在零点，那么通过此方法可以将原点设置到起点和终点之间。
        /// 注意：使用前需要将轨迹设为本地坐标
        /// </code>
        /// </summary>
        public void ReapplyOrigin()
        {
            if (TrajectoryRenderer == null || TrajectoryCalculator == null)
            {
                Debug.LogError("当前轨迹或线渲染器无效，无法计算原点");
                return;
            }
            
            // 假定其中的轨迹是本地坐标的
            
            // 期望的本地零点
            var center = ((TrajectoryCalculator.GetStartPointPosition + TrajectoryCalculator.GetEndPointPosition) / 2).UpwardPlaneToVector3();
            // 本地原点
            var local = TrajectoryCalculator.Origin.UpwardPlaneToVector3();
            // 期望偏移
            var offset = local - center;
            
            TrajectoryCalculator.Origin = offset.UpwardPlaneToVector2();
            transform.position += center;
            
            // 更新
            UpdateRender();
        }
        
        #endregion
        
        #region 实现 - ILinkconfidentPe
        
        public bool GetLinkedNode(out LinkedListNode<PlacementBase> node)
        {
            if (_linkNode == null)
            {
                node = null;
                return false;
            }

            node = _linkNode;
            return true;
        }
        
        public void SetLinkedNode(LinkedListNode<PlacementBase> node)
        {
            _linkNode = node;
        }
        
        #endregion
        
        #region 重写 - ISerializablePost

        public override SerializerHelper.Type.SerializeUnion SerializedOccurs()
        {
            return base.SerializedOccurs();
        }
        public override bool CheckDeserializeUnion(SerializerHelper.Type.SerializeUnion context)
        {
            return base.CheckDeserializeUnion(context);
        }
        public override void DeserializeOccurs(SerializerHelper.Type.SerializeUnion context)
        {
            base.DeserializeOccurs(context);
        }
        public override void DeserializeHysteresisOccurs(SerializerHelper.Type.SerializeUnion context)
        {
            base.DeserializeHysteresisOccurs(context);
        }
        
        #endregion
    }
}
