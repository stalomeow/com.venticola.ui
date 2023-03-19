using System;
using Unity.Collections.LowLevel.Unsafe;

namespace VentiCola.UI.Internals
{
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
        /// <remarks>在转换过程中会尽可能避免装箱，除非目标类型是引用类型。</remarks>
        /// <typeparam name="V">原始类型，必须为值类型</typeparam>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T CastValueType<V, T>(V value) where V : struct
        {
            Type targetType = typeof(T);

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

        public static T CastAny<U, T>(U from)
        {
            Type srcType = typeof(U);
            Type targetType = typeof(T);

            // 通常情况下，srcType 和 targetType 都是一样的。这里优先处理，提高性能。
            if (targetType == srcType)
            {
                return UnsafeUtility.As<U, T>(ref from);
            }

            if (srcType.IsValueType && targetType.IsValueType && targetType.IsAssignableFrom(srcType))
            {
                // T is Nullable<U>
                var nullable = new DummyNullable<U>
                {
                    HasValue = true,
                    Value = from
                };
                return UnsafeUtility.As<DummyNullable<U>, T>(ref nullable);
            }

            return (T)(object)from;
        }
    }
}