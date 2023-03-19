using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internals;
using ArgumentType = VentiCola.UI.Bindings.DynamicArgument.ArgumentType;
using Object = UnityEngine.Object;

namespace VentiColaEditor.UI.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(VoidMethodProxy<>))]
    public class VoidMethodProxyDrawer : PropertyDrawer
    {
        public const BindingFlags MethodBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        public const string MethodNameField = "m_MethodName";
        public const string TargetObjField = "m_Target";
        public const string ArgumentsField = "m_Arguments";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = 2;
            SerializedProperty methodName = property.FindPropertyRelative(MethodNameField);
            SerializedProperty targetObj = property.FindPropertyRelative(TargetObjField);

            if (!string.IsNullOrEmpty(methodName.stringValue) && targetObj.objectReferenceValue)
            {
                Type pageType = targetObj.objectReferenceValue.GetType();
                MethodInfo method = pageType.GetMethod(methodName.stringValue, MethodBindingFlags);
                ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
                lineCount += Mathf.Max(parameters.Length - 1, 0);
            }

            return lineCount * EditorGUIUtility.singleLineHeight
                + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            SerializedProperty methodName = property.FindPropertyRelative(MethodNameField);
            SerializedProperty targetObj = property.FindPropertyRelative(TargetObjField);
            SerializedProperty args = property.FindPropertyRelative(ArgumentsField);

            if (!targetObj.objectReferenceValue)
            {
                methodName.stringValue = null;
            }

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect popupRect = EditorGUI.PrefixLabel(rect, label);

            UIPage page = FindUIPage(property);
            Type pageType = page.GetType();
            GUIContent displayLabel = GetMethodDisplayContent(pageType, methodName.stringValue);

            if (EditorGUI.DropdownButton(popupRect, displayLabel, FocusType.Keyboard, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(GetMethodDisplayContent(pageType, null), string.IsNullOrEmpty(methodName.stringValue), () =>
                {
                    targetObj.objectReferenceValue = null;
                    methodName.stringValue = null;

                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                });
                menu.AddSeparator("");

                Type genericTypeArg = GetGenericTypeArgument();

                foreach (MethodInfo method in pageType.GetMethods(MethodBindingFlags))
                {
                    if (!FilterMethod(method, genericTypeArg))
                    {
                        continue;
                    }

                    menu.AddItem(GetMethodDisplayContent(method), (methodName.stringValue == method.Name), () =>
                    {
                        targetObj.objectReferenceValue = page;
                        methodName.stringValue = method.Name;

                        property.serializedObject.ApplyModifiedProperties();
                        property.serializedObject.Update();
                    });
                }

                menu.DropDown(popupRect);
            }

            if (!string.IsNullOrEmpty(methodName.stringValue))
            {
                MethodInfo method = pageType.GetMethod(methodName.stringValue, MethodBindingFlags);
                ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
                int displayArgCount = GetDisplayArgumentCount(parameters, out int startIndex);

                if (displayArgCount > 0)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (args.arraySize != displayArgCount)
                        {
                            args.arraySize = displayArgCount;
                        }

                        for (int i = 0; i < displayArgCount; i++)
                        {
                            rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                            DrawArgument(rect, parameters[startIndex + i], args.GetArrayElementAtIndex(i));
                        }
                    }
                }
                else
                {
                    args.ClearArray();
                }

                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                DrawMethodTip(rect, displayArgCount);
            }
            else
            {
                args.ClearArray();

                rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
                DrawMethodTip(rect, null);
            }

            EditorGUI.EndProperty();
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

        protected UIPage FindUIPage(SerializedProperty property)
        {
            var component = property.serializedObject.targetObject as Component;
            Assert.IsNotNull(component);

            GameObject go = component.gameObject;

            // 如果 go 是一个 PrefabAsset，那它永远不是 Active 的，
            // 所以找组件时需要把 includeInactive 设为 true
            return go.GetComponentInParent<UIPage>(true);
        }

        protected Type GetGenericTypeArgument()
        {
            Type type = fieldInfo.FieldType;
            Type genericTypeDefinition = GetGenericTypeDefinition();

            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                type = type.GenericTypeArguments[0];
            }

            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
                {
                    return type.GenericTypeArguments[0];
                }

                type = type.BaseType;
            }

            Debug.LogError("Invalid Type: " + fieldInfo.FieldType.Name);
            return null;
        }

        protected GUIContent GetMethodDisplayContent(Type pageType, string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return new GUIContent("No Method");
            }

            MethodInfo method = pageType.GetMethod(methodName, MethodBindingFlags);

            if (method is null)
            {
                return new GUIContent("Missing Method: " + methodName);
            }

            return GetMethodDisplayContent(method);
        }

        protected GUIContent GetMethodDisplayContent(MethodInfo method)
        {
            var returnName = TypeUtility.GetFriendlyTypeName(method.ReturnType, false);
            var paramNames = (
                from p in method.GetParameters()
                select TypeUtility.GetFriendlyTypeName(p.ParameterType, false)
            );
            return new GUIContent($"{returnName} {method.Name}({string.Join(", ", paramNames)})");
        }

        protected bool HasInvalidParameterType(ParameterInfo[] parameters, int startIndex)
        {
            for (int i = startIndex; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;

                if (type.IsByRef)
                {
                    return true;
                }
            }

            return false;
        }

        protected Type GetGenericTypeDefinition()
        {
            return typeof(VoidMethodProxy<>);
        }

        protected int GetDisplayArgumentCount(ParameterInfo[] parameters, out int startIndex)
        {
            startIndex = 1;
            return Mathf.Max(0, parameters.Length - 1);
        }

        protected bool FilterMethod(MethodInfo method, Type genericTypeArg)
        {
            if (method.IsGenericMethod || method.ReturnType != typeof(void))
            {
                return false;
            }

            // 有 SpecialName 的方法一般都是 Property 的 getter 或 setter
            if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length < 1 || parameters[0].ParameterType != genericTypeArg)
            {
                return false;
            }

            if (HasInvalidParameterType(parameters, 1))
            {
                return false;
            }

            return true;
        }

        protected void DrawMethodTip(Rect rect, int? argumentCount)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            style.normal.textColor = new Color32(0x88, 0x88, 0x88, 255);

            string tipContent;

            if (argumentCount.HasValue)
            {
                tipContent = $"{argumentCount} Extra Arguments";
            }
            else
            {
                Type argType = GetGenericTypeArgument();
                string argTypeName = TypeUtility.GetFriendlyTypeName(argType, false);
                tipContent = $"void (*)({argTypeName}, ...)";
            }

            EditorGUI.LabelField(rect, tipContent, style);
        }
    }
}