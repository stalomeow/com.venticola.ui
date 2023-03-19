using System;
using System.Diagnostics;
using UnityEngine;
using VentiCola.UI.Factories;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace VentiCola.UI
{
    internal class UIRuntimeSettings : ScriptableObject
    {
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
        private class DefaultShaderAttribute : Attribute
        {
            public string AssetPath { get; }

            public DefaultShaderAttribute(string assetPath)
            {
                AssetPath = assetPath;
            }
        }

        [Serializable]
        public class ShaderResources
        {
            [DefaultShader("Packages/com.venticola.ui/Shaders/GaussianBlur.shader")]
            public Shader GaussianBlur5x5;

            [DefaultShader("Packages/com.venticola.ui/Shaders/GaussianBlur.shader")]
            public Shader GaussianBlur3x3;

            public Shader BoxBlur;

            public Shader KawaseBlur;

            public Shader DualBlur;
        }

        private const string k_ConfigName = "com.stalo.venticola.ui";

        public static Type DefaultPageFactoryType => typeof(ResourcesPageFactory);

        [SerializeField] private string m_PageFactoryTypeName;
        [SerializeField, Min(1)] private int m_UIStackMinGrow = 5;
        [SerializeField, Min(1)] private int m_UIStackMaxGrow = 10;
        [SerializeField] private GameObject m_UIRootPrefab;
        [SerializeField] private Texture2D m_BlurFallbackTexture;
        [SerializeField] private ShaderResources m_Shaders;

        public string PageFactoryTypeName
        {
            get
            {
                // 需要保证至少有一个 Factory
                if (string.IsNullOrWhiteSpace(m_PageFactoryTypeName))
                {
                    return DefaultPageFactoryType.AssemblyQualifiedName;
                }

                return m_PageFactoryTypeName;
            }
        }

        public int UIStackMinGrow
        {
            get => m_UIStackMinGrow;
        }

        public int UIStackMaxGrow
        {
            get => m_UIStackMaxGrow;
        }

        public GameObject UIRootPrefab
        {
            get => m_UIRootPrefab;
        }

        public Texture2D BlurFallbackTexture
        {
            get => m_BlurFallbackTexture;
        }

        public ShaderResources Shaders
        {
            get => m_Shaders;
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

        public static UIRuntimeSettings FindInstance()
        {
#if UNITY_EDITOR
            if (EditorBuildSettings.TryGetConfigObject(k_ConfigName, out UIRuntimeSettings settings))
            {
                return settings;
            }

            return null;
#else
            var settings = Resources.FindObjectsOfTypeAll<UIRuntimeSettings>();

            if (settings.Length == 0)
            {
                UnityEngine.Debug.LogError($"No {typeof(UIRuntimeSettings)} was found!");
                return null;
            }

            if (settings.Length > 1)
            {
                UnityEngine.Debug.LogWarning($"{settings.Length} {typeof(UIRuntimeSettings)} were found! Only the first one will be used!");
            }

            return settings[0];
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void SetInstance(UIRuntimeSettings settings)
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
    }
}