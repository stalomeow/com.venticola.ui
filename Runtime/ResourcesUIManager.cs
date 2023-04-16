using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VentiCola.UI
{
    public class ResourcesUIManager : BaseUIManager
    {
        public ResourcesUIManager() { }

        protected override void InstantiateAsync(string key, PromiseCallbacks<BaseUIPageView> callbacks)
        {
            Resources.LoadAsync<GameObject>(key).completed += op =>
            {
                BaseUIPageView page;

                try
                {
                    var asset = ((ResourceRequest)op).asset as GameObject;
                    var go = Object.Instantiate(asset, Vector3.zero, Quaternion.identity);
                    page = go.GetComponent<BaseUIPageView>();
                }
                catch (Exception e)
                {
                    callbacks.Reject(e);
                    return;
                }

                // 不应该把 Resolve 写在 try 里面，因为 Resolve 可能也会抛出异常
                callbacks.Resolve(page);
            };
        }

        protected override void Destroy(string key, BaseUIPageView page)
        {
            Object.Destroy(page.gameObject);
        }
    }
}