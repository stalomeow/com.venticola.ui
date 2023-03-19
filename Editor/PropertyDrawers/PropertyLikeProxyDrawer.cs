using System;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using ArgumentType = VentiCola.UI.Bindings.DynamicArgument.ArgumentType;
using Object = UnityEngine.Object;

namespace VentiColaEditor.UI.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(PropertyLikeProxy))]
    public class PropertyLikeProxyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = 2;

            var path = property.FindPropertyRelative("m_PropertyPath");
            var sourceObj = property.FindPropertyRelative("m_SourceObj");
            var isMethod = property.FindPropertyRelative("m_IsMethod");

            if (isMethod.boolValue && !string.IsNullOrEmpty(path.GetArrayElementAtIndex(0).stringValue) && sourceObj.objectReferenceValue)
            {
                Type pageType = sourceObj.objectReferenceValue.GetType();
                MethodInfo method = pageType.GetMethod(path.GetArrayElementAtIndex(0).stringValue, BindingFlags.Public | BindingFlags.Instance);
                ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
                lineCount += parameters.Length;
            }

            return lineCount * EditorGUIUtility.singleLineHeight
                + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            position = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            var path = property.FindPropertyRelative("m_PropertyPath");
            var sourceObj = property.FindPropertyRelative("m_SourceObj");
            var typeName = property.FindPropertyRelative("m_TypeName");
            var isMethod = property.FindPropertyRelative("m_IsMethod");
            var args = property.FindPropertyRelative("m_Arguments");

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            var popupStyle = new GUIStyle(EditorStyles.popup) { richText = true };
            var displayContent = new GUIContent(PropertyProxyDrawer.GetDisplayName(path, sourceObj, isMethod.boolValue, out string displayTooltip), displayTooltip);

            var typeConstraints = (
                from attr in fieldInfo.GetCustomAttributes<PropertyTypeConstraintsAttribute>(true)
                select attr.Constraints
            ).ToArray();

            if (EditorGUI.DropdownButton(fieldRect, displayContent, FocusType.Keyboard, popupStyle))
            {
                var transform = (property.serializedObject.targetObject as Component)?.transform;
                var dropdown = new AdvancedPropertyDropdown(typeConstraints, transform, true, new AdvancedDropdownState());
                dropdown.MinSize = new Vector2(dropdown.MinSize.x, 250);
                dropdown.OnItemSelcted += (pathArray, holderValue, typeValue, isMethodValue) =>
                {
                    Array.Reverse(pathArray);
                    path.ClearArray();

                    for (int i = 0; i < pathArray.Length; i++)
                    {
                        path.InsertArrayElementAtIndex(i);
                        SerializedProperty element = path.GetArrayElementAtIndex(i);
                        element.stringValue = pathArray[i];
                    }

                    sourceObj.objectReferenceValue = holderValue;
                    typeName.stringValue = typeValue.AssemblyQualifiedName;
                    isMethod.boolValue = isMethodValue;

                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                };
                dropdown.Show(fieldRect);
            }

            if (isMethod.boolValue)
            {
                Type pageType = FindUIPage(property).GetType();
                MethodInfo method = pageType.GetMethod(path.GetArrayElementAtIndex(0).stringValue, BindingFlags.Public | BindingFlags.Instance);
                ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();

                if (parameters.Length > 0)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (args.arraySize != parameters.Length)
                        {
                            args.arraySize = parameters.Length;
                        }

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                            DrawArgument(position, parameters[i], args.GetArrayElementAtIndex(i));
                        }
                    }
                }
                else
                {
                    args.ClearArray();
                }
            }
            else
            {
                args.ClearArray();
            }

            position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
            PropertyProxyDrawer.DrawTypeConstraints(position, typeConstraints);

            EditorGUI.EndProperty();
        }

        private UIPage FindUIPage(SerializedProperty property)
        {
            var component = property.serializedObject.targetObject as Component;
            Assert.IsNotNull(component);

            GameObject go = component.gameObject;

            // 如果 go 是一个 PrefabAsset，那它永远不是 Active 的，
            // 所以找组件时需要把 includeInactive 设为 true
            return go.GetComponentInParent<UIPage>(true);
        }

        private void DrawArgument(Rect rect, ParameterInfo parameterInfo, SerializedProperty property)
        {
            Type paramType = parameterInfo.ParameterType;

            if (paramType == typeof(Object))
            {
                DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.UnityObj, parameterInfo.Name, property);
            }
            else if (paramType == typeof(int))
            {
                Type customEnumType = null;
                var attr = parameterInfo.GetCustomAttribute<EditAsEnumAttribute>(true);

                if (attr is not null && attr.EnumType.IsEnum && attr.EnumType.GetEnumUnderlyingType() == typeof(int))
                {
                    customEnumType = attr.EnumType;
                }

                DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.Int32, parameterInfo.Name, property, customEnumType);
            }
            else if (paramType == typeof(float))
            {
                DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.Float32, parameterInfo.Name, property);
            }
            else if (paramType == typeof(string))
            {
                DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.String, parameterInfo.Name, property);
            }
            else if (paramType == typeof(bool))
            {
                DrawBoolTypeArgument(rect, parameterInfo.Name, property);
            }
            else
            {
                DrawPropertyProxyArgument(rect, paramType, parameterInfo.Name, property);
            }
        }

        private void DrawPrimitiveTypeArgument(Rect rect, Type type, ArgumentType typeEnum, string name, SerializedProperty property, Type customEnumType = null)
        {
            SerializedProperty argTypeProp = property.FindPropertyRelative("m_ArgType");
            ArgumentType argType = (ArgumentType)argTypeProp.intValue;

            if ((argType != typeEnum) && (argType != ArgumentType.Property))
            {
                argTypeProp.intValue = (int)typeEnum;
                argType = typeEnum;
            }

            const float switchBtnWidth = 16f;
            const float switchBtnPadding = 2f;

            Rect fieldRect = new Rect(rect.x, rect.y, rect.width - switchBtnWidth - switchBtnPadding, rect.height);
            Rect switchRect = new Rect(fieldRect.xMax + switchBtnPadding, rect.y, switchBtnWidth, rect.height);

            if (argType == typeEnum)
            {
                SerializedProperty p = property.FindPropertyRelative(typeEnum switch
                {
                    ArgumentType.UnityObj => "m_UnityObjArg",
                    ArgumentType.Int32 => "m_NumberArg",
                    ArgumentType.Float32 => "m_NumberArg",
                    ArgumentType.String => "m_StringArg",
                    _ => throw new NotImplementedException(),
                });

                if (typeEnum == ArgumentType.Int32 && customEnumType is not null)
                {
                    Enum enumValue = (Enum)Enum.ToObject(customEnumType, p.intValue);
                    Enum newValue;

                    if (customEnumType.GetCustomAttribute<FlagsAttribute>() is null)
                    {
                        newValue = EditorGUI.EnumPopup(fieldRect, new GUIContent(name), enumValue);
                    }
                    else
                    {
                        newValue = EditorGUI.EnumFlagsField(fieldRect, new GUIContent(name), enumValue);
                    }

                    p.intValue = Convert.ToInt32(newValue);
                }
                else if (typeEnum == ArgumentType.Float32)
                {
                    int intValue = p.intValue;
                    float floatValue = UnsafeUtility.As<int, float>(ref intValue);
                    floatValue = EditorGUI.FloatField(rect, name, floatValue);
                    p.intValue = UnsafeUtility.As<float, int>(ref floatValue);
                }
                else
                {
                    EditorGUI.PropertyField(fieldRect, p, new GUIContent(name));
                }
            }
            else
            {
                SerializedProperty p = property.FindPropertyRelative("m_PropertyArg");
                PropertyProxyDrawer.DrawFieldGUI(fieldRect, p, new GUIContent(name), new Type[][] { new Type[] { type } }, true);
            }

            bool constMode = (argType == typeEnum);
            GUIContent switchIcon = new GUIContent(EditorGUIUtility.IconContent(constMode ? "d_Unlinked" : "d_Linked").image);

            if (GUI.Button(switchRect, switchIcon, "IconButton"))
            {
                argTypeProp.intValue = constMode
                    ? (int)ArgumentType.Property
                    : (int)typeEnum;
            }
        }

        private void DrawBoolTypeArgument(Rect rect, string name, SerializedProperty property)
        {
            SerializedProperty argTypeProp = property.FindPropertyRelative("m_ArgType");
            ArgumentType argType = (ArgumentType)argTypeProp.intValue;

            if (argType is not ArgumentType.True or ArgumentType.False or ArgumentType.Property)
            {
                argTypeProp.intValue = (int)ArgumentType.False;
                argType = ArgumentType.False;
            }

            const float switchBtnWidth = 16f;
            const float switchBtnPadding = 2f;

            Rect fieldRect = new Rect(rect.x, rect.y, rect.width - switchBtnWidth - switchBtnPadding, rect.height);
            Rect switchRect = new Rect(fieldRect.xMax + switchBtnPadding, rect.y, switchBtnWidth, rect.height);

            if (argType == ArgumentType.Property)
            {
                SerializedProperty p = property.FindPropertyRelative("m_PropertyArg");
                PropertyProxyDrawer.DrawFieldGUI(fieldRect, p, new GUIContent(name), new Type[][] { new Type[] { typeof(bool) } }, true);
            }
            else
            {
                bool newValue = EditorGUI.Toggle(rect, name, argType == ArgumentType.True);
                argTypeProp.intValue = (int)(newValue ? ArgumentType.True : ArgumentType.False);
            }

            bool constMode = (argType != ArgumentType.Property);
            GUIContent switchIcon = new GUIContent(EditorGUIUtility.IconContent(constMode ? "d_Unlinked" : "d_Linked").image);

            if (GUI.Button(switchRect, switchIcon, "IconButton"))
            {
                argTypeProp.intValue = constMode
                    ? (int)ArgumentType.Property
                    : (int)ArgumentType.False;
            }
        }

        private void DrawPropertyProxyArgument(Rect rect, Type type, string name, SerializedProperty property)
        {
            property.FindPropertyRelative("m_ArgType").intValue = (int)ArgumentType.Property;

            SerializedProperty p = property.FindPropertyRelative("m_PropertyArg");
            PropertyProxyDrawer.DrawFieldGUI(rect, p, new GUIContent(name), new Type[][] { new Type[] { type } }, true);
        }
    }
}