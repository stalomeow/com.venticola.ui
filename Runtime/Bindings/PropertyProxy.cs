using System;
using System.Collections.Generic;
using UnityEngine;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Bindings
{
    [Serializable]
    public class PropertyProxy
    {
        [SerializeField] protected Component m_SourceObj;
        [SerializeField] protected string[] m_PropertyPath;
        [SerializeField] private string m_TypeName;
        [NonSerialized] private Type m_CachedType;

        protected internal PropertyProxy() { }

        public Type PropertyType
        {
            get
            {
                if (string.IsNullOrEmpty(m_TypeName))
                {
                    return null;
                }

                return m_CachedType ??= Type.GetType(m_TypeName);
            }
        }

        public virtual T GetValue<T>()
        {
            if (m_PropertyPath.Length == 0)
            {
                throw new InvalidOperationException("PropertyProxy is unbound!");
            }

            ReactiveModel model;

            if (m_SourceObj is ICustomScope scope)
            {
                if (m_PropertyPath.Length == 1)
                {
                    return scope.GetVariable<T>(m_PropertyPath[0]);
                }

                model = scope.GetVariable<ReactiveModel>(m_PropertyPath[0]);
            }
            else
            {
                if (!PropertyProxyUtility.TryGetGlobalModel(m_PropertyPath[0], out model))
                {
                    throw new KeyNotFoundException($"Can not find Global Model <{m_PropertyPath[0]}>.");
                }
            }

            for (int i = 1; i < m_PropertyPath.Length - 1; i++)
            {
                model = model.Get<ReactiveModel>(m_PropertyPath[i]);
            }

            return model.Get<T>(m_PropertyPath[^1]);
        }

        public virtual void SetValue<T>(T value)
        {
            if (m_PropertyPath.Length == 0)
            {
                throw new InvalidOperationException("PropertyProxy is unbound!");
            }

            ReactiveModel model;

            if (m_SourceObj is ICustomScope scope)
            {
                if (m_PropertyPath.Length == 1)
                {
                    throw new InvalidOperationException("Property is not writable!");
                }

                model = scope.GetVariable<ReactiveModel>(m_PropertyPath[0]);
            }
            else
            {
                if (!PropertyProxyUtility.TryGetGlobalModel(m_PropertyPath[0], out model))
                {
                    throw new KeyNotFoundException($"Can not find Global Model <{m_PropertyPath[0]}>.");
                }
            }

            for (int i = 1; i < m_PropertyPath.Length - 1; i++)
            {
                model = model.Get<ReactiveModel>(m_PropertyPath[i]);
            }

            model.Set<T>(m_PropertyPath[^1], value);
        }
    }
}