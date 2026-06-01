using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LRC
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigTypeOverrideAttribute : PropertyAttribute
    {
        public readonly string TypeName;

        public ConfigTypeOverrideAttribute(string name)
        {
            TypeName = name;
        }

        /// <summary>
        /// 尝试获取其中的rename定义名称
        /// </summary>
        /// <param name="field"></param>
        /// <param name="name">通过特性获取的名称，如果没有则为原生的名称</param>
        /// <returns>是否成功通过特性获取名称，否则是普通类型名称</returns>
        public static bool TryGetMemberName(FieldInfo field, out string name)
        {
            if (field.GetCustomAttribute(typeof(ConfigTypeOverrideAttribute)) is ConfigTypeOverrideAttribute rename)
            {
                name = rename.TypeName;
                return true;
            }

            name = field.FieldType.Name;
            return false;
        }

    }
}