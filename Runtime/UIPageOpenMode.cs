namespace VentiCola.UI
{
    /// <summary>
    /// <see cref="UIPage"/> 的加载模式
    /// </summary>
    public enum UIPageOpenMode
    {
        /// <summary>
        /// 隐藏之前的所有 UI，然后加载自己
        /// </summary>
        Single,
        /// <summary>
        /// 直接加载自己
        /// </summary>
        Additive
    }
}