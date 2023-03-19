using System;
using UnityEngine;

namespace VentiCola.UI
{
    /// <summary>
    /// UI 的标记
    /// </summary>
    [Flags]
    public enum UIPageFlags
    {
        /// <summary>
        /// 无
        /// </summary>
        [InspectorName("Nothing")]
        None = 0,
        /// <summary>
        /// 经常被使用
        /// </summary>
        /// <remarks>具有此标记的 UI 在第一次被加载后会被缓存</remarks>
        [InspectorName("Frequently Used")]
        FrequentlyUsed = 1 << 0,
        /// <summary>
        /// 全屏页面
        /// </summary>
        /// <remarks>具有此标记的 UI 在打开后会关闭后面所有 UI 的渲染</remarks>
        [InspectorName("Full Screen")]
        FullScreen = 1 << 1,
        /// <summary>
        /// 模糊背景
        /// </summary>
        /// <remarks>具有此标记的 UI 在打开后会关闭后面所有 UI 的渲染</remarks>
        [InspectorName("Blur Background")]
        BlurBackground = 1 << 2
    }
}