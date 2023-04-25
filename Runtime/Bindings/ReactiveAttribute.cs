using System;

namespace VentiCola.UI.Bindings
{
    /// <summary>
    /// 使得被标记的属性成为响应式属性
    /// </summary>
    /// <remarks>响应式属性：自动记录读取过该属性的对象。之后，当属性的值发生变化时，会反过来通知之前被记录的对象</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReactiveAttribute : Attribute
    {
        /// <summary>
        /// 用于判断新旧属性值是否相等时使用的 <see cref="System.Collections.Generic.IEqualityComparer{T}"/> 对象的类型
        /// </summary>
        /// <remarks>
        /// 该类型必须为具有公共构造方法的可实例化引用类型（非抽象、非开放类型、非泛型定义、......），
        /// 并且实现了 <see cref="System.Collections.Generic.IEqualityComparer{T}"/>，其中的类型参数是被标记属性的返回值类型。
        /// 如果传入类型不满足以上要求，在运行时可能会出现一些意想不到的错误
        /// </remarks>
        public Type EqualityComparer { get; set; }

        /// <summary>
        /// 是否将被标记属性转换成惰性求值的计算属性
        /// </summary>
        /// <remarks>该参数要求被标记属性有一个手动实现的 getter，但对 setter 没有要求</remarks>
        public bool LazyComputed { get; set; }
    }
}