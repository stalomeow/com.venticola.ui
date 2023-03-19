using UnityEditor;
using UnityEngine;
using VentiCola.UI.Bindings;

namespace VentiColaEditor.UI
{
    [CustomPropertyDrawer(typeof(ValueTransition))]
    public class ValueTransitionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.FindPropertyRelative(nameof(ValueTransition.Enable)).boolValue
                ? 5 * EditorGUIUtility.singleLineHeight + 4 * EditorGUIUtility.standardVerticalSpacing
                : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var enable = property.FindPropertyRelative(nameof(ValueTransition.Enable));
            var curve = property.FindPropertyRelative(nameof(ValueTransition.Curve));
            var delay = property.FindPropertyRelative(nameof(ValueTransition.Delay));
            var duration = property.FindPropertyRelative(nameof(ValueTransition.Duration));
            var ignoreTimeScale = property.FindPropertyRelative(nameof(ValueTransition.IgnoreTimeScale));

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            enable.boolValue = EditorGUI.Toggle(rect, label, enable.boolValue);

            if (enable.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, curve);

                    rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, delay);

                    rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, duration);

                    rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, ignoreTimeScale);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}