using System;
using System.Text;

namespace VentiCola.UI.Internals
{
    internal static class TypeUtility
    {
        [ThreadStatic] private static StringBuilder s_StrBuilder;

        public static bool IsDerivedFromSpecificGenericType(Type type, Type genericTypeDefinition, out Type[] genericTypeArguments)
        {
            if (type.IsGenericType && !type.IsConstructedGenericType)
            {
                throw new ArgumentException("The type is not a closed type.", nameof(type));
            }

            if (!genericTypeDefinition.IsGenericTypeDefinition)
            {
                throw new ArgumentException("The type is not a generic type definition.", nameof(genericTypeDefinition));
            }

            if (genericTypeDefinition.IsInterface)
            {
                foreach (Type i in type.GetInterfaces())
                {
                    if (MatchGenericTypeDefinition(i, genericTypeDefinition))
                    {
                        genericTypeArguments = i.GenericTypeArguments;
                        return true;
                    }
                }
            }
            else
            {
                while (type != null)
                {
                    if (MatchGenericTypeDefinition(type,genericTypeDefinition))
                    {
                        genericTypeArguments = type.GenericTypeArguments;
                        return true;
                    }

                    type = type.BaseType;
                }
            }

            genericTypeArguments = Array.Empty<Type>();
            return false;
        }

        private static bool MatchGenericTypeDefinition(Type type, Type genericTypeDefinition)
        {
            return type.IsConstructedGenericType && (type.GetGenericTypeDefinition() == genericTypeDefinition);
        }

        public static string GetFriendlyTypeName(Type type, bool fullName)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return GetTypeNameImpl(type, fullName);
        }

        private static string GetTypeNameDirect(Type type, bool fullName)
        {
            return fullName ? type.FullName.Replace('+', '.') : type.Name;
        }

        private static string GetNonGenericTypeName(Type type, bool fullName)
        {
            if (type == typeof(object))
            {
                return "object";
            }

            if (type == typeof(void))
            {
                return "void";
            }

            if (type.IsEnum)
            {
                // Enum 的 TypeCode 是它下属类型的 TypeCode，如 Int32
                return GetTypeNameDirect(type, fullName);
            }

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => "bool",
                TypeCode.Byte => "byte",
                TypeCode.Char => "char",
                TypeCode.Decimal => "decimal",
                TypeCode.Double => "double",
                TypeCode.Int16 => "short",
                TypeCode.Int32 => "int",
                TypeCode.Int64 => "long",
                TypeCode.SByte => "sbyte",
                TypeCode.Single => "float",
                TypeCode.String => "string",
                TypeCode.UInt16 => "ushort",
                TypeCode.UInt32 => "uint",
                TypeCode.UInt64 => "ulong",
                _ => GetTypeNameDirect(type, fullName)
            };
        }

        private static string GetTypeNameImpl(Type type, bool fullName)
        {
            if (!type.IsGenericType)
            {
                return GetNonGenericTypeName(type, fullName);
            }

            s_StrBuilder ??= new StringBuilder();
            AppendTypeNameToStrBuilder(type, fullName);

            string result = s_StrBuilder.ToString();
            s_StrBuilder.Clear();
            return result;
        }

        private static void AppendTypeNameToStrBuilder(Type type, bool fullName)
        {
            if (type.IsGenericType)
            {
                Type[] arguments = type.GetGenericArguments();

                string typeName = GetTypeNameDirect(type, fullName);
                int startIndex = typeName.LastIndexOf('`');
                s_StrBuilder.Append(typeName[..startIndex]);
                s_StrBuilder.Append('<');

                for (int i = 0; i < arguments.Length; i++)
                {
                    if (i > 0)
                    {
                        s_StrBuilder.Append(", ");
                    }

                    if (arguments[i].IsGenericTypeParameter)
                    {
                        continue;
                    }

                    AppendTypeNameToStrBuilder(arguments[i], fullName);
                }

                s_StrBuilder.Append('>');
            }
            else
            {
                s_StrBuilder.Append(GetNonGenericTypeName(type, fullName));
            }
        }
    }
}