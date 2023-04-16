using System;
using UnityEditor;
using UnityEngine;

namespace VentiColaEditor.UI
{
    public static class CustomEditorGUIUtils
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

        /// <summary>Draw a splitter separator</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            // * from unity srp

            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }
    }
}