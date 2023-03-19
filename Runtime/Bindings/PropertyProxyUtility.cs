using System.Collections.Generic;

namespace VentiCola.UI.Bindings
{
    internal static class PropertyProxyUtility
    {
        private static Dictionary<string, ReactiveModel> s_GlobalModels;

        public static void SetGlobalModel(string name, ReactiveModel model)
        {
            s_GlobalModels ??= new Dictionary<string, ReactiveModel>();
            s_GlobalModels[name] = model;
        }

        public static bool TryGetGlobalModel(string name, out ReactiveModel model)
        {
            if (s_GlobalModels is null)
            {
                model = null;
                return false;
            }

            return s_GlobalModels.TryGetValue(name, out model);
        }
    }
}