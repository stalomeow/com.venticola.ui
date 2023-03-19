using UnityEditor;
using UnityEngine;
using VentiCola.UI.Transitions;

namespace VentiColaEditor.UI
{
    [CustomPropertyDrawer(typeof(TweenConfig))]
    public class TweenConfigDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            const int extraLineCount = 5;

            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(nameof(TweenConfig.Easing)))
                + extraLineCount * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            /*label = */EditorGUI.BeginProperty(position, label, property);

            using (new EditorGUI.IndentLevelScope())
            {
                GUIStyle downArrowStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                GUIContent downArrowContent = new GUIContent(EditorGUIUtility.IconContent("IN foldout act on").image);

                Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(rect, downArrowContent, downArrowStyle);

                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(TweenConfig.Delay)));

                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(TweenConfig.Duration)));

                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                SerializedProperty easing = property.FindPropertyRelative(nameof(TweenConfig.Easing));
                EditorGUI.PropertyField(rect, easing);

                rect.y += EditorGUI.GetPropertyHeight(easing) + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(TweenConfig.IgnoreTimeScale)));

                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.LabelField(rect, downArrowContent, downArrowStyle);
            }

            // 绘制左侧长长的竖线
            const float timeLineWidth = 2f;
            float indentWidth = EditorGUIUtils.SingleIndentWidth;
            Rect lineRect = new Rect(position.x + (indentWidth - timeLineWidth) / 2, position.y, timeLineWidth, position.height);
            EditorGUI.DrawRect(lineRect, new Color32(0x88, 0x88, 0x88, 255));

            EditorGUI.EndProperty();
        }
    }
}