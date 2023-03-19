using System.Reflection;
using UnityEditor;
using UnityEngine;
using VentiCola.UI.Transitions;

namespace VentiColaEditor.UI.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(EasingCurve))]
    public class EasingCurveDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GetCurveTypeProperty(property, out EasingCurveType type);
            int lines = type switch
            {
                EasingCurveType.QuadraticBezier => 2,
                EasingCurveType.CubicBezier => 3,
                _ => 1
            };
            return lines * EditorGUIUtility.singleLineHeight
                + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var typeProperty = GetCurveTypeProperty(property, out EasingCurveType type);

            if (type == EasingCurveType.Custom)
            {
                DrawCustomCurve(position, label, property, typeProperty);
            }
            else
            {
                var typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(typeRect, typeProperty, label);

                if (type is EasingCurveType.QuadraticBezier or EasingCurveType.CubicBezier)
                {
                    int pointCount = (type is EasingCurveType.QuadraticBezier) ? 1 : 2;
                    float startY = typeRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    DrawBezierControlPoints(property, pointCount, position.x, startY, position.width);
                }
            }

            EditorGUI.EndProperty();
        }

        private static void DrawCustomCurve(Rect position, GUIContent label, SerializedProperty property, SerializedProperty typeProperty)
        {
            const float dropdownWidth = 20f;
            const float dropdownPadding = 2f;

            var curveRect = new Rect(position.x, position.y, position.width - dropdownWidth - dropdownPadding, position.height);
            var dropdownRect = new Rect(curveRect.xMax + dropdownPadding, position.y, dropdownWidth, position.height);

            EditorGUI.PropertyField(curveRect, property.FindPropertyRelative("m_Curve"), label);

            if (EditorGUI.DropdownButton(dropdownRect, GUIContent.none, FocusType.Keyboard, EditorStyles.popup))
            {
                var menu = new GenericMenu();
                var fields = typeof(EasingCurveType).GetFields(BindingFlags.Public | BindingFlags.Static);

                foreach (var field in fields)
                {
                    var attr = field.GetCustomAttribute<InspectorNameAttribute>(false);
                    var enumLabel = new GUIContent(attr is null ? field.Name : attr.displayName);
                    var value = (int)field.GetRawConstantValue();
                    menu.AddItem(enumLabel, (value == (int)EasingCurveType.Custom), () =>
                    {
                        typeProperty.intValue = value;
                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();
                    });
                }

                menu.DropDown(dropdownRect);
            }
        }

        private static void DrawBezierControlPoints(SerializedProperty property, int count, float x, float y, float width)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var rect = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);

                for (int i = 1; i <= count; i++)
                {
                    SerializedProperty point = property.FindPropertyRelative($"m_ControlPoint{i}");
                    EditorGUI.PropertyField(rect, point, GUIContent.none);

                    // 确保 point 的 x 的范围在 [0, 1] 内
                    var pointX = point.FindPropertyRelative("x");
                    pointX.floatValue = Mathf.Clamp01(pointX.floatValue);

                    // 下一行
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }

        private static SerializedProperty GetCurveTypeProperty(SerializedProperty property, out EasingCurveType type)
        {
            SerializedProperty result = property.FindPropertyRelative("m_Type");
            type = (EasingCurveType)result.intValue;
            return result;
        }
    }
}