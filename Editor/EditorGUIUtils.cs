using System;
using UnityEditor;
using UnityEngine;

namespace VentiColaEditor.UI
{
    public static class EditorGUIUtils
    {
        public static float CurrentIndentWidth
        {
            get
            {
                // 原理请看 IndentedRect 源码
                // 所以为什么 EditorGUI.indent 不公开啊？？？
                return EditorGUI.IndentedRect(new Rect(0, 0, 0, 0)).x;
            }
        }

        public static float SingleIndentWidth
        {
            get
            {
                EditorGUI.indentLevel++;
                float oneIndent = CurrentIndentWidth;
                EditorGUI.indentLevel--;
                return oneIndent - CurrentIndentWidth;
            }
        }

        /// <summary>
        /// Just equivalent to <code>new UnityEditor.SettingsWindow.GUIScope()</code>
        /// </summary>
        /// <remarks>See TimelineProjectSettingsProvider in Packages/com.unity.timeline/Editor/inspectors/TimelineProjectSettings.cs</remarks>
        /// <returns></returns>
        public static GUI.Scope NewSettingsWindowGUIScope()
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as GUI.Scope;
        }
    }
}