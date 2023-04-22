using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using VentiCola.UI;
using Object = UnityEngine.Object;

namespace VentiColaEditor.UI.Settings
{
    internal class UISettingsBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = UIRuntimeSettings.Instance;

            if (settings == null)
            {
                return;
            }

            Object[] preloadAssets = PlayerSettings.GetPreloadedAssets();

            if (Array.IndexOf(preloadAssets, settings) >= 0)
            {
                return;
            }

            Array.Resize(ref preloadAssets, preloadAssets.Length + 1);
            preloadAssets[^1] = settings;
            PlayerSettings.SetPreloadedAssets(preloadAssets);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Object[] preloadAssets = PlayerSettings.GetPreloadedAssets();
            int preloadAssetCount = preloadAssets.Length;

            for (int i = preloadAssets.Length - 1; i >= 0; i--)
            {
                if (preloadAssets[i] is UIRuntimeSettings)
                {
                    for (int j = i; j < preloadAssetCount - 1; j++)
                    {
                        preloadAssets[j] = preloadAssets[j + 1];
                    }

                    preloadAssetCount--;
                }
            }

            PlayerSettings.SetPreloadedAssets(preloadAssets[..preloadAssetCount]);
        }
    }
}