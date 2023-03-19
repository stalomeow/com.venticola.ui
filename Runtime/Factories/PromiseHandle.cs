using System;
using VentiCola.UI.Internals;

namespace VentiCola.UI.Factories
{
    public readonly struct PromiseHandle<T>
    {
        private readonly IReusableObject m_State;
        private readonly Action<IReusableObject, T> m_ResolveCallback;
        private readonly Action<IReusableObject, Exception> m_RejectCallback;
        private readonly int m_Version;

        internal PromiseHandle(IReusableObject state, Action<IReusableObject, T> resolveCallback, Action<IReusableObject, Exception> rejectCallback)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_ResolveCallback = resolveCallback ?? throw new ArgumentNullException(nameof(resolveCallback));
            m_RejectCallback = rejectCallback ?? throw new ArgumentNullException(nameof(rejectCallback));
            m_Version = state.Version;
        }

        public void Resolve(T value)
        {
            if (m_State.Version != m_Version)
            {
                throw new ObjectDisposedException(nameof(PromiseHandle<T>));
            }

            m_ResolveCallback(m_State, value);
        }

        public void Reject(Exception exception)
        {
            if (m_State.Version != m_Version)
            {
                throw new ObjectDisposedException(nameof(PromiseHandle<T>));
            }

            m_RejectCallback(m_State, exception);
        }
    }
}