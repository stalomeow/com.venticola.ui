using System;

namespace VentiCola.UI.Internals
{
    /// <summary>
    /// 表示被标记的程序集已经完成了代码注入。
    /// </summary>
    /// <remarks>请不要随意使用该 Attribute。</remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class VentiColaUICodesInjectedAttribute : Attribute { }
}