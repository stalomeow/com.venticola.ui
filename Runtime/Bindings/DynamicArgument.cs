using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using VentiCola.UI.Internals;
using Object = UnityEngine.Object;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public class DynamicArgument
    {
        internal enum ArgumentType
        {
            /// <summary>
            /// 表示默认值 <c>default(T)</c>
            /// </summary>
            Default = 0,
            /// <summary>
            /// 表示 <see cref="Object"/>
            /// </summary>
            UnityObj = 1,
            /// <summary>
            /// 表示 <see cref="int"/>
            /// </summary>
            Int32 = 2,
            /// <summary>
            /// 表示 <see cref="float"/>
            /// </summary>
            Float32 = 3,
            /// <summary>
            /// 表示 <see cref="string"/>
            /// </summary>
            String = 4,
            /// <summary>
            /// 表示 <c>true</c>
            /// </summary>
            True = 5,
            /// <summary>
            /// 表示 <c>false</c>
            /// </summary>
            False = 6,
            /// <summary>
            /// 表示 <see cref="PropertyProxy"/>
            /// </summary>
            Property = 7
        }


        [SerializeField] private ArgumentType m_ArgType = ArgumentType.Default;
        [SerializeField] private PropertyProxy m_PropertyArg;
        [SerializeField] private Object m_UnityObjArg;
        [SerializeField] private string m_StringArg;
        [SerializeField] private int m_NumberArg;


        protected internal DynamicArgument() { }


        /// <summary>
        /// 指示该参数的值是否为任意类型的默认值
        /// </summary>
        public bool IsAnyDefaultValue
        {
            get => m_ArgType == ArgumentType.Default;
        }

        /// <summary>
        /// 指示该参数的值是否为常量
        /// </summary>
        public bool IsConstValue
        {
            get => m_ArgType != ArgumentType.Property;
        }

        /// <summary>
        /// 获取参数的类型
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public Type ArgType
        {
            get => m_ArgType switch
            {
                // 默认值的类型是任意的
                ArgumentType.Default => throw new InvalidOperationException(
                    $"The type of default {nameof(DynamicArgument)} is <Any>! You should use {nameof(IsAnyDefaultValue)}/{nameof(IsAssignableTo)} instead."),

#pragma warning disable UNT0008 // Null propagation on Unity objects

                // 这里不能用 Unity 重载的运算符判断 null，我只是单纯想知道 C# 对象是否为 null
                ArgumentType.UnityObj => m_UnityObjArg?.GetType() ?? typeof(Object),

#pragma warning restore UNT0008 // Null propagation on Unity objects

                ArgumentType.Int32 => typeof(int),
                ArgumentType.Float32 => typeof(float),
                ArgumentType.String => typeof(string),
                ArgumentType.True => typeof(bool),
                ArgumentType.False => typeof(bool),
                ArgumentType.Property => m_PropertyArg.PropertyType,

                _ => throw new NotSupportedException($"Unsupported Argument Type: {m_ArgType}!")
            };
        }

        public bool IsAssignableTo<T>()
        {
            return IsAssignableTo(typeof(T));
        }

        public bool IsAssignableTo(Type type)
        {
            if (m_ArgType == ArgumentType.Default)
            {
                return true;
            }

            return type.IsAssignableFrom(ArgType);
        }

        public T GetValue<T>() => m_ArgType switch
        {
            ArgumentType.Default => default,
            ArgumentType.UnityObj => (T)(object)m_UnityObjArg,
            ArgumentType.Int32 => CastUtility.CastValueType<int, T>(m_NumberArg),
            ArgumentType.Float32 => CastUtility.CastValueType<float, T>(UnsafeUtility.As<int, float>(ref m_NumberArg)),
            ArgumentType.String => (T)(object)m_StringArg,
            ArgumentType.True => CastUtility.CastValueType<bool, T>(true),
            ArgumentType.False => CastUtility.CastValueType<bool, T>(false),
            ArgumentType.Property => m_PropertyArg.GetValue<T>(),
            _ => throw new NotSupportedException($"Unsupported Argument Type: {m_ArgType}!")
        };

        public static bool CheckArgumentTypes(DynamicArgument[] args, params Type[] types)
        {
            if (args.Length != types.Length)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].IsAssignableTo(types[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}