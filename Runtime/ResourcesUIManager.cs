using System;
using UnityEngine;

namespace VentiCola.UI
{
    public class ResourcesUIManager : BaseUIManager
    {
        public ResourcesUIManager() { }

        protected override void LoadPrefabAsync(string key, Action<string, GameObject> callback)
        {
            Resources.LoadAsync<GameObject>(key).completed += op =>
            {
                callback(key, ((ResourceRequest)op).asset as GameObject);
            };
        }

        protected override void ReleasePrefab(string key, GameObject prefab)
        {
            Resources.UnloadAsset(prefab);
        }
    }
}