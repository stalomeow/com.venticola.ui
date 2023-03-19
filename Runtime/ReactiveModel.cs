using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VentiCola.UI.Internals;

namespace VentiCola.UI
{
    /// <summary>
    /// 表示一个响应式的数据模型
    /// </summary>
    public abstract class ReactiveModel
    {
        // fields are lazy initialized
        [NonSerialized] private Dictionary<string, Delegate> m_PropertyGetters;
        [NonSerialized] private Dictionary<string, Delegate> m_PropertySetters;
        [NonSerialized] private Dictionary<string, UnityWeakHashSet<IChangeObserver>> m_PropertyObservers;

        protected ReactiveModel() { }

        // thread-safe
        public virtual T Get<T>(string propertyName)
        {
            UnityEngine.Debug.LogWarning($"Get property '{propertyName}' in '{GetType().Name}' using reflection!");

            if (m_PropertyGetters is null || !m_PropertyGetters.TryGetValue(propertyName, out Delegate func))
            {
                Type type = GetType();

                if (!PropertyReflectionUtility.TryGetPropertyPublicGetMethod(type, propertyName, out MethodInfo method))
                {
                    throw new InvalidOperationException($"{type} does not have public readable property '{propertyName}'.");
                }

                func = method.CreateDelegate(typeof(Func<>).MakeGenericType(method.ReturnType), this);
                m_PropertyGetters ??= new Dictionary<string, Delegate>();
                m_PropertyGetters.Add(propertyName, func);
            }

            // observers will be collected in the getter of the property.

            if (func is Func<T> getter)
            {
                return getter();
            }

            return (T)func.DynamicInvoke(args: Array.Empty<object>());
        }

        // thread-safe
        public virtual void Set<T>(string propertyName, T value)
        {
            UnityEngine.Debug.LogWarning($"Set property '{propertyName}' in '{GetType().Name}' using reflection!");

            if (m_PropertySetters is null || !m_PropertySetters.TryGetValue(propertyName, out Delegate func))
            {
                Type type = GetType();

                if (!PropertyReflectionUtility.TryGetPropertyPublicSetMethod(type, propertyName, out MethodInfo method))
                {
                    throw new InvalidOperationException($"{type} does not have public writable property '{propertyName}'.");
                }

                // TODO: Optimize next line.
                func = method.CreateDelegate(typeof(Action<>).MakeGenericType(method.GetParameters()[0].ParameterType), this);
                m_PropertySetters ??= new Dictionary<string, Delegate>();
                m_PropertySetters.Add(propertyName, func);
            }

            if (func is Action<T> setter)
            {
                setter(value);
            }
            else
            {
                // TODO: Optimize next line.
                func.DynamicInvoke(value);
            }
        }

        protected T GetProperty<T>(in T value, [CallerMemberName] string propertyName = null)
        {
            IChangeObserver observer = ChangeUtility.CurrentObserver;

            if (observer is { IsPassive: false })
            {
                m_PropertyObservers ??= new Dictionary<string, UnityWeakHashSet<IChangeObserver>>();

                if (!m_PropertyObservers.TryGetValue(propertyName, out UnityWeakHashSet<IChangeObserver> observers))
                {
                    observers = new UnityWeakHashSet<IChangeObserver>();
                    m_PropertyObservers.Add(propertyName, observers);
                }

                observers.Add(observer);
            }

            return value;
        }

        protected void SetProperty<T>(ref T location, T value, [CallerMemberName] string propertyName = null)
        {
            SetProperty(ref location, value, EqualityComparer<T>.Default, propertyName);
        }

        protected void SetProperty<T>(ref T location, T value, IEqualityComparer<T> comparer, [CallerMemberName] string propertyName = null)
        {
            if (comparer.Equals(location, value))
            {
                return;
            }

            location = value;
            SetPropertyChanged(propertyName);
        }

        protected void SetPropertyChanged(string propertyName)
        {
            if (m_PropertyObservers is null)
            {
                return;
            }

            if (m_PropertyObservers.TryGetValue(propertyName, out UnityWeakHashSet<IChangeObserver> observers))
            {
                ChangeUtility.NotifyObservers(observers);
            }
        }
    }
}