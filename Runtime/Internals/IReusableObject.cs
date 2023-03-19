namespace VentiCola.UI.Internals
{
    /// <summary>
    /// 表示一个可重用对象
    /// </summary>
    public interface IReusableObject
    {
        /// <summary>
        /// 表示该对象的版本。
        /// </summary>
        int Version { get; }

        /// <summary>
        /// 重置该对象。
        /// </summary>
        void ResetObject();
    }
}