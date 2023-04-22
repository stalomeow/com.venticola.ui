using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VentiColaEditor.UI.CodeInjection
{
    public static class MetaDataUtility
    {
        public delegate void ForEachAction<T>(int index, int count, T item);

        public static void ForEachAllTypesIncludeNested(AssemblyDefinition assembly, ForEachAction<TypeDefinition> action)
        {
            var queue = new Queue<TypeDefinition>();
            var allTypes = new List<TypeDefinition>();

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

        public static TypeReference ImportTypeReference(ModuleDefinition module, Type type, params TypeReference[] genericArguments)
        {
            TypeReference importedType = module.ImportReference(type);

            if (genericArguments.Length == 0)
            {
                return importedType;
            }

            // 泛型
            var instanceType = new GenericInstanceType(importedType);

            for (int i = 0; i < genericArguments.Length; i++)
            {
                instanceType.GenericArguments.Add(genericArguments[i]);
            }

            return instanceType;
        }

        public static MethodReference MakeGenericInstanceMethod(MethodReference importedMethod, params TypeReference[] genericArguments)
        {
            if (genericArguments.Length == 0)
            {
                return importedMethod;
            }

            // 泛型
            var instanceMethod = new GenericInstanceMethod(importedMethod);

            for (int i = 0; i < genericArguments.Length; i++)
            {
                instanceMethod.GenericArguments.Add(genericArguments[i]);
            }

            return instanceMethod;
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