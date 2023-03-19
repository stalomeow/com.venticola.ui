using UnityEngine.Scripting;

namespace VentiCola.UI.Factories
{
    [RequireImplementors]
    public interface IAsyncPageFactory
    {
        /// <summary>
        /// 异步实例化一个 UI 页面
        /// </summary>
        /// <param name="key">UI 页面的 Key</param>
        /// <param name="handle">the promise handle</param>
        void InstantiateAsync(string key, PromiseHandle<UIPage> handle);

        /// <summary>
        /// 销毁一个 UI 页面
        /// </summary>
        /// <param name="key">UI 页面的 Key</param>
        /// <param name="page">UI 页面对象</param>
        void Destroy(string key, UIPage page);
    }
}