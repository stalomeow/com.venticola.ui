namespace VentiCola.UI
{
    /// <summary>
    /// 表示 UI 页面的状态
    /// </summary>
    public enum UIPageStatus
    {
        Inactive = 0,
        Active = 1,
        /// <summary>
        /// 表示页面正在做状态过渡
        /// </summary>
        Transiting = 3
    }
}