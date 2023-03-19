using Mono.Cecil;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internals;

namespace VentiColaEditor.UI.CodeInjection
{
    internal class MethodReferenceCache
    {
        private readonly ModuleDefinition m_Module;
        private MethodReference m_StringHashMethod;
        private MethodReference m_StringEqualityOperator;
        private MethodReference m_MissingPublicPropertyExceptionConstructor;
        private MethodReference m_MissingPublicMethodExceptionConstructor;
        private MethodReference m_CastValueTypeMethod;
        private MethodReference m_CastAnyMethod;
        private MethodReference m_DataModelGetMethod;
        private MethodReference m_DataModelSetMethod;
        private MethodReference m_DataModelGetPropertyMethod;
        private MethodReference m_DataModelSetPropertyMethod;
        private MethodReference m_DynamicArgumentGetValueMethod;
        private MethodReference m_UIPageVoidInvokeMethod;
        private MethodReference m_UIPageTInvokeMethod;

        public MethodReferenceCache(ModuleDefinition module)
        {
            m_Module = module;
        }

        public MethodReference StringHashMethod
        {
            get
            {
                if (m_StringHashMethod is null)
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                    var method = typeof(StringHashUtility).GetMethod(nameof(StringHashUtility.ComputeStringHash), flags);
                    m_StringHashMethod = m_Module.ImportReference(method);
                }

                return m_StringHashMethod;
            }
        }

        public MethodReference StringEqualityOperator
        {
            get
            {
                if (m_StringEqualityOperator is null)
                {
                    var typeSystem = m_Module.TypeSystem;
                    m_StringEqualityOperator = new("op_Equality", typeSystem.Boolean, typeSystem.String)
                    {
                        HasThis = false,
                        Parameters = { new(typeSystem.String), new(typeSystem.String) }
                    };
                }

                return m_StringEqualityOperator;
            }
        }

        public MethodReference MissingPublicPropertyExceptionConstructor
        {
            get
            {
                if (m_MissingPublicPropertyExceptionConstructor is null)
                {
                    var ctor = typeof(MissingPublicPropertyException).GetConstructor(new Type[] { typeof(string) });
                    m_MissingPublicPropertyExceptionConstructor = m_Module.ImportReference(ctor);
                }

                return m_MissingPublicPropertyExceptionConstructor;
            }
        }

        public MethodReference MissingPublicMethodExceptionConstructor
        {
            get
            {
                if (m_MissingPublicMethodExceptionConstructor is null)
                {
                    var ctor = typeof(MissingPublicMethodException).GetConstructor(new Type[] { typeof(string) });
                    m_MissingPublicMethodExceptionConstructor = m_Module.ImportReference(ctor);
                }

                return m_MissingPublicMethodExceptionConstructor;
            }
        }

        public GenericInstanceMethod MakeCastValueTypeMethod(params TypeReference[] genericArguments)
        {
            if (m_CastValueTypeMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                var method = typeof(CastUtility).GetMethod(nameof(CastUtility.CastValueType), flags);
                m_CastValueTypeMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_CastValueTypeMethod, genericArguments);
        }

        public GenericInstanceMethod MakeCastAnyMethod(params TypeReference[] genericArguments)
        {
            if (m_CastAnyMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                var method = typeof(CastUtility).GetMethod(nameof(CastUtility.CastAny), flags);
                m_CastAnyMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_CastAnyMethod, genericArguments);
        }

        public GenericInstanceMethod MakeDataModelGetMethod(params TypeReference[] genericArguments)
        {
            if (m_DataModelGetMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                var method = typeof(ReactiveModel).GetMethod(nameof(ReactiveModel.Get), flags);
                m_DataModelGetMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_DataModelGetMethod, genericArguments);
        }

        public GenericInstanceMethod MakeDataModelSetMethod(params TypeReference[] genericArguments)
        {
            if (m_DataModelSetMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                var method = typeof(ReactiveModel).GetMethod(nameof(ReactiveModel.Set), flags);
                m_DataModelSetMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_DataModelSetMethod, genericArguments);
        }

        public GenericInstanceMethod MakeDataModelGetPropertyMethod(params TypeReference[] genericArguments)
        {
            if (m_DataModelGetPropertyMethod is null)
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var method = typeof(ReactiveModel).GetMethod("GetProperty", flags);
                m_DataModelGetPropertyMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_DataModelGetPropertyMethod, genericArguments);
        }

        public GenericInstanceMethod MakeDataModelSetPropertyMethod(params TypeReference[] genericArguments)
        {
            if (m_DataModelSetPropertyMethod is null)
            {
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var method = typeof(ReactiveModel).GetMethod("SetProperty", 1, flags, null, new Type[]
                {
                    Type.MakeGenericMethodParameter(0).MakeByRefType(),
                    Type.MakeGenericMethodParameter(0),
                    typeof(string)
                }, null);
                m_DataModelSetPropertyMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_DataModelSetPropertyMethod, genericArguments);
        }

        public GenericInstanceMethod MakeDynamicArgumentGetValueMethod(params TypeReference[] genericArguments)
        {
            if (m_DynamicArgumentGetValueMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                var method = typeof(DynamicArgument).GetMethod(nameof(DynamicArgument.GetValue), flags);
                m_DynamicArgumentGetValueMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_DynamicArgumentGetValueMethod, genericArguments);
        }

        public GenericInstanceMethod MakeUIPageVoidInvokeMethod(params TypeReference[] genericArguments)
        {
            if (m_UIPageVoidInvokeMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                var method = typeof(UIPage).GetMethod(nameof(UIPage.InvokeMethod), 1, flags, null, new Type[]
                {
                    typeof(string),
                    typeof(Transform),
                    Type.MakeGenericMethodParameter(0),
                    typeof(DynamicArgument[])
                }, null);
                m_UIPageVoidInvokeMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_UIPageVoidInvokeMethod, genericArguments);
        }

        public GenericInstanceMethod MakeUIPageTInvokeMethod(params TypeReference[] genericArguments)
        {
            if (m_UIPageTInvokeMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                var method = typeof(UIPage).GetMethod(nameof(UIPage.InvokeMethod), 1, flags, null, new Type[]
                {
                    typeof(string),
                    typeof(Transform),
                    typeof(DynamicArgument[])
                }, null);
                m_UIPageTInvokeMethod = m_Module.ImportReference(method);
            }

            return MakeGenericMethod(m_UIPageTInvokeMethod, genericArguments);
        }

        public MethodReference MakeFuncDelegateConstructor(ModuleDefinition module, TypeReference genericTypeArgument)
        {
            var funcType = new GenericInstanceType(new TypeReference("System", "Func`1", module,
                module.TypeSystem.CoreLibrary, false));
            funcType.GenericArguments.Add(genericTypeArgument);

            return new MethodReference(".ctor", module.TypeSystem.Void, funcType)
            {
                HasThis = true,
                Parameters = { new(module.TypeSystem.Object), new(module.TypeSystem.IntPtr) }
            };
        }

        public MethodReference MakeLazyComputedPropertyCtor2Args(ModuleDefinition module, TypeReference declaringType)
        {
            MethodReference computedCtor = module.ImportReference(typeof(LazyComputedProperty<>)
                .GetConstructors().First(ctor => ctor.GetParameters().Length == 2));
            computedCtor.DeclaringType = declaringType;
            return computedCtor;
        }

        public MethodReference MakeLazyComputedPropertyValueGetter(ModuleDefinition module, TypeReference declaringType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var method = typeof(LazyComputedProperty<>).GetProperty("Value", flags).GetGetMethod(false);

            MethodReference result = module.ImportReference(method);
            result.DeclaringType = declaringType;
            return result;
        }

        private static GenericInstanceMethod MakeGenericMethod(MethodReference genericMethodDefinition, TypeReference[] genericArguments)
        {
            var result = new GenericInstanceMethod(genericMethodDefinition);

            for (int i = 0; i < genericArguments.Length; i++)
            {
                result.GenericArguments.Add(genericArguments[i]);
            }

            return result;
        }
    }
}