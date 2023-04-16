namespace VentiCola.UI.Internals
{
    /// <summary>
    /// 表示一个有版本的对象
    /// </summary>
    public interface IVersionable
    {
        /// <summary>
        /// 表示该对象的版本
        /// </summary>
        int Version { get; }
    }
}