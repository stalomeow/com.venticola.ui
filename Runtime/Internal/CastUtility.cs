using Unity.Collections.LowLevel.Unsafe;

namespace VentiCola.UI.Internal
{
    /// <summary>
    /// 提供用于强制类型转换的辅助方法
    /// </summary>
    public static class CastUtility
    {
        private struct DummyNullable<T>
        {
            public bool HasValue;
            public T Value;
        }

        /// <summary>
        /// 将 <typeparamref name="V"/> 类型的值转换为 <typeparamref name="T"/> 类型。
        /// </summary>
        /// <param name="value">原始的值</param>
        /// <typeparam name="V">原始类型，必须为值类型</typeparam>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>转换后的对象或值</returns>
        /// <remarks>在转换过程中会尽可能避免装箱，除非目标类型是引用类型。</remarks>
        public static T CastValueType<V, T>(V value) where V : struct
        {
            var targetType = typeof(T);

            if (targetType == typeof(V))
            {
                return UnsafeUtility.As<V, T>(ref value);
            }

            if (targetType == typeof(V?))
            {
                V? nullable = value;
                return UnsafeUtility.As<V?, T>(ref nullable);
            }

            // box is inevitable here!
            return (T)(object)value;
        }

        /// <summary>
        /// 将 <typeparamref name="U"/> 类型的对象或值转换为 <typeparamref name="T"/> 类型。
        /// </summary>
        /// <param name="from">原始的对象或值</param>
        /// <typeparam name="U">原始类型，没有限制</typeparam>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>转换后的对象或值</returns>
        /// <remarks>
        /// 如果已知 <typeparamref name="U"/> 是值类型，请考虑使用 <see cref="CastValueType{V,T}"/>，它有更好的性能。
        /// 如果已知 <typeparamref name="U"/> 是引用类型，请考虑直接强制类型转换。
        /// </remarks>
        public static T CastAny<U, T>(U from)
        {
            var srcType = typeof(U);
            var targetType = typeof(T);

            // 通常情况下，srcType 和 targetType 都是一样的。这里优先处理，提高性能。
            if (targetType == srcType)
            {
                return UnsafeUtility.As<U, T>(ref from);
            }

            // 检查 T 是否为 Nullable<U>
            if (srcType.IsValueType && targetType.IsValueType && targetType.IsAssignableFrom(srcType))
            {
                var nullable = new DummyNullable<U>
                {
                    HasValue = true,
                    Value = from
                };
                return UnsafeUtility.As<DummyNullable<U>, T>(ref nullable);
            }

            // box is inevitable here!
            return (T)(object)from;
        }
    }
}