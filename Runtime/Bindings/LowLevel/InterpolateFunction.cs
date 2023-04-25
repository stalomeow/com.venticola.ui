namespace VentiCola.UI.Bindings.LowLevel
{
    /// <summary>
    /// （线性）插值方法
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="from">起始值</param>
    /// <param name="to">目标值</param>
    /// <param name="progress">进度</param>
    /// <returns>插值结果</returns>
    public delegate T InterpolateFunction<T>(T from, T to, float progress);
}