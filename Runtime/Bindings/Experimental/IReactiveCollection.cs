using System;

namespace VentiCola.UI.Bindings.Experimental
{
    /// <summary>
    /// 表示一个响应式的集合
    /// </summary>
    public interface IReactiveCollection
    {
        int Count { get; }

        T GetKeyAt<T>(Index index);

        T GetValueAt<T>(Index index);

        void CopyTo(ref IReactiveCollection destination, Range range);
    }
}