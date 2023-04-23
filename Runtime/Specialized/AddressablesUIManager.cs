#if PACKAGE_ADDRESSABLES
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace VentiCola.UI.Specialized
{
    public class AddressablesUIManager : BaseUIManager
    {
        protected override void LoadPrefabAsync(string key, Action<string, GameObject> callback)
        {
            Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
            {
                callback(key, handle.Result);
            };
        }

        protected override void ReleasePrefab(string key, GameObject prefab)
        {
            Addressables.Release(prefab);
        }
    }
}
#endif