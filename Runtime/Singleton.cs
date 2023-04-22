using System.Threading;

namespace VentiCola.UI
{
    public static class Singleton<T> where T : class, new()
    {
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if (s_Instance is null)
                {
                    Interlocked.CompareExchange(ref s_Instance, new T(), null);
                }

                return s_Instance;
            }
        }
    }
}