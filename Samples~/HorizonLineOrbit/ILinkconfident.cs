using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SesothoLine
{
    /// <summary>
    /// 可链接的对象，表示此成员内包含一个链表的节点，可用于链表跟踪
    /// </summary>
    public interface ILinkconfident<T>
    {
        /// <summary>
        /// 获取链接点
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool GetLinkedNode(out LinkedListNode<T> node);
        /// <summary>
        /// 设置连接点
        /// </summary>
        /// <param name="node"></param>
        public void SetLinkedNode(LinkedListNode<T> node);
    }
}
