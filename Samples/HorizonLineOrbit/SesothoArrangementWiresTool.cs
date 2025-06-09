using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Deconstruction.Element;
using Deconstruction.Interface;
using Deconstruction.Manager;
using Deconstruction.Tool;
using Deconstruction.Type;
using Deconstruction.Type.DMToolSlot;
using Deconstruction.Type.Linkable;
using SerializerHelper.Type;
using UnityEngine;
using UnityEngine.Pool;
using XericLibrary.Runtime.MacroLibrary;
using XericLibrary.Runtime.Nav;

namespace SesothoLine
{
    /// <summary>
    /// 线路绘制工具类
    /// </summary>
    public class SesothoArrangementWiresTool : DecisionMakerToolBase
    {
        #region 事件委托

        /// <summary>
        /// 工具中成员变更委托
        /// </summary>
        public delegate void ToolMemberChange(PlacementBase obj);

        /// <summary>
        /// 工具中成员添加事件
        /// </summary>
        public event ToolMemberChange OnAdditional;

        /// <summary>
        /// 工具中成员移除事件
        /// </summary>
        public event ToolMemberChange OnDecreasing;
        
        /// <summary>
        /// 当任意元素苏醒
        /// </summary>
        public event ToolMemberChange OnAnyVivification;
        
        /// <summary>
        /// 当任意元素休眠
        /// </summary>
        public event ToolMemberChange OnAnyDormancy;
        
        #endregion
        
        #region 快捷键

        /*
         * 1 控制强制直线
         * 2,3 控制圆弧的角度为90和180度
         * R 反转圆弧
         * shift+b 忽略对象吸附
         * shift+n 忽略网格吸附
         */
        
        /// <summary>
        /// 强制直线
        /// </summary>
        public static KeyPack OpKey_ForceStr = new KeyPack(KeyCode.Alpha1);
        /// <summary>
        /// 强制90度圆弧
        /// </summary>
        public static KeyPack OpKey_90Arc = new KeyPack(KeyCode.Alpha2);
        /// <summary>
        /// 强制180度圆弧
        /// </summary>
        public static KeyPack OpKey_180Arc = new KeyPack(KeyCode.Alpha3);
        /// <summary>
        /// 反转顺向，比如单圆弧的方向翻转。
        /// 话说shift+alt+r是英伟达的一个快捷键呢，有点冲突
        /// </summary>
        public static KeyPack OpKey_RevCisoid = new KeyPack(KeyCode.R);
        /// <summary>
        /// 忽略吸附功能
        /// </summary>
        public static KeyPack OpKey_IgnoreNeighborAdsorption = new KeyPack(KeyCode.LeftShift, KeyCode.B);
        /// <summary>
        /// 忽略网格吸附
        /// </summary>
        public static KeyPack OpKey_IgnoreGridAdsorption = new KeyPack(KeyCode.LeftShift, KeyCode.N);
        
        #endregion

        #region 字段属性

        private ObjectPool<TerminalLine2T3> _linePool;
        private ObjectPool<TerminalPoint> _pointPool;

        #endregion

        #region 获取转换

        /// <summary>
        /// 获取光标下最近的对象为点
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static bool GetNearestObjectAsPoint(out TerminalPoint point)
        {
            if (SesothoPeManager.NearestObject is not null &&
                SesothoPeManager.NearestObject is TerminalPoint terminalPoint)
            {
                point = terminalPoint;
                return true;
            }
            point = null;
            return false;
        }
        
        /// <summary>
        /// 获取光标下最近的对象为线
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool GetNearestObjectAsLine(out TerminalLine2T3 line)
        {
            if (SesothoPeManager.NearestObject is not null &&
                SesothoPeManager.NearestObject is TerminalPoint terminalPoint &&
                terminalPoint.LinkedData.GetOpposite(LinkType.Export) is TerminalLine2T3 terminalLine)
            {
                line = terminalLine;
                return true;
            }
            line = null;
            return false;
        }

        /// <summary>
        /// 获取以给定点上与最近的相切线相对
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static bool GetNearestObjectAsLine(TerminalPoint point, out TerminalLine2T3 line, out Vector3 normal)
        {
            if (point is null)
                goto End;
            // 检查最近线切线是否有效，是否相连
            if (SesothoPeManager.NearestLineTangentObject is TerminalLine2T3 otherLine && 
                point.TryGetLinkLineNormal(otherLine, out normal))
            {
                line = otherLine;
                return true;
            }
           
            End:
            line = null;
            normal = default;
            return false;
        }

        #endregion
        
        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
            
            _linePool = new ObjectPool<TerminalLine2T3>(
                createFunc: CreatElement<TerminalLine2T3>,
                actionOnGet: ElementActive,
                actionOnRelease: ElementInactive,
                actionOnDestroy: a =>
                { },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 10000);
            
            _pointPool = new ObjectPool<TerminalPoint>(
                createFunc: CreatElement<TerminalPoint>,
                actionOnGet: ElementActive,
                actionOnRelease: ElementInactive,
                actionOnDestroy: a =>
                { },
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 10000);
            
        }

        protected override void OnEnableTool()
        {
            // 启用时创建一个输入等待行为，随后进入绘制行为
            var target = GenerateOperateSlot<WaitForUserInput_OpSlot>();
            target.InitializeNextTodo(() => new DrawTerminalLine2T3_OpSlot());
            
            Debug.Log("激活线路绘制工具");
        }

        protected override void OnDisableTool()
        {
	        
        }

        protected override void OnFinalEnd()
        {
            base.OnFinalEnd();
            EnableTool = false;
        }

        #endregion

        #region 序列化

        public override SerializeUnion ToolSerializeDispost()
        {
            base.ToolSerializeDispost();
            foreach (var element in PlaceElementManager.Inst.StructuralLinkedList)
            {
                var union = element.SerializedOccurs();
                // 索引是后面用来标识类的 
                union.Index = TypeConsignMap.AddMapIndex(element.GetType());
                union.RefreshSerializedContext();
                SerializeTemp.Add(union);
            }
            SerializeTemp.RefreshSerializedContext();
            return SerializeTemp;
        }

        public override bool ToolDiserializDispost(SerializeUnion context)
        {
            if (!base.ToolDiserializDispost(context))
                return false;
            
            var deserializeList = new List<(PlacementBase, SerializeUnion)>();
            var safeCount = ushort.MaxValue;
            Debug.Log("反序列化过程开始创建对象");
            while (0 <-- safeCount && SerializeTemp.IndexMoveToNext(out var block))
            {
                var union = SerializeTemp.GetDeserializeObject<SerializeUnion>();
                var type = TypeConsignMap.GetMapType(union.Index);
                if (GetElement(type, out var obj))
                {
                    deserializeList.Add((obj, union));
                    union.RefreshDeserializeContext();
                    union.IndexMoveToStart();
                    obj.DeserializeOccurs(union);
                    // switch (obj)
                    // {
                    //     case TerminalLine2T3 line:
                    //         line.DeserializeOccurs(union);
                    //         break;
                    //     case TerminalPoint point:
                    //         point.DeserializeOccurs(union);
                    //         break;
                    // }
                }
                else
                    Debug.LogError($"无法创建序列化元素，原因是类型不支持{type}");
                
                if (block) break;
            }
            Debug.Log("反序列化过程开始恢复链接关系");
            foreach (var item in deserializeList)
                item.Item1.DeserializeHysteresisOccurs(item.Item2);
            
            return true;
        }

        #endregion
        
        #region 方法

        /// <summary>
        /// 创建元素
        /// </summary>
        /// <returns></returns>
        private T CreatElement<T>()
            where T : PlacementBase, ILinkconfidentPe
        {
            var obj = CreatPlacementObject<T>();
            obj.OnPlancementDestory += a =>
            {
                if (a is null)
                {
                    Debug.LogError("元素已经被销毁，无法回收链表元素");
                    return;
                }
                if (a is T b &&
                    b.GetLinkedNode(out var node))
                    SesothoPeManager.Inst.RemoveLinkedTarget(node);
                else
                    Debug.LogError("元素并非工具元素类型，或者其中的链表节点已丢失，导致无法回收链表元素");
            };
            return obj;
        }

        /// <summary>
        /// 池对象激活
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        private void ElementActive<T>(T obj)
            where T : PlacementBase, ILinkconfidentPe
        {
            obj.gameObject.SetActive(true);
            OnAnyVivification?.Invoke(obj);
        }

        /// <summary>
        /// 池对象取消激活
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        private void ElementInactive<T>(T obj)
            where T : PlacementBase, ILinkconfidentPe
        {
            obj.gameObject.SetActive(false);
            OnAnyDormancy?.Invoke(obj);
        }
        
        /// <summary>
        /// 获取元素
        /// </summary>
        /// <returns></returns>
        public bool GetElement<T>(out T obj)
            where T : PlacementBase, ILinkconfidentPe
        {
            var name = typeof(T).Name;
            switch (name)
            {
                case nameof(TerminalLine2T3):
                    obj = _linePool.Get() as T;
                    return true;
                case nameof(TerminalPoint):
                    obj = _pointPool.Get() as T;
                    return true;
                default:
                    Debug.LogError($"给定的元素类型({name})并非预期，这将跳过对象池过程");
                    break;
            }
            obj = null;
            return false;
        }
        
        /// <summary>
        /// 获取元素
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool GetElement(Type type, out PlacementBase obj)
        {
            var name = type.Name;
            switch (name)
            {
                case nameof(TerminalLine2T3):
                    obj = _linePool.Get();
                    return true;
                case nameof(TerminalPoint):
                    obj = _pointPool.Get();
                    return true;
                default:
                    Debug.LogError($"给定的元素类型({name})并非预期，这将跳过对象池过程");
                    break;
            }
            obj = null;
            return false;
        }
        

        /// <summary>
        /// 删除元素
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        public void DeleteElement<T>(T obj)
            where T : PlacementBase, 
            ILinkconfidentPe
        {
            var name = typeof(T).Name;
            switch (name)
            {
                case nameof(TerminalLine2T3):
                    _linePool.Release(obj as TerminalLine2T3);
                    break;
                case nameof(TerminalPoint):
                    _pointPool.Release(obj as TerminalPoint);
                    break;
                default:
                    Debug.LogError($"给定的元素类型({name})并非预期，这将跳过对象池过程");
                    break;
            }
        }

        
        
        /// <summary>
        /// 保存元素链，在创建并常态化元素后都需要执行的操作 (注意不是在创建后立刻保存)
        /// <code>
        /// 如果这是一个线，那么还应该调用 AddBulkLineToNeighborGrid;
        /// 如果这是一个点，那么应该调用 AddPeToNeighborGrid。
        /// </code>
        /// </summary>
        public void PersistentElement<T>(T obj)
            where T : PlacementBase, 
            ILinkconfidentPe
        {
            // 保存链表结构，反过来也持有这个节点，便于后续查找
            var lineNode = SesothoPeManager.Inst.AddLinkedTarget(obj);
            obj.SetLinkedNode(lineNode);
        }

        /// <summary>
        /// 移除元素链
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        public void ExcisionElement<T>(T obj)
            where T : PlacementBase, 
            ILinkconfidentPe
        {
            if (obj.GetLinkedNode(out var node))
                SesothoPeManager.Inst.RemoveLinkedTarget(node);
        }
        
        
        /// <summary>
        /// 添加小型元素到网格中
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        public void AddPeToNeighborGrid<T>(T obj)
            where T : PlacementBase,
            ILinkconfidentPe
        {
            if (obj.NeighborGridIndex != null)
            {
                Debug.LogError("无法重复插入元素：当前向管理器中插入的的元素，可能已经存在于其他管理器内，请先退出其他管理器后重试。");
                return;
            }
            SesothoPeManager.Inst.InsertNeighbor(obj, out var index);
            obj.NeighborGridIndex = index;
            
            // 回调事件
            OnAdditional?.Invoke(obj);
        }

        /// <summary>
        /// 添加大型元素到网格中
        /// </summary>
        public void AddBulkPeToNeighborGrid<T>(T obj)
            where T : PlacementBase, 
            ILinkconfidentPe, 
            IPossessorTrajectory2
        {
            if (obj.NeighborGridIndex != null)
            {
                Debug.LogError("无法重复插入元素：当前向管理器中插入的的元素，可能已经存在于其他管理器内，请先退出其他管理器后重试。");
                return;
            }
            SesothoPeManager.Inst.InsertGiantNeighbor<T>(obj, out var index);
            obj.NeighborGridIndex = index;
            
            // 回调事件
            OnAdditional?.Invoke(obj);
        }

        /// <summary>
        /// 移除这个小元素
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        public void RemovePeToNeighborGrid<T>(T obj)
            where T : PlacementBase,
            ILinkconfidentPe
        {
            if (obj.NeighborGridIndex != null && 
                obj.NeighborGridIndex.GetAsIndex(out var index))
                SesothoPeManager.Inst.RemoveNeighbor(index);
            else
                SesothoPeManager.Inst.RemoveNeighbor(obj);
            ExcisionElement(obj);
            // 回调事件
            OnDecreasing?.Invoke(obj);
        }
        
        /// <summary>
        /// 移除这个大型元素
        /// </summary>
        public void RemoveBulkPeFormNeighborGrid<T>(T obj)
            where T : PlacementBase, 
            ILinkconfidentPe, 
            IPossessorTrajectory2
        {
            if (obj.NeighborGridIndex != null && 
                obj.NeighborGridIndex.GetAsMappingIndex(out var index))
                SesothoPeManager.Inst.RemoveGiantNeighbor(index);
            else
                SesothoPeManager.Inst.RemoveGiantNeighbor<T>(obj);
            ExcisionElement(obj);
            // 回调事件
            OnDecreasing?.Invoke(obj);
        }

        #endregion

        #region 绘制寻路

        /*
         * 没写完，如果不嫌麻烦可以看看Astart类，或者自己实现。
         *
         * 主要提供绘制线路时的多段线拟合，障碍物避让
         */
        
        /// <summary>
        /// 获取一个可以通过的路径
        /// <code>
        /// 这是一个只在向上的二维平面上有效的路径。
        /// 寻路的规则是仅避开建筑物，贪婪算法。
        /// </code>
        /// </summary>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <param name="avoidRadius">避让半径，在遭遇碰撞时将沿法向退回这个距离</param>
        public DrawWayPoints GetPassThroughRouteShortcut(Vector3 startPoint, Vector3 endPoint, float avoidRadius)
        {
            var result = new DrawWayPoints();
            Debug.LogError("此方法未完成");
            return result;
        }

        /// <summary>
        /// 获取一个可以通过的路径的迭代器；
        /// </summary>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <param name="avoidRadius">避让半径，在遭遇障碍时将沿法向退回这个距离</param>
        /// <param name="layer">检查碰撞对象的层</param>
        /// <returns></returns>
        protected IEnumerable<Vector3> GetPassThroughRoute(Vector3 startPoint, Vector3 endPoint, float avoidRadius, LayerMask layer)
        {
            yield return Vector3.zero;
        }

        /// <summary>
        /// 使用给定的路径创建一段工具拟合的路径
        /// </summary>
        /// <param name="wayPoints">一段路径</param>
        public void BuildPaths(DrawWayPoints wayPoints)
        {
            new AStart2();
        }

        #endregion
    }
}
