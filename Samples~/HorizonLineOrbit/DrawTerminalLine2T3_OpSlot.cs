using Deconstruction.Manager;
using Deconstruction.Type.DMToolSlot;
using Deconstruction.Type.Linkable;
using UnityEngine;
using XericLibrary.Runtime.MacroLibrary;

namespace SesothoLine
{
    /// <summary>
    /// 绘制轨迹线的操作
    /// <code>
    /// 当从其他WaitForUserInput_OpSlot到达此处时，将创建TerminalLine2T3用于线路的绘制，
    /// 同时在线路的两端有两个TerminalPoint用于线路的相连。
    ///
    /// 支持常规的线路连续绘制，起点吸附，终点吸附。
    /// 不支持首尾相连。
    /// </code>
    /// </summary>
    public class DrawTerminalLine2T3_OpSlot : WaitForUserInput_OpSlot
    {
        #region 字段属性

        /// <summary>
        /// 上一步的坐标
        /// </summary>
        private Vector3 _lastPointPosition;

        /// <summary>
        /// 上一步的对齐轨迹计算器
        /// </summary>
        private Vector3 _lastPointNormal;

        /// <summary>
        /// 下一步的坐标
        /// </summary>
        private Vector3 _helperPosition = Vector3.zero;

        /// <summary>
        /// 下一步的法向
        /// </summary>
        private Vector3 _helperNormal = Vector3.zero;

        /// <summary>
        /// 操作的轨迹线
        /// </summary>
        private TerminalLine2T3 _line;

        /// <summary>
        /// 起点
        /// </summary>
        private TerminalPoint _startPoint;

        /// <summary>
        /// 操作的点，也是终点
        /// </summary>
        private TerminalPoint _endPoint;

        /// <summary>
        /// 在场上找到的其他的点
        /// </summary>
        private TerminalPoint _otherPoint;

        /// <summary>
        /// 起点是用上一步的终点替代的
        /// </summary>
        private bool _isStartPointSubstitute = false;

        /// <summary>
        /// 绘制时，终点是自由点
        /// </summary>
        private bool _isFreeEndPoint;

        // public SesothoArrangementWiresTool myWiresTool;
        // myWiresTool = targetTool as SesothoArrangementWiresTool;

        public static bool 判断是否在绘制区域内;
        #endregion

        #region 引用封装

        /// <summary>
        /// 创建自己的工具
        /// </summary>
        private SesothoArrangementWiresTool selfWiresTool => SesothoPeManager.WiresTool;

        #endregion

        #region 生命周期

        protected override void OnStart()
        {
            base.OnStart();

            // 检查抵达此处的流程是否正确
            if (!HasLastNode)
            {
                Debug.LogError(
                    $"绘制节点行为树顺序错误，必须从其他任意WaitForUserInput_OpSlot中抵达此处");
                RemoveThis();
                return;
            }

            if (Last is not WaitForUserInput_OpSlot)
            {
                Debug.LogError(
                    $"绘制节点行为树顺序错误，应该是 WaitForUserInput_OpSlot -> {nameof(DrawTerminalLine2T3_OpSlot)}，而不是{Last.GetType().Name}");
                RemoveThis();
                return;
            }

            // 设置先决条件
            InitPrecondition();
        }

        protected override void OnEnd()
        {
            base.OnEnd();
        }

        protected override void Update()
        {
            // 阻止基类的默认操作
            // base.Update();

            // 线的绘制计算，包括空间点吸附，链接等
            DrawingProcess();
            // 计算完毕后执行渲染，这里调用有可能会带来空引用问题
            // 但在此之前的阶段已经对此进行过了处理所以没有关系，如果要在其他地方照抄的话需要注意。
            _line.UpdateRender();

            // 下一步
            if (IsReleaseContinueKey())
            {
                _helperNormal = _line.TrajectoryCalculator.GetEndPointNormal.UpwardPlaneToVector3();

                // 起点链接
                if (_isStartPointSubstitute)
                {
                    // 在开始时已经做过转换了
                    // var dtlt_Slot = Last as DrawTerminalLine2T3_OpSlot;
                    _line.LinkedDataAddPolarity(_startPoint, LinkType.Import);
                }
                else
                {
                    _line.LinkedDataAddPolarity(_startPoint, LinkType.Import);
                    selfWiresTool.PersistentElement(_startPoint);
                    selfWiresTool.AddPeToNeighborGrid(_startPoint);
                }

                // 终点链接
                if (_otherPoint is null) // || _endPoint.gameObject.activeSelf 
                {
                    _line.LinkedDataAddPolarity(_endPoint, LinkType.Export);
                    selfWiresTool.PersistentElement(_endPoint);
                    selfWiresTool.AddPeToNeighborGrid(_endPoint);
                }
                // 如果存在吸附目标
                else
                {
                    selfWiresTool.DeleteElement(_endPoint);
                    _line.LinkedDataAddPolarity(_otherPoint, LinkType.Export);
                }

                // 保存线
                _line.ReapplyOrigin();
                selfWiresTool.PersistentElement(_line);
                selfWiresTool.AddBulkPeToNeighborGrid(_line);

                // 下一步
                ReplaceThis(new DrawTerminalLine2T3_OpSlot());
            }
            // 取消
            else if (IsReleaseCanelKey() || (!判断是否在绘制区域内))
            {
                RemoveThis();
                EndPrecondition();
            }
            // 判断按下ESC取消线段绘制 2024.8.8
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                RemoveThis();
                EndPrecondition();
            }
            // todo: 回到上一步
            /*
             * 操作栈的结构使得操作与对象的更新可以分离,
             * 就是撤回的过程可能有点复杂，
             * 需要分为工具运行期间撤回和常规撤回。
             */
        }



        protected override void AsyncBeforeUpdate()
        {
            base.AsyncBeforeUpdate();
        }

        protected override void AsyncUpdate()
        {
            base.AsyncUpdate();
        }

        protected override void AsyncAfterUpdate()
        {
            base.AsyncAfterUpdate();
        }

        #endregion

        #region 方法

        /// <summary>
        /// 设置先决属性
        /// </summary>
        private void InitPrecondition()
        {
            switch (Last)
            {
                // 使用来自前者的元素
                case DrawTerminalLine2T3_OpSlot dtlt_Slot:
                    _isStartPointSubstitute = true;
                    _startPoint = dtlt_Slot._endPoint;
                    _lastPointPosition = dtlt_Slot._helperPosition;
                    _lastPointNormal = dtlt_Slot._helperNormal;
                    break;
                // 新建，或从空间中继承
                case WaitForUserInput_OpSlot wfui_Slot:
                    if (SesothoArrangementWiresTool.GetNearestObjectAsPoint(out var terminalPoint))
                    {
                        _isStartPointSubstitute = true;
                        _startPoint = terminalPoint;
                        _lastPointPosition = terminalPoint.GetPosition3();
                        if (SesothoArrangementWiresTool.GetNearestObjectAsLine(terminalPoint, out var terminalLine,
                                out var normal))
                            _lastPointNormal = normal;
                        else
                        {
                            Debug.LogError("错误：当前绘制的线路准备吸附的目标并非一个完整的链路，或类型错误，正在退出。");
                            RemoveThis();
                        }
                    }
                    else
                    {
                        _isStartPointSubstitute = false;
                        selfWiresTool.GetElement(out _startPoint);
                        _lastPointPosition = SesothoArrangementWiresTool.OpKey_IgnoreNeighborAdsorption.Getkey() ?
                            wfui_Slot.MouseHelperPosition :
                            CameraMouseInputHelper.Inst.GetGridAdsorb(wfui_Slot.MouseHelperPosition);
                        _lastPointNormal = default;
                    }

                    break;
                default:
                    break;
            }

            selfWiresTool.GetElement(out _line);
            selfWiresTool.GetElement(out _endPoint);
        }

        /// <summary>
        /// 结束清理属性
        /// </summary>
        private void EndPrecondition()
        {
            selfWiresTool.DeleteElement(_line);
            // 不能把别人的东西给回收了
            if (!_isStartPointSubstitute)
                selfWiresTool.DeleteElement(_startPoint);
            selfWiresTool.DeleteElement(_endPoint);
        }

        /// <summary>
        /// 线路绘制过程
        /// </summary>
        private void DrawingProcess()
        {
            var forceStr = SesothoArrangementWiresTool.OpKey_ForceStr.Getkey();
            var forceStraightLine = SesothoArrangementWiresTool.OpKey_ForceStraightLine.Getkey();
            var force90Arc = SesothoArrangementWiresTool.OpKey_90Arc.Getkey();

            // 获取当前的坐标
            _helperPosition = PlaceElementManager.Inst.InputHelper.CurrentGridPosition;
            _startPoint.transform.position = _lastPointPosition;

            _isFreeEndPoint = true;
            // 如果吸附到任意物体，或者用按键忽略这一过程
            if (SesothoPeManager.NearestObject is not null &&
                SesothoArrangementWiresTool.GetNearestObjectAsPoint(out _otherPoint) &&
                !SesothoArrangementWiresTool.OpKey_IgnoreNeighborAdsorption.Getkey())
            {
                _isFreeEndPoint = false;
                if (_endPoint.gameObject.activeSelf)
                    _endPoint.gameObject.SetActive(false);

                _helperPosition = SesothoPeManager.NearestPoint;
                _endPoint.transform.position = SesothoPeManager.NearestPoint;
                if (SesothoArrangementWiresTool.GetNearestObjectAsLine(_otherPoint, out var terminalLine,
                        out var normal))
                    _helperNormal = normal;
            }
            // 如果吸附到任意切线
            // else if (NearestLineTangentObject != null)
            // {   
            //     NearestLineTangentPoint
            // }

            // 否则自由点
            if (_isFreeEndPoint)
            {
                if (!_endPoint.gameObject.activeSelf)
                    _endPoint.gameObject.SetActive(true);
                _otherPoint = null;
                _helperNormal = default;
                _endPoint.transform.position = _helperPosition;
            }

            if (forceStr)
            {
                Vector3 startPos = _startPoint.GetPosition3();
                Vector3 endPoint = _endPoint.GetPosition3();
                if ((int)startPos.x != (int)endPoint.x && (int)startPos.z != (int)endPoint.z)
                {
                    Vector3 vector3 = endPoint - startPos;
                    if (Mathf.Abs(vector3.x) > Mathf.Abs(vector3.z))
                    {
                        Vector3 vector = new Vector3(endPoint.x, endPoint.y, startPos.z);
                        _endPoint.SetPosition3(vector);
                    }
                    else if (Mathf.Abs(vector3.x) < Mathf.Abs(vector3.z))
                    {
                        Vector3 vector = new Vector3(startPos.x, endPoint.y, endPoint.z);
                        _endPoint.SetPosition3(vector);
                    }
                    _helperPosition = _endPoint.GetPosition3();
                    _line.SetLineSegment(
                        _lastPointPosition, _helperPosition,
                        _lastPointNormal, _helperNormal
                    );
                }
            }
            else if (force90Arc)
            {
                _helperPosition = _line.GetEndPointWorldPosition();
            }


            _line.SetLineSegment(
                _lastPointPosition, _helperPosition,
                _lastPointNormal, _helperNormal
            );

            // 如果当前线的线计算器是圆弧计算器，那么获取它的角度
            // if (_line.NowCalculator is RoundTrajectory2 arc)
            // {
            //     Debug.Log($"角度{arc.Angle} {arc.AngleExact}");
            // }

            if (forceStr || forceStraightLine)
            {
                _line.DrawStraightLine();
            }
            else
            {
                var force180Arc = SesothoArrangementWiresTool.OpKey_180Arc.Getkey();
                var forceRevCisoid = SesothoArrangementWiresTool.OpKey_RevCisoid.Getkey();

                var angle = -1;
                if (force90Arc || force180Arc)
                {
                    angle = 0;
                    if (force90Arc)
                    {
                        angle += 90;

                    }
                    if (force180Arc)
                        angle += 180;
                }

                _line.AutomaticDrawCircularLine(angle, forceRevCisoid);
            }
        }

        /// <summary>
        /// 获取光标下的起点
        /// </summary>
        /// <returns></returns>
        private bool GetCurrentObject()
        {
            // SesothoPeManager.
            return false;
        }

        #endregion
    }
}