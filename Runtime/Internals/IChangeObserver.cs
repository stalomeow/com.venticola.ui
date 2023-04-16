namespace VentiCola.UI.Internals
{
    /// <summary>
    /// 用于监听某种“变化”
    /// </summary>
    public interface IChangeObserver
    {
        /// <summary>
        /// *Internal Use
        /// </summary>
        ref ChangeObserverPlugin Plugin { get; }

        /// <summary>
        /// 表示该对象是否只被动监听变化
        /// </summary>
        bool IsPassive { get; }

        /// <summary>
        /// 通知该对象“某种变化发生了”
        /// </summary>
        void NotifyChanged();
    }
}