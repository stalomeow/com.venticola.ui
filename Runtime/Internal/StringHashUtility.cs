namespace VentiCola.UI.Internal
{
    public static class StringHashUtility
    {
        public static uint ComputeStringHash(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return default;
            }

            uint hash = 2166136261u;

            for (int i = 0; i < s.Length; i++)
            {
                hash = (s[i] ^ hash) * 16777619u;
            }

            return hash;
        }
    }
}