using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using VentiCola.UI;
using VentiCola.UI.Bindings;
using VentiCola.UI.Internals;
using VentiColaEditor.UI.Settings;

// TODO: Add more tooltips? for example, for global models

namespace VentiColaEditor.UI
{
    internal class AdvancedPropertyDropdownItem : AdvancedDropdownItem
    {
        public string[] Path { get; }

        public Component SourceObj { get; }

        public Type Type { get; }

        public bool IsMethod { get; }

        public AdvancedPropertyDropdownItem(string name, string[] path, Component sourceObj, Type type, bool isMethod, string tooltip) : base(name)
        {
            Path = path;
            SourceObj = sourceObj;
            Type = type;
            IsMethod = isMethod;

            icon = (Texture2D)EditorGUIUtility.IconContent(true switch
            {
                _ when isMethod => "sv_icon_dot7_pix16_gizmo",
                _ when type.IsValueType => "sv_icon_dot1_pix16_gizmo",
                _ => "sv_icon_dot0_pix16_gizmo"
            }).image;
            GetType().GetProperty("tooltip", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, tooltip);
        }
    }

    internal class AdvancedPropertyDropdown : AdvancedDropdown
    {
        private readonly Type[][] m_TypeConstraints;
        private readonly Transform m_Transform;
        private readonly bool m_SelectMethods;

        public Vector2 MinSize
        {
            get => minimumSize;
            set => minimumSize = value;
        }

        public event Action<string[], Component, Type, bool> OnItemSelcted;

        public AdvancedPropertyDropdown(Type[][] constraints, Transform transform, bool selectMethods, AdvancedDropdownState state) : base(state)
        {
            m_TypeConstraints = constraints;
            m_Transform = transform;
            m_SelectMethods = selectMethods;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Scopes");
            var globals = new AdvancedDropdownItem("Global Definitions")
            {
                icon = (Texture2D)EditorGUIUtility.IconContent("Folder Icon").image
            };
            var locals = new AdvancedDropdownItem("Local GameObjects")
            {
                icon = (Texture2D)EditorGUIUtility.IconContent("Folder Icon").image
            };
            var methods = new AdvancedDropdownItem("Local Methods")
            {
                icon = (Texture2D)EditorGUIUtility.IconContent("Folder Icon").image
            };

            AddGlobalModels(globals);
            AddLocalVars(locals);

            if (m_SelectMethods)
            {
                AddLocalMethods(methods);
            }

            if (HasChildren(globals, out _))
            {
                root.AddChild(globals);
            }

            if (HasChildren(locals, out _))
            {
                root.AddChild(locals);
            }

            if (HasChildren(methods, out _))
            {
                root.AddChild(methods);
            }

            return root;
        }

        private void AddGlobalModels(AdvancedDropdownItem parent)
        {
            UIProjectSettings settings = UIProjectSettings.instance;
            Stack<string> pathStack = new Stack<string>();

            foreach (var item in settings.GlobalModels)
            {
                AddModel(parent, item.Name, Type.GetType(item.TypeAssemblyQualifiedName), pathStack, null);
                Assert.AreEqual(0, pathStack.Count);
            }
        }

        private void AddLocalVars(AdvancedDropdownItem parent)
        {
            Transform transform = m_Transform;
            bool hasPage = false;

            // 要自己身上的 Local Var！

            while (transform && !hasPage)
            {
                ICustomScope[] scopes = transform.GetComponents<ICustomScope>();

                if (scopes.Length == 0)
                {
                    transform = transform.parent;
                    continue;
                }

                var objItem = new AdvancedDropdownItem(transform.gameObject.name)
                {
                    icon = (Texture2D)EditorGUIUtility.IconContent("GameObject Icon").image
                };

                foreach (var scope in scopes)
                {
                    var component = scope as Component;
                    hasPage |= component is UIPage;

                    var variables = scope.GetVarHintsInEditor();

                    if (variables.Length == 0)
                    {
                        continue;
                    }

                    foreach ((Type type, string name, string tooltip) in variables)
                    {
                        if (typeof(ReactiveModel).IsAssignableFrom(type))
                        {
                            AddModel(objItem, name, type, new Stack<string>(), component);
                        }
                        else if (MatchTypeConstraints(type))
                        {
                            objItem.AddChild(new AdvancedPropertyDropdownItem(name, new string[] { name }, component, type, false, tooltip));
                        }
                    }
                }

                if (HasChildren(objItem, out _))
                {
                    parent.AddChild(objItem);
                }

                transform = transform.parent;
            }

            // 遍历的时候是从下往上，但显示的时候从上往下会更符合直觉
            ReverseChildren(parent);
        }

        private void AddLocalMethods(AdvancedDropdownItem parent)
        {
            Transform transform = m_Transform;
            UIPage page = null;

            while (transform is not null)
            {
                if (transform.TryGetComponent(out page))
                {
                    break;
                }

                transform = transform.parent;
            }

            if (page == null)
            {
                return;
            }

            IEnumerable<MethodInfo> methods = (
                from method in page.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                group method by method.Name into methodGroup
                where methodGroup.Count() == 1 // 不能有重载！
                let fistMethod = methodGroup.First()
                where FilterMethod(fistMethod)
                select fistMethod
            );

            foreach (MethodInfo method in methods)
            {
                parent.AddChild(new AdvancedPropertyDropdownItem(
                    method.Name, new string[] { method.Name }, page, method.ReturnType, true, ""));
            }
        }

        private bool FilterMethod(MethodInfo method)
        {
            if (method.IsGenericMethod || method.ReturnType == typeof(void))
            {
                return false;
            }

            // 有 SpecialName 的方法一般都是 Property 的 getter 或 setter
            if (method.Attributes.HasFlag(MethodAttributes.SpecialName))
            {
                return false;
            }

            if (!MatchTypeConstraints(method.ReturnType))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.IsByRef)
                {
                    return false;
                }
            }

            return true;
        }

        private void AddModel(AdvancedDropdownItem parent, string modelName, Type modelType, Stack<string> pathStack, Component holder)
        {
            var modelItem = new AdvancedDropdownItem(modelName)
            {
                icon = (Texture2D)EditorGUIUtility.IconContent("ScriptableObject Icon").image
            };

            pathStack.Push(modelName);

            if (MatchTypeConstraints(modelType))
            {
                modelItem.AddChild(new AdvancedPropertyDropdownItem("Self&", pathStack.ToArray(), holder, modelType, false, "Refer to the model itself."));
                modelItem.AddSeparator();
            }

            foreach (PropertyInfo prop in modelType.GetProperties(PropertyReflectionUtility.PropertyBindingFlags))
            {
                if (!PropertyReflectionUtility.IsPublicReadableProperty(prop))
                {
                    continue;
                }

                if (typeof(ReactiveModel).IsAssignableFrom(prop.PropertyType))
                {
                    AddModel(modelItem, prop.Name, prop.PropertyType, pathStack, holder);
                }
                else if (MatchTypeConstraints(prop.PropertyType))
                {
                    pathStack.Push(prop.Name);
                    modelItem.AddChild(new AdvancedPropertyDropdownItem(prop.Name, pathStack.ToArray(), holder, prop.PropertyType, false, ""));
                    pathStack.Pop();
                }
            }

            pathStack.Pop();

            if (HasChildren(modelItem, out _))
            {
                parent.AddChild(modelItem);
            }
        }

        private static bool HasChildren(AdvancedDropdownItem item, out int childrenCount)
        {
            childrenCount = (item.children as List<AdvancedDropdownItem>).Count;
            return childrenCount > 0;
        }

        private static void ReverseChildren(AdvancedDropdownItem item)
        {
            (item.children as List<AdvancedDropdownItem>).Reverse();
        }

        private bool MatchTypeConstraints(Type type)
        {
            return (m_TypeConstraints.Length == 0) || m_TypeConstraints.Any(constraints =>
            {
                return constraints.All(constraintType =>
                {
                    if (constraintType.IsGenericTypeDefinition)
                    {
                        return TypeUtility.IsDerivedFromSpecificGenericType(type, constraintType, out _);
                    }
                    else
                    {
                        return constraintType.IsAssignableFrom(type);
                    }
                });
            });
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            if (item is AdvancedPropertyDropdownItem refItem)
            {
                OnItemSelcted?.Invoke(refItem.Path, refItem.SourceObj, refItem.Type, refItem.IsMethod);
            }
        }
    }

    [CustomPropertyDrawer(typeof(PropertyProxy))]
    internal class PropertyProxyDrawer : PropertyDrawer
    {
        public bool SingleLineDisplay
        {
            get
            {
                var attr = fieldInfo.GetCustomAttribute<PropertyProxyOptionsAttribute>();
                return attr?.CompactDisplay ?? false;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = SingleLineDisplay ? 1 : 2;
            return lineCount * EditorGUIUtility.singleLineHeight
                + (lineCount - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeConstraints = (
                from attr in fieldInfo.GetCustomAttributes<PropertyTypeConstraintsAttribute>(true)
                select attr.Constraints
            ).ToArray();

            DrawFieldGUI(position, property, label, typeConstraints, SingleLineDisplay);
        }

        public static string GetDisplayName(SerializedProperty path, SerializedProperty sourceObj, bool isMethod, out string tooltip)
        {
            if (path.arraySize == 0)
            {
                tooltip = "Nothing";
                return "<color=red><b>[N]</b></color> Nothing";
            }

            List<string> paths = new List<string>();

            for (int i = 0; i < path.arraySize; i++)
            {
                paths.Add(path.GetArrayElementAtIndex(i).stringValue);
            }

            if (isMethod)
            {
                tooltip = paths[0];
                return "<color=#2bf><b>[M]</b></color> " + tooltip;
            }

            if (sourceObj.objectReferenceValue is Component component)
            {
                tooltip = $"{component.gameObject.name} > {string.Join(" > ", paths)}";
                return "<color=#2bf><b>[L]</b></color> " + tooltip;
            }

            tooltip = string.Join(" > ", paths);
            return "<color=#2bf><b>[G]</b></color> " + tooltip;
        }

        public static void DrawFieldGUI(Rect position, SerializedProperty property, GUIContent label, Type[][] typeConstraints, bool forceSingleLine)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            position = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            var path = property.FindPropertyRelative("m_PropertyPath");
            var sourceObj = property.FindPropertyRelative("m_SourceObj");
            var typeName = property.FindPropertyRelative("m_TypeName");

            GUIStyle labelStyle;

            if (forceSingleLine)
            {
                labelStyle = new GUIStyle(EditorStyles.label) { richText = true };
                label.text += $" <color=#888888>({GetTypeConstraintsDisplayName(typeConstraints)})</color>";
            }
            else
            {
                labelStyle = EditorStyles.label;
            }

            var fieldRect = EditorGUI.PrefixLabel(position, label, labelStyle);
            var popupStyle = new GUIStyle(EditorStyles.popup) { richText = true };
            var displayContent = new GUIContent(GetDisplayName(path, sourceObj, false, out string displayTooltip), displayTooltip);

            if (EditorGUI.DropdownButton(fieldRect, displayContent, FocusType.Keyboard, popupStyle))
            {
                var transform = (property.serializedObject.targetObject as Component)?.transform;
                var dropdown = new AdvancedPropertyDropdown(typeConstraints, transform, false, new AdvancedDropdownState());
                dropdown.MinSize = new Vector2(dropdown.MinSize.x, 250);
                dropdown.OnItemSelcted += (pathArray, holderValue, typeValue, _) =>
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

                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                };
                dropdown.Show(fieldRect);
            }

            if (!forceSingleLine)
            {
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                DrawTypeConstraints(position, typeConstraints);
            }

            EditorGUI.EndProperty();
        }

        public static void DrawTypeConstraints(Rect rect, Type[][] constraints)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            style.normal.textColor = new Color32(0x88, 0x88, 0x88, 255);
            EditorGUI.LabelField(rect, GetTypeConstraintsDisplayName(constraints), style);
        }

        private static string GetTypeConstraintsDisplayName(Type[][] constraints)
        {
            HashSet<string> names = new HashSet<string>();

            for (int i = 0; i < constraints.Length; i++)
            {
                Type[] types = constraints[i];

                if (types.Length == 0)
                {
                    continue;
                }

                if (types.Length == 1)
                {
                    names.Add(TypeUtility.GetFriendlyTypeName(types[0], false));
                }
                else
                {
                    IEnumerable<string> typeNames = (
                        from type in types
                        let name = TypeUtility.GetFriendlyTypeName(type, false)
                        orderby name ascending
                        select name
                    );
                    names.Add($"({string.Join(" & ", typeNames)})");
                }
            }

            return (names.Count == 0) ? "object" : string.Join(" | ", names);
        }
    }
}