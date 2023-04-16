using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VentiColaEditor.UI.CodeInjection;

namespace VentiColaEditor.UI.Settings
{
    [FilePath("ProjectSettings/VentiCola/UIProjectSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class UIProjectSettings : ScriptableSingleton<UIProjectSettings>
    {
        [SerializeField] private bool m_AutoCodeInjection = true;
        [SerializeField] private LogLevel m_CodeInjectionLogLevel = LogLevel.Assembly;
        [SerializeField] private List<string> m_CodeInjectionAssemblyWhiteList = new()
        {
            "Assembly-CSharp",
            "VentiCola.UI.Tests"
        };

        /// <summary>
        /// 获取/设置是否自动执行 Code Injection。
        /// </summary>
        public bool AutoCodeInjection
        {
            get => m_AutoCodeInjection;
            set => m_AutoCodeInjection = value;
        }

        /// <summary>
        /// 获取/设置 Code Injection 的日志输出级别。
        /// </summary>
        public LogLevel CodeInjectionLogLevel
        {
            get => m_CodeInjectionLogLevel;
            set => m_CodeInjectionLogLevel = value;
        }

        /// <summary>
        /// 获取 Code Injection 的白名单程序集。
        /// </summary>
        public List<string> CodeInjectionAssemblyWhiteList
        {
            get => m_CodeInjectionAssemblyWhiteList;
        }


        private void OnEnable() => hideFlags &= ~HideFlags.NotEditable;

        private void OnDisable() => Save();

        public void Save() => Save(true);

        public SerializedObject AsSerializedObject() => new SerializedObject(this);


        /// <summary>
        /// 在 ProjectSettings 窗口中的路径
        /// </summary>
        public const string PathInProjectSettings = "Project/VentiCola/UI";

        /// <summary>
        /// 在 ProjectSettings 窗口中打开
        /// </summary>
        public static void OpenInProjectSettings() => SettingsService.OpenProjectSettings(PathInProjectSettings);
    }
}