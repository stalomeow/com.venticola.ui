using System;
using System.Collections.Generic;
using System.Reflection;

namespace VentiCola.UI.Internals
{
    internal static class PropertyReflectionUtility
    {
        public const BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.Instance;

        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> s_Cache = new();

        public static bool IsPublicReadableProperty(PropertyInfo property)
        {
            return property.GetGetMethod(false) != null;
        }

        public static Dictionary<string, PropertyInfo> CollectPublicReadableProperties(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(PropertyBindingFlags);
            var methods = new Dictionary<string, PropertyInfo>(properties.Length);

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];

                if (IsPublicReadableProperty(property))
                {
                    methods.Add(property.Name, property);
                }
            }

            return methods;
        }

        private static bool TryGetPropertyInfo(Type type, string propertyName, out PropertyInfo property)
        {
            if (!s_Cache.TryGetValue(type, out Dictionary<string, PropertyInfo> allProps))
            {
                // should be thread-safe here.
                lock (s_Cache)
                {
                    if (!s_Cache.TryGetValue(type, out allProps))
                    {
                        allProps = CollectPublicReadableProperties(type);
                        s_Cache.Add(type, allProps);
                    }
                }
            }

            return allProps.TryGetValue(propertyName, out property);
        }

        public static bool TryGetPropertyPublicGetMethod(Type type, string propertyName, out MethodInfo method)
        {
            if (TryGetPropertyInfo(type, propertyName, out PropertyInfo prop))
            {
                method = prop.GetMethod;
                return true;
            }

            method = default;
            return false;
        }

        public static bool TryGetPropertyPublicSetMethod(Type type, string propertyName, out MethodInfo method)
        {
            if (TryGetPropertyInfo(type, propertyName, out PropertyInfo prop))
            {
                method = prop.GetSetMethod(false);
                return (method is not null);
            }

            method = default;
            return false;
        }
    }
}