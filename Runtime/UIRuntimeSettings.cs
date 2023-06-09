using System;
using UnityEngine;
using VentiCola.UI.Rendering;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace VentiCola.UI
{
    public class UIRuntimeSettings : ScriptableObject
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        private sealed class DefaultShaderAttribute : Attribute
        {
            public string AssetPath { get; }

            public DefaultShaderAttribute(string assetPath) => AssetPath = assetPath;
        }

        [Serializable]
        public class ShaderResources
        {
            [DefaultShader("Packages/com.venticola.ui/Shaders/BlurBackground.shader")]
            public Shader BlurBackground;

            [DefaultShader("Packages/com.venticola.ui/Shaders/GaussianBlur.shader")]
            public Shader GaussianBlur;

            public Shader BoxBlur;

            public Shader KawaseBlur;

            public Shader DualBlur;
        }

        private const string k_ConfigName = "com.stalo.venticola.ui";

        [SerializeField, Min(3)] private int m_LRUCacheSize = 5;
        [SerializeField] private GameObject m_UIRootPrefab;
        [SerializeField] private ShaderResources m_Shaders;
        [SerializeField] private bool m_EnableMainCameraOverrideSettings = true;
        [SerializeField] private bool m_EnableMainCameraRendererSettings = true;
        [SerializeField] private CameraOverrideSettings m_MainCameraOverrideSettings;
        [SerializeField] private CameraRendererSettings m_MainCameraRendererSettings;
        [SerializeField] private ScriptableObject m_AdditionalData;

        public int LRUCacheSize => m_LRUCacheSize;

        public GameObject UIRootPrefab => m_UIRootPrefab;

        public ShaderResources DefaultShaders => m_Shaders;

        public bool EnableMainCameraOverrideSettings => m_EnableMainCameraOverrideSettings;

        public bool EnableMainCameraRendererSettings => m_EnableMainCameraRendererSettings;

        public CameraOverrideSettings MainCameraOverrideSettings => m_MainCameraOverrideSettings;

        public CameraRendererSettings MainCameraRendererSettings => m_MainCameraRendererSettings;

        public T GetAdditionalData<T>() where T : ScriptableObject
        {
            return m_AdditionalData as T;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            m_Shaders ??= new ShaderResources();

            // reset all shaders
            foreach (var field in typeof(ShaderResources).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = field.GetCustomAttribute<DefaultShaderAttribute>();
                var shaderPath = attr?.AssetPath;

                if (string.IsNullOrEmpty(shaderPath))
                {
                    continue;
                }

                field.SetValue(m_Shaders, AssetDatabase.LoadAssetAtPath<Shader>(shaderPath));
            }
        }
#endif

        private static UIRuntimeSettings s_Instance;

        public static UIRuntimeSettings Instance
        {
            get
            {
                if (s_Instance == null)
                {
#if UNITY_EDITOR
                    if (EditorBuildSettings.TryGetConfigObject(k_ConfigName, out UIRuntimeSettings settings))
                    {
                        s_Instance = settings;
                    }
#else
                    var settings = Resources.FindObjectsOfTypeAll<UIRuntimeSettings>();

                    if (settings.Length == 0)
                    {
                        throw new MissingReferenceException($"Can not find {typeof(UIRuntimeSettings)}!");
                    }

                    if (settings.Length > 1)
                    {
                        UnityEngine.Debug.LogWarning($"{settings.Length} {typeof(UIRuntimeSettings)} were found! Only the first one will be used!");
                    }

                    s_Instance = settings[0];
#endif
                }

                return s_Instance;
            }
        }

#if UNITY_EDITOR
        internal static void SetInstance(UIRuntimeSettings settings)
        {

            if (settings == null)
            {
                EditorBuildSettings.RemoveConfigObject(k_ConfigName);
            }
            else
            {
                EditorBuildSettings.AddConfigObject(k_ConfigName, settings, true);
            }
        }
#endif
    }
}