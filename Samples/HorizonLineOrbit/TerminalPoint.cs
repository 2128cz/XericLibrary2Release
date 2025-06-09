using System;
using System.Collections;
using System.Collections.Generic;
using Deconstruction.Element;
using UnityEngine;
using XericLibrary.Runtime.Debuger;
using XericLibrary.Runtime.MacroLibrary;
using Random = System.Random;

namespace SesothoLine
{
    /// <summary>
    /// 点，承担连接的责任
    /// </summary>
    public class TerminalPoint : Deconstruction.Element.ContactPoint, 
        ILinkconfidentPe    // 链表跟踪
    {
        private LinkedListNode<PlacementBase> _linkNode;

        #region 生命周期
        
        // 调试绘制一下位置
        // private void Update()
        // {
        //     MacroDebugDraw.DrawDownArrow(transform.position + MacroMath.RandomVector3(0.1f, Identifier), Color.green);
        // }

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
