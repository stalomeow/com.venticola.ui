using System;

namespace VentiCola.UI
{
    public readonly struct PromiseCallbacks<T>
    {
        private readonly object m_State;
        private readonly Action<object, T> m_ResolveCallback;
        private readonly Action<object, Exception> m_RejectCallback;

        public PromiseCallbacks(object state, Action<object, T> resolveCallback, Action<object, Exception> rejectCallback)
        {
            m_State = state;
            m_ResolveCallback = resolveCallback;
            m_RejectCallback = rejectCallback;
        }

        public void Resolve(T value)
        {
            m_ResolveCallback?.Invoke(m_State, value);
        }

        public void Reject(Exception exception)
        {
            m_RejectCallback?.Invoke(m_State, exception);
        }
    }
}