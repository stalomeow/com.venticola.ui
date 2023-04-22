namespace VentiColaEditor.UI.CodeInjection
{
    public readonly struct LocalOrParameter
    {
        private readonly bool m_IsLocal;
        private readonly int m_Index;

        private LocalOrParameter(bool isLocal, int index)
        {
            m_IsLocal = isLocal;
            m_Index = index;
        }

        public bool IsLocal => m_IsLocal;

        public int Index => m_Index;

        public static LocalOrParameter Local(int index)
        {
            return new LocalOrParameter(true, index);
        }

        public static LocalOrParameter Parameter(int index)
        {
            return new LocalOrParameter(false, index);
        }
    }
}