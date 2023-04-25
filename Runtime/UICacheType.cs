namespace VentiCola.UI
{
    /// <summary>
    /// UI 的缓存类型
    /// </summary>
    public enum UICacheType
    {
        /// <summary>
        /// 不缓存
        /// </summary>
        Never = -1,

        /// <summary>
        /// 只缓存一个
        /// </summary>
        One = 0,

        /// <summary>
        /// 缓存在全局的 LRU Cache 中
        /// </summary>
        LRU = 1
    }
}