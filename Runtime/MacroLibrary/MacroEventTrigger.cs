using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using XericLibrary.Runtime.MacroLibrary;

namespace Deconstruction.Runtime
{
    public static class MacroEventTrigger
    {
        #region 基本事件

        /// <summary>
        /// 添加事件
        /// </summary>
        /// <param name="eventTrigger"></param>
        /// <param name="eventType"></param>
        /// <param name="entryEvent"></param>
        public static void AddEntryEvent(this EventTrigger eventTrigger, EventTriggerType eventType,
            UnityAction<BaseEventData> entryEvent)
        {
            var e = new EventTrigger.TriggerEvent();
            e.AddListener(entryEvent);
            var eventData = new EventTrigger.Entry()
            {
                eventID = eventType,
                callback = e,
            };
            eventTrigger.triggers.Add(eventData);
        }

        /// <summary>
        /// 移除类别下的所有事件
        /// </summary>
        /// <param name="eventTrigger"></param>
        /// <param name="eventType"></param>
        public static void RemoveEntryEvent(this EventTrigger eventTrigger, EventTriggerType eventType)
        {
            foreach (var trigger in eventTrigger.triggers
                         .Where(a => a.eventID == eventType))
            {
                eventTrigger.triggers.Remove(trigger);
            }
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="eventTrigger"></param>
        /// <param name="entryEvent"></param>
        public static void RemoveEntryEvent(this EventTrigger eventTrigger, UnityAction<BaseEventData> entryEvent)
        {
            // 不知道，所以全部欧拉一遍
            eventTrigger.triggers
                .ForEachDo(a => a.callback.RemoveListener(entryEvent));
        }

        /// <summary>
        /// 清除所有事件
        /// </summary>
        /// <param name="eventTrigger"></param>
        public static void RemoveAllEntryEvent(this EventTrigger eventTrigger)
        {
            eventTrigger.triggers.Clear();
        }

        /// <summary>
        /// 尝试返回光标事件下的碰撞结果
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool TryGetPointerEventData(this BaseEventData data, out RaycastResult result)
        {
            if (data is PointerEventData pointer)
            {
                result = pointer.pointerCurrentRaycast;
                return true;
            }
            result = default;
            return false;
        }
        
        #endregion

        #region 细节事件

        public static EventTrigger.TriggerEvent onPointerEnter(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.PointerEnter);

        public static EventTrigger.TriggerEvent onPointerExit(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.PointerExit);

        public static EventTrigger.TriggerEvent onPointerDown(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.PointerDown);

        public static EventTrigger.TriggerEvent onPointerUp(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.PointerUp);

        public static EventTrigger.TriggerEvent onPointerClick(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.PointerClick);

        public static EventTrigger.TriggerEvent onDrag(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Drag);

        public static EventTrigger.TriggerEvent onDrop(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Drop);

        public static EventTrigger.TriggerEvent onScroll(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Scroll);

        public static EventTrigger.TriggerEvent onUpdateSelected(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.UpdateSelected);

        public static EventTrigger.TriggerEvent onSelect(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Select);

        public static EventTrigger.TriggerEvent onDeselect(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Deselect);

        public static EventTrigger.TriggerEvent onMove(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Move);

        public static EventTrigger.TriggerEvent onInitializePotentialDrag(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.InitializePotentialDrag);

        public static EventTrigger.TriggerEvent onBeginDrag(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.BeginDrag);

        public static EventTrigger.TriggerEvent onEndDrag(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.EndDrag);

        /// <summary>
        /// 用户提交输入时触发（比如回车）
        /// </summary>
        /// <param name="eventTrigger"></param>
        /// <returns></returns>
        public static EventTrigger.TriggerEvent onSubmit(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Submit);

        public static EventTrigger.TriggerEvent onCancel(this EventTrigger eventTrigger)
            => eventTrigger.GetEventTrigger(EventTriggerType.Cancel);

        private static EventTrigger.TriggerEvent GetEventTrigger(this EventTrigger eventTrigger, EventTriggerType type)
        {
            EventTrigger.TriggerEvent e = null;
            var entry = eventTrigger.triggers.FirstOrDefault(a => a.eventID == type);
            if (entry == null)
            {
                e = new EventTrigger.TriggerEvent();
                entry = new EventTrigger.Entry()
                {
                    eventID = type,
                    callback = e,
                };
                eventTrigger.triggers.Add(entry);
            }
            else
                e = entry.callback;

            return e;
        }

        public static void ClearAllEventTrigger(this EventTrigger eventTrigger) => eventTrigger.triggers.Clear();

        #endregion
    }
}