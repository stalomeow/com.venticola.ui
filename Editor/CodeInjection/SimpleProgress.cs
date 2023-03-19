using System;

namespace VentiColaEditor.UI.CodeInjection
{
    internal sealed class SimpleProgress<T> : IProgress<T>
    {
        private readonly Action<T> m_Callback;

        public SimpleProgress(Action<T> callback)
        {
            m_Callback = callback;
        }

        public void Report(T value)
        {
            m_Callback.Invoke(value);
        }
    }
}