namespace VentiCola.UI
{
    /// <summary>
    /// UI 的状态
    /// </summary>
    public enum UIState
    {
        /// <summary>
        /// 已关闭
        /// </summary>
        Closed = 0,

        /// <summary>
        /// 活跃状态（位于最顶层）
        /// </summary>
        Active = 1,

        /// <summary>
        /// 暂停状态
        /// </summary>
        Paused = 2,

        /// <summary>
        /// 正在关闭
        /// </summary>
        Closing = 3,
    }
}