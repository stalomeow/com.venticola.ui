using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VentiColaEditor.UI.CodeInjection
{
    internal static class MetaDataUtility
    {
        public static void ForEachAllTypes(AssemblyDefinition assembly, Action<int, int, TypeDefinition> action)
        {
            var queue = new Queue<TypeDefinition>();
            var allTypes = new List<TypeDefinition>();

            // find types including nested types
            foreach (TypeDefinition topLevelType in assembly.MainModule.Types)
            {
                queue.Enqueue(topLevelType);

                while (queue.TryDequeue(out TypeDefinition type))
                {
                    allTypes.Add(type);

                    if (type.HasNestedTypes)
                    {
                        foreach (TypeDefinition nested in type.NestedTypes)
                        {
                            queue.Enqueue(nested);
                        }
                    }
                }
            }

            for (int i = 0; i < allTypes.Count; i++)
            {
                action(i, allTypes.Count, allTypes[i]);
            }
        }

        public static TypeReference ImportTypeReference(ModuleDefinition module, Type type, params TypeReference[] typeArguments)
        {
            var importedType = module.ImportReference(type);

            if (typeArguments.Length == 0)
            {
                return importedType;
            }

            // 泛型
            var instanceType = new GenericInstanceType(importedType);

            for (int i = 0; i < typeArguments.Length; i++)
            {
                instanceType.GenericArguments.Add(typeArguments[i]);
            }

            return instanceType;
        }

        public static bool IsSubclassOf<T>(TypeDefinition type) where T : class
        {
            bool isSuperType = false;

            while (type != null)
            {
                if (type.FullName == typeof(T).FullName)
                {
                    // 如果一开始的 type 和 T 一样，也返回 false
                    return isSuperType;
                }

                // Note: '<Module>' 和 object 两个类没有基类！
                type = type.BaseType?.Resolve();
                isSuperType = true;
            }

            return false;
        }

        /// <summary>
        /// 在 <paramref name="type"/> 的继承链上往上遍历，直到类型 <typeparamref name="T"/> 或者没有基类。
        /// 遍历时包含 <paramref name="type"/> 和 <typeparamref name="T"/>。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public static void WalkUpTypesUntil<T>(TypeDefinition type, Action<TypeDefinition> callback) where T : class
        {
            while (type != null)
            {
                callback(type);

                if (type.FullName == typeof(T).FullName)
                {
                    break;
                }

                // Note: '<Module>' 和 object 两个类没有基类！
                type = type.BaseType?.Resolve();
            }
        }

        public static bool HasCustomAttribute<T>(Collection<CustomAttribute> attributes) where T : Attribute
        {
            return HasCustomAttribute<T>(attributes, out _);
        }

        public static bool HasCustomAttribute<T>(Collection<CustomAttribute> attributes, out CustomAttribute attribute) where T : Attribute
        {
            foreach (CustomAttribute attr in attributes)
            {
                if (attr.AttributeType.FullName == typeof(T).FullName)
                {
                    attribute = attr;
                    return true;
                }
            }

            attribute = null;
            return false;
        }

        public static void RemoveCustomAttributes(Collection<CustomAttribute> attributes, Type attributeType)
        {
            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                if (attributes[i].AttributeType.FullName == attributeType.FullName)
                {
                    attributes.RemoveAt(i);
                }
            }
        }

        public static bool IsCompilerGeneratedMethod(MethodDefinition method)
        {
            return HasCustomAttribute<CompilerGeneratedAttribute>(method.CustomAttributes);
        }

        public static void AddCompilerGeneratedAttribute(ModuleDefinition module, Collection<CustomAttribute> attributes)
        {
            var type = new TypeReference("System.Runtime.CompilerServices", "CompilerGeneratedAttribute",
                module, module.TypeSystem.CoreLibrary, false);
            var ctor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
            attributes.Add(new CustomAttribute(ctor));
        }
    }
}