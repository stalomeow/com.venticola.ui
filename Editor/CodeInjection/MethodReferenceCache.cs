using Mono.Cecil;
using System;
using System.Reflection;
using VentiCola.UI.Internal;

namespace VentiColaEditor.UI.CodeInjection
{
    public class MethodReferenceCache
    {
        private readonly ModuleDefinition m_Module;
        private MethodReference m_StringHashMethod;
        private MethodReference m_StringEqualityOperator;
        private MethodReference m_CastValueTypeMethod;
        private MethodReference m_CastAnyMethod;
        private MethodReference m_ChangeUtilityTryAddCurrentObserverMethod;
        private MethodReference m_ChangeUtilitySetWithNotifyMethod;
        private MethodReference m_ChangeUtilitySetWithNotifyMethodWithRefComparer;

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

        public MethodReference ChangeUtilityTryAddCurrentObserverMethod
        {
            get
            {
                if (m_ChangeUtilityTryAddCurrentObserverMethod is null)
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                    var method = typeof(ChangeUtility).GetMethod(nameof(ChangeUtility.TryAddCurrentObserver), flags);
                    m_ChangeUtilityTryAddCurrentObserverMethod = m_Module.ImportReference(method);
                }

                return m_ChangeUtilityTryAddCurrentObserverMethod;
            }
        }

        public MethodReference MakeCastValueTypeMethod(params TypeReference[] genericArguments)
        {
            if (m_CastValueTypeMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                var method = typeof(CastUtility).GetMethod(nameof(CastUtility.CastValueType), flags);
                m_CastValueTypeMethod = m_Module.ImportReference(method);
            }

            return MetaDataUtility.MakeGenericInstanceMethod(m_CastValueTypeMethod, genericArguments);
        }

        public MethodReference MakeCastAnyMethod(params TypeReference[] genericArguments)
        {
            if (m_CastAnyMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                var method = typeof(CastUtility).GetMethod(nameof(CastUtility.CastAny), flags);
                m_CastAnyMethod = m_Module.ImportReference(method);
            }

            return MetaDataUtility.MakeGenericInstanceMethod(m_CastAnyMethod, genericArguments);
        }

        public MethodReference MakeChangeUtilitySetWithNotifyMethod(params TypeReference[] genericArguments)
        {
            if (m_ChangeUtilitySetWithNotifyMethod is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                var method = typeof(ChangeUtility).GetMethod(nameof(ChangeUtility.SetWithNotify), 1, flags, null, new Type[]
                {
                    Type.MakeGenericMethodParameter(0).MakeByRefType(),
                    Type.MakeGenericMethodParameter(0),
                    typeof(WeakHashSet<IChangeObserver>),
                }, Array.Empty<ParameterModifier>());
                m_ChangeUtilitySetWithNotifyMethod = m_Module.ImportReference(method);
            }

            return MetaDataUtility.MakeGenericInstanceMethod(m_ChangeUtilitySetWithNotifyMethod, genericArguments);
        }

        public MethodReference MakeChangeUtilitySetWithNotifyMethodWithRefComparer(params TypeReference[] genericArguments)
        {
            if (m_ChangeUtilitySetWithNotifyMethodWithRefComparer is null)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                var method = typeof(ChangeUtility).GetMethod(nameof(ChangeUtility.SetWithNotify), 2, flags, null, new Type[]
                {
                    Type.MakeGenericMethodParameter(0).MakeByRefType(),
                    Type.MakeGenericMethodParameter(0),
                    typeof(WeakHashSet<IChangeObserver>),
                    Type.MakeGenericMethodParameter(1).MakeByRefType(),
                }, Array.Empty<ParameterModifier>());
                m_ChangeUtilitySetWithNotifyMethodWithRefComparer = m_Module.ImportReference(method);
            }

            return MetaDataUtility.MakeGenericInstanceMethod(m_ChangeUtilitySetWithNotifyMethodWithRefComparer, genericArguments);
        }

        public MethodReference MakeFuncDelegateConstructor(TypeReference genericTypeArgument)
        {
            var funcType = new GenericInstanceType(new TypeReference("System", "Func`1", m_Module,
                m_Module.TypeSystem.CoreLibrary, false));
            funcType.GenericArguments.Add(genericTypeArgument);

            return new MethodReference(".ctor", m_Module.TypeSystem.Void, funcType)
            {
                HasThis = true,
                Parameters = { new(m_Module.TypeSystem.Object), new(m_Module.TypeSystem.IntPtr) }
            };
        }

        public MethodReference MakeLazyComputedPropertyCtor(TypeReference declaringType)
        {
            MethodReference computedCtor = m_Module.ImportReference(typeof(LazyComputedProperty<>).GetConstructors()[0]);
            computedCtor.DeclaringType = declaringType;
            return computedCtor;
        }

        public MethodReference MakeLazyComputedPropertyValueGetter(TypeReference declaringType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var method = typeof(LazyComputedProperty<>).GetProperty("Value", flags).GetGetMethod(false);

            MethodReference result = m_Module.ImportReference(method);
            result.DeclaringType = declaringType;
            return result;
        }
    }
}