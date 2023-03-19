using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Events;
using Object = UnityEngine.Object;

namespace VentiColaEditor.UI
{
    //internal abstract class EventDelegateDrawer : PropertyDrawer
    //{
    //    public const BindingFlags MethodBindingFlags = BindingFlags.Public | BindingFlags.Instance;
    //    public const string MethodNameField = "m_MethodName";
    //    public const string TargetObjField = "m_TargetObj";
    //    public const string ArgumentsField = "m_Arguments";

    //    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    //    {
    //        int lineCount = 2;
    //        SerializedProperty methodName = property.FindPropertyRelative(MethodNameField);
    //        SerializedProperty targetObj = property.FindPropertyRelative(TargetObjField);

    //        if (!string.IsNullOrEmpty(methodName.stringValue) && targetObj.objectReferenceValue)
    //        {
    //            Type pageType = targetObj.objectReferenceValue.GetType();
    //            MethodInfo method = pageType.GetMethod(methodName.stringValue, MethodBindingFlags);
    //            ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
    //            lineCount += GetDisplayArgumentCount(parameters, out _);
    //        }

    //        return lineCount * EditorGUIUtility.singleLineHeight
    //            + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
    //    }

    //    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //    {
    //        label = EditorGUI.BeginProperty(position, label, property);

    //        SerializedProperty methodName = property.FindPropertyRelative(MethodNameField);
    //        SerializedProperty targetObj = property.FindPropertyRelative(TargetObjField);
    //        SerializedProperty args = property.FindPropertyRelative(ArgumentsField);

    //        if (!targetObj.objectReferenceValue)
    //        {
    //            methodName.stringValue = null;
    //        }

    //        Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
    //        Rect popupRect = EditorGUI.PrefixLabel(rect, label);

    //        UIPage page = FindUIPage(property);
    //        Type pageType = page.GetType();
    //        GUIContent displayLabel = GetMethodDisplayContent(pageType, methodName.stringValue);

    //        if (EditorGUI.DropdownButton(popupRect, displayLabel, FocusType.Keyboard, EditorStyles.popup))
    //        {
    //            GenericMenu menu = new GenericMenu();

    //            menu.AddItem(GetMethodDisplayContent(pageType, null), string.IsNullOrEmpty(methodName.stringValue), () =>
    //            {
    //                targetObj.objectReferenceValue = null;
    //                methodName.stringValue = null;

    //                property.serializedObject.ApplyModifiedProperties();
    //                property.serializedObject.Update();
    //            });
    //            menu.AddSeparator("");

    //            Type genericTypeArg = GetGenericTypeArgument();

    //            foreach (MethodInfo method in pageType.GetMethods(MethodBindingFlags))
    //            {
    //                if (!FilterMethod(method, genericTypeArg))
    //                {
    //                    continue;
    //                }

    //                menu.AddItem(GetMethodDisplayContent(method), (methodName.stringValue == method.Name), () =>
    //                {
    //                    targetObj.objectReferenceValue = page;
    //                    methodName.stringValue = method.Name;

    //                    property.serializedObject.ApplyModifiedProperties();
    //                    property.serializedObject.Update();
    //                });
    //            }

    //            menu.DropDown(popupRect);
    //        }

    //        if (!string.IsNullOrEmpty(methodName.stringValue))
    //        {
    //            MethodInfo method = pageType.GetMethod(methodName.stringValue, MethodBindingFlags);
    //            ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
    //            int displayArgCount = GetDisplayArgumentCount(parameters, out int startIndex);

    //            if (displayArgCount > 0)
    //            {
    //                using (new EditorGUI.IndentLevelScope())
    //                {
    //                    if (args.arraySize != displayArgCount)
    //                    {
    //                        args.arraySize = displayArgCount;
    //                    }

    //                    for (int i = 0; i < displayArgCount; i++)
    //                    {
    //                        rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
    //                        DrawArgument(rect, parameters[startIndex + i], args.GetArrayElementAtIndex(i));
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                args.ClearArray();
    //            }

    //            rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
    //            DrawMethodTip(rect, displayArgCount);
    //        }
    //        else
    //        {
    //            args.ClearArray();

    //            rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
    //            DrawMethodTip(rect, null);
    //        }

    //        EditorGUI.EndProperty();
    //    }

    //    protected void DrawArgument(Rect rect, ParameterInfo parameterInfo, SerializedProperty property)
    //    {
    //        Type paramType = parameterInfo.ParameterType;

    //        if (paramType == typeof(Object))
    //        {
    //            DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.UnityObj, parameterInfo.Name, property);
    //        }
    //        else if (paramType == typeof(int))
    //        {
    //            Type customEnumType = null;
    //            var attr = parameterInfo.GetCustomAttribute<EditAsEnumAttribute>(true);

    //            if (attr is not null && attr.EnumType.IsEnum && attr.EnumType.GetEnumUnderlyingType() == typeof(int))
    //            {
    //                customEnumType = attr.EnumType;
    //            }

    //            DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.Integer, parameterInfo.Name, property, customEnumType);
    //        }
    //        else if (paramType == typeof(float))
    //        {
    //            DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.Float, parameterInfo.Name, property);
    //        }
    //        else if (paramType == typeof(string))
    //        {
    //            DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.String, parameterInfo.Name, property);
    //        }
    //        else if (paramType == typeof(bool))
    //        {
    //            DrawPrimitiveTypeArgument(rect, paramType, ArgumentType.Boolean, parameterInfo.Name, property);
    //        }
    //        else
    //        {
    //            DrawPropertyProxyArgument(rect, paramType, parameterInfo.Name, property);
    //        }
    //    }

    //    protected void DrawPrimitiveTypeArgument(Rect rect, Type type, ArgumentType typeEnum, string name, SerializedProperty property, Type customEnumType = null)
    //    {
    //        SerializedProperty argTypeProp = property.FindPropertyRelative("Type");
    //        ArgumentType argType = (ArgumentType)argTypeProp.intValue;

    //        if ((argType != typeEnum) && (argType != ArgumentType.Property))
    //        {
    //            argTypeProp.intValue = (int)typeEnum;
    //            argType = typeEnum;
    //        }

    //        const float switchBtnWidth = 16f;
    //        const float switchBtnPadding = 2f;

    //        Rect fieldRect = new Rect(rect.x, rect.y, rect.width - switchBtnWidth - switchBtnPadding, rect.height);
    //        Rect switchRect = new Rect(fieldRect.xMax + switchBtnPadding, rect.y, switchBtnWidth, rect.height);

    //        if (argType == typeEnum)
    //        {
    //            SerializedProperty p = property.FindPropertyRelative(typeEnum switch
    //            {
    //                ArgumentType.UnityObj => "ObjArg",
    //                ArgumentType.Integer => "IntArg",
    //                ArgumentType.Float => "FloatArg",
    //                ArgumentType.String => "StringArg",
    //                ArgumentType.Boolean => "BoolArg",
    //                _ => throw new NotImplementedException(),
    //            });

    //            if (typeEnum == ArgumentType.Integer && customEnumType is not null)
    //            {
    //                Enum enumValue = (Enum)Enum.ToObject(customEnumType, p.intValue);
    //                Enum newValue;

    //                if (customEnumType.GetCustomAttribute<FlagsAttribute>() is null)
    //                {
    //                    newValue = EditorGUI.EnumPopup(fieldRect, new GUIContent(name), enumValue);
    //                }
    //                else
    //                {
    //                    newValue = EditorGUI.EnumFlagsField(fieldRect, new GUIContent(name), enumValue);
    //                }

    //                p.intValue = Convert.ToInt32(newValue);
    //            }
    //            else
    //            {
    //                EditorGUI.PropertyField(fieldRect, p, new GUIContent(name));
    //            }
    //        }
    //        else
    //        {
    //            SerializedProperty p = property.FindPropertyRelative("VarRefArg");
    //            PropertyProxyDrawer.DrawFieldGUI(fieldRect, p, new GUIContent(name), new Type[][] { new Type[] { type } }, true);
    //        }

    //        bool constMode = (argType == typeEnum);
    //        GUIContent switchIcon = new GUIContent(EditorGUIUtility.IconContent(constMode ? "d_Unlinked" : "d_Linked").image);

    //        if (GUI.Button(switchRect, switchIcon, "IconButton"))
    //        {
    //            argTypeProp.intValue = constMode
    //                ? (int)ArgumentType.Property
    //                : (int)typeEnum;
    //        }
    //    }

    //    protected void DrawPropertyProxyArgument(Rect rect, Type type, string name, SerializedProperty property)
    //    {
    //        property.FindPropertyRelative("Type").intValue = (int)ArgumentType.Property;

    //        SerializedProperty p = property.FindPropertyRelative("VarRefArg");
    //        PropertyProxyDrawer.DrawFieldGUI(rect, p, new GUIContent(name), new Type[][] { new Type[] { type } }, true);
    //    }

    //    protected UIPage FindUIPage(SerializedProperty property)
    //    {
    //        // PrefabUtility.
    //        GameObject go = (property.serializedObject.targetObject as Component)?.gameObject;
    //        return go.GetComponentInParent<UIPage>();
    //    }

    //    protected Type GetGenericTypeArgument()
    //    {
    //        Type type = fieldInfo.FieldType;
    //        Type genericTypeDefinition = GetGenericTypeDefinition();

    //        if (type.IsArray)
    //        {
    //            type = type.GetElementType();
    //        }
    //        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
    //        {
    //            type = type.GenericTypeArguments[0];
    //        }

    //        while (type != null)
    //        {
    //            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
    //            {
    //                return type.GenericTypeArguments[0];
    //            }

    //            type = type.BaseType;
    //        }

    //        Debug.LogError("Invalid Type: " + fieldInfo.FieldType.Name);
    //        return null;
    //    }

    //    protected GUIContent GetMethodDisplayContent(Type pageType, string methodName)
    //    {
    //        if (string.IsNullOrEmpty(methodName))
    //        {
    //            return new GUIContent("No Method");
    //        }

    //        MethodInfo method = pageType.GetMethod(methodName, MethodBindingFlags);

    //        if (method is null)
    //        {
    //            return new GUIContent("Missing Method: " + methodName);
    //        }

    //        return GetMethodDisplayContent(method);
    //    }

    //    protected GUIContent GetMethodDisplayContent(MethodInfo method)
    //    {
    //        var returnName = TypeUtility.GetFriendlyTypeName(method.ReturnType, false);
    //        var paramNames = (
    //            from p in method.GetParameters()
    //            select TypeUtility.GetFriendlyTypeName(p.ParameterType, false)
    //        );
    //        return new GUIContent($"{returnName} {method.Name}({string.Join(", ", paramNames)})");
    //    }

    //    protected bool HasInvalidParameterType(ParameterInfo[] parameters, int startIndex)
    //    {
    //        for (int i = startIndex; i < parameters.Length; i++)
    //        {
    //            Type type = parameters[i].ParameterType;

    //            if (type.IsByRef)
    //            {
    //                return true;
    //            }
    //        }

    //        return false;
    //    }

    //    protected abstract Type GetGenericTypeDefinition();

    //    protected abstract int GetDisplayArgumentCount(ParameterInfo[] parameters, out int startIndex);

    //    protected abstract bool FilterMethod(MethodInfo method, Type genericTypeArg);

    //    protected abstract void DrawMethodTip(Rect rect, int? argumentCount);
    //}

    //[CustomPropertyDrawer(typeof(UIEventHandler<>), true)]
    //internal class UIEventHandlerDrawer : EventDelegateDrawer
    //{
    //    protected override Type GetGenericTypeDefinition()
    //    {
    //        return typeof(UIEventHandler<>);
    //    }

    //    protected override int GetDisplayArgumentCount(ParameterInfo[] parameters, out int startIndex)
    //    {
    //        startIndex = 1;
    //        return Mathf.Max(0, parameters.Length - 1);
    //    }

    //    protected override bool FilterMethod(MethodInfo method, Type genericTypeArg)
    //    {
    //        if (method.IsGenericMethod || method.ReturnType != typeof(void))
    //        {
    //            return false;
    //        }

    //        // 有 SpecialName 的方法一般都是 Property 的 getter 或 setter
    //        if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
    //        {
    //            return false;
    //        }

    //        ParameterInfo[] parameters = method.GetParameters();

    //        if (parameters.Length < 1 || parameters[0].ParameterType != genericTypeArg)
    //        {
    //            return false;
    //        }

    //        if (HasInvalidParameterType(parameters, 1))
    //        {
    //            return false;
    //        }

    //        return true;
    //    }

    //    protected override void DrawMethodTip(Rect rect, int? argumentCount)
    //    {
    //        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
    //        {
    //            alignment = TextAnchor.MiddleRight
    //        };
    //        style.normal.textColor = new Color32(0x88, 0x88, 0x88, 255);

    //        string tipContent;

    //        if (argumentCount.HasValue)
    //        {
    //            tipContent = $"{argumentCount} Extra Arguments";
    //        }
    //        else
    //        {
    //            Type argType = GetGenericTypeArgument();
    //            string argTypeName = TypeUtility.GetFriendlyTypeName(argType, false);
    //            tipContent = $"void (*)({argTypeName}, ...)";
    //        }

    //        EditorGUI.LabelField(rect, tipContent, style);
    //    }
    //}

    //[CustomPropertyDrawer(typeof(ComputeFunc<>), true)]
    //internal class ComputeFuncDrawer : EventDelegateDrawer
    //{
    //    protected override Type GetGenericTypeDefinition()
    //    {
    //        return typeof(ComputeFunc<>);
    //    }

    //    protected override int GetDisplayArgumentCount(ParameterInfo[] parameters, out int startIndex)
    //    {
    //        startIndex = 0;
    //        return parameters.Length;
    //    }

    //    protected override bool FilterMethod(MethodInfo method, Type genericTypeArg)
    //    {
    //        if (method.IsGenericMethod || method.ReturnType != genericTypeArg)
    //        {
    //            return false;
    //        }

    //        // 有 SpecialName 的方法一般都是 Property 的 getter 或 setter
    //        if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
    //        {
    //            return false;
    //        }

    //        ParameterInfo[] parameters = method.GetParameters();

    //        if (HasInvalidParameterType(parameters, 0))
    //        {
    //            return false;
    //        }

    //        return true;
    //    }

    //    protected override void DrawMethodTip(Rect rect, int? argumentCount)
    //    {
    //        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
    //        {
    //            alignment = TextAnchor.MiddleRight
    //        };
    //        style.normal.textColor = new Color32(0x88, 0x88, 0x88, 255);

    //        string tipContent;

    //        if (argumentCount.HasValue)
    //        {
    //            tipContent = $"{argumentCount} Arguments";
    //        }
    //        else
    //        {
    //            Type returnType = GetGenericTypeArgument();
    //            string returnTypeName = TypeUtility.GetFriendlyTypeName(returnType, false);
    //            tipContent = $"{returnTypeName} (*)(...)";
    //        }

    //        EditorGUI.LabelField(rect, tipContent, style);
    //    }
    //}
}