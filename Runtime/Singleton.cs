using System.Threading;

namespace VentiCola.UI
{
    /// <summary>
    /// 线程安全的单例辅助类
    /// </summary>
    /// <typeparam name="T"></typeparam>
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