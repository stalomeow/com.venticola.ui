using System;
using UnityEngine;

namespace VentiColaEditor.UI.CodeInjection
{
    /// <summary>
    /// 表示 Code Injection 的日志输出级别。
    /// </summary>
    [Flags]
    public enum LogLevel
    {
        /// <summary>
        /// 不输出日志。
        /// </summary>
        [InspectorName("Nothing")]
        None = 0,
        /// <summary>
        /// 输出程序集信息。
        /// </summary>
        [InspectorName("Assembly")]
        Assembly = 1 << 0,
        /// <summary>
        /// 输出类型信息。
        /// </summary>
        [InspectorName("Type")]
        Type = 1 << 1,
        /// <summary>
        /// 输出属性信息。
        /// </summary>
        [InspectorName("Property")]
        Property = 1 << 2,
        /// <summary>
        /// 输出方法信息。
        /// </summary>
        [InspectorName("Method")]
        Method = 1 << 3,
        /// <summary>
        /// 输出所有信息。
        /// </summary>
        [InspectorName("Everything")]
        All = Assembly | Type | Property | Method
    }
}