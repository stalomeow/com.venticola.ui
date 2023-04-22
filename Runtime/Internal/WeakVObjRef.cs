using System;

namespace VentiCola.UI.Internal
{
    /// <summary>
    /// 对 <see cref="IVersionable"/> 对象的弱引用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct WeakVObjRef<T> where T : class, IVersionable
    {
        private readonly T m_Target;
        private readonly int m_Version;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">目标对象，可以为 null</param>
        public WeakVObjRef(T target)
        {
            m_Target = target;
            m_Version = (target == null) ? -1 : target.Version;
        }

        public bool IsAlive => (m_Target != null) && (m_Target.Version == m_Version);

        public T Target
        {
            get
            {
                if (!IsAlive)
                {
                    throw new ObjectDisposedException(TypeUtility.GetFriendlyTypeName(typeof(T), false));
                }

                return m_Target;
            }
        }

        public T GetTargetOrDefault(T defaultValue)
        {
            return IsAlive ? m_Target : defaultValue;
        }

        public bool TryGetTarget(out T target)
        {
            if (IsAlive)
            {
                target = m_Target;
                return true;
            }

            target = null;
            return false;
        }

        public static implicit operator WeakVObjRef<T>(T target)
        {
            return new WeakVObjRef<T>(target);
        }
    }
}