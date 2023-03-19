using System;
using UnityEngine;

namespace VentiCola.UI.Factories
{
    using Object = UnityEngine.Object;

    [CustomPageFactory("Resources Folder")]
    internal class ResourcesPageFactory : IAsyncPageFactory
    {
        public void InstantiateAsync(string key, PromiseHandle<UIPage> handle)
        {
            Resources.LoadAsync<GameObject>(key).completed += op =>
            {
                UIPage page;

                try
                {
                    var asset = ((ResourceRequest)op).asset as GameObject;
                    var go = Object.Instantiate(asset, Vector3.zero, Quaternion.identity);
                    page = go.GetComponent<UIPage>();
                }
                catch (Exception e)
                {
                    handle.Reject(e);
                    return;
                }

                // 不应该把 Resolve 写在 try 里面，因为 Resolve 可能也会抛出异常
                handle.Resolve(page);
            };
        }

        public void Destroy(string key, UIPage page)
        {
            Object.Destroy(page.gameObject);
        }
    }
}