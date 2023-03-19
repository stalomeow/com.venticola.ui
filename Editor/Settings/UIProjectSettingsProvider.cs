using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using VentiCola.UI;
using VentiColaEditor.UI.CodeInjection;

namespace VentiColaEditor.UI.Settings
{
    internal class UIProjectSettingsProvider : SettingsProvider
    {
        private SerializedObject m_SerializedObject;
        private SerializedProperty m_GlobalModels;
        private SerializedProperty m_AutoCodeInjection;
        private SerializedProperty m_CodeInjectionTasks;
        private SerializedProperty m_CodeInjectionLogLevel;
        private SerializedProperty m_CodeInjectionAssemblyWhiteList;

        private ReorderableList m_GlobalModelsList;
        private ReorderableList m_CIAssemblyWhiteList; // for m_CodeInjectionAssemblyWhiteList

        public UIProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
            guiHandler = OnGUIHandler;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            UIProjectSettings.instance.Save();
            m_SerializedObject = UIProjectSettings.instance.AsSerializedObject();

            // initialize properities
            FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    SerializedProperty property = m_SerializedObject.FindProperty(field.Name);
                    field.SetValue(this, property);
                }
            }
        }

        private void OnGUIHandler(string searchContext)
        {
            m_SerializedObject.Update();
            EditorGUI.BeginChangeCheck();

            using (EditorGUIUtils.NewSettingsWindowGUIScope())
            {
                EditorGUILayout.LabelField("Runtime Settings", EditorStyles.boldLabel);

                if (DrawRuntimeSettingsObjField(out UIRuntimeSettings settings))
                {
                    GUILayout.Space(1);

                    if (GUILayout.Button("Edit Runtime Settings", GUILayout.Height(30)))
                    {
                        FocusOnObject(settings);
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);
                DrawGlobalModelsField();

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Code Injection", EditorStyles.boldLabel);
                DrawCodeInjectionTasksField();
                DrawCodeInjectionLogLevelField();
                DrawEditorOptionsFields();
                EditorGUILayout.Space(1);
                DrawCodeInjectionAssemblyWhiteListField();

                EditorGUILayout.Space();
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedObject.ApplyModifiedProperties();
                UIProjectSettings.instance.Save();
            }
        }

        private bool DrawRuntimeSettingsObjField(out UIRuntimeSettings settings)
        {
            settings = UIRuntimeSettings.FindInstance();

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Runtime Settings are stored in an asset. Click the button below to create a settings asset.", MessageType.Error);

                if (GUILayout.Button("Create Runtime Settings", GUILayout.Height(30)))
                {
                    CreateNewSettingsAsset();
                }

                return false;
            }

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField(GUIContent.none, settings, typeof(UIRuntimeSettings), false);
            }

            return true;
        }

        private void CreateNewSettingsAsset()
        {
            var defaultName = PlayerSettings.productName + "-UIRuntimeSettings";
            var path = EditorUtility.SaveFilePanel("Create UI Runtime Settings File", "Assets", defaultName, "asset");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // Make sure the path is in the Assets/ folder.
            path = path.Replace("\\", "/"); // Make sure we only get '/' separators.
            var dataPath = Application.dataPath + "/";
            if (!path.StartsWith(dataPath, StringComparison.CurrentCultureIgnoreCase))
            {
                Debug.LogError($"UI Runtime Settings must be stored in Assets folder of the project (got: '{path}')");
                return;
            }

            // Make sure it ends with .asset.
            var extension = Path.GetExtension(path);
            if (string.Compare(extension, ".asset", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                path += ".asset";
            }

            // Create settings file.
            CreateNewSettingsAsset("Assets/" + path[dataPath.Length..]);
        }

        private void CreateNewSettingsAsset(string relativePath)
        {
            var settings = ScriptableObject.CreateInstance<UIRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, relativePath);
            UIRuntimeSettings.SetInstance(settings);

            FocusOnObject(settings);
        }

        private void DrawGlobalModelsField()
        {
            m_GlobalModelsList ??= new ReorderableList(m_SerializedObject, m_GlobalModels, false, false, true, true)
            {
                multiSelect = false,
                elementHeight = EditorGUIUtility.singleLineHeight,
                drawNoneElementCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "No Definition Found");
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var prop = m_GlobalModels.GetArrayElementAtIndex(index);
                    var name = prop.FindPropertyRelative(nameof(GlobalModelVar.Name));
                    var typeName = prop.FindPropertyRelative(nameof(GlobalModelVar.TypeAssemblyQualifiedName));
                    var type = Type.GetType(typeName.stringValue);

                    rect.height = EditorGUIUtility.singleLineHeight;
                    name.stringValue = EditorGUI.TextField(rect, $"Model Definition {index}", name.stringValue);

                    GUIStyle style = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleRight,
                        richText = true
                    };
                    EditorGUI.LabelField(rect, $"<color=#888888> ({type.Name}) </color>", style);
                },
                onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
                {
                    IEnumerable<Type> types = (
                        from type in TypeCache.GetTypesDerivedFrom<ReactiveModel>()
                        where !type.IsGenericType // the types of properties in generic types may be undetermined.
                        select type
                    );

                    var menu = new GenericMenu();

                    foreach (Type type in types)
                    {
                        string itemName = new Regex(@"[\.\+]").Replace(type.FullName, "/");
                        menu.AddItem(new GUIContent(itemName), false, obj =>
                        {
                            var array = list.serializedProperty;
                            array.InsertArrayElementAtIndex(array.arraySize);

                            var prop = array.GetArrayElementAtIndex(array.arraySize - 1);
                            var name = prop.FindPropertyRelative(nameof(GlobalModelVar.Name));
                            var type = prop.FindPropertyRelative(nameof(GlobalModelVar.TypeAssemblyQualifiedName));
                            name.stringValue = (obj as Type).Name;
                            type.stringValue = (obj as Type).AssemblyQualifiedName;

                            array.serializedObject.ApplyModifiedProperties();
                            GUI.changed = true;
                        }, type);
                    }

                    menu.DropDown(buttonRect);
                }
            };

            m_GlobalModels.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_GlobalModels.isExpanded, "Global Model Definition");

            if (m_GlobalModels.isExpanded)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, m_GlobalModelsList.GetHeight());
                m_GlobalModelsList.DoList(EditorGUI.IndentedRect(rect));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawEditorOptionsFields()
        {
            EditorGUILayout.LabelField("Editor Options");

            using (new EditorGUI.IndentLevelScope())
            {
                var oldValue = m_AutoCodeInjection.boolValue;
                var newValue = EditorGUILayout.Toggle("Execute After Compilation", oldValue);
                m_AutoCodeInjection.boolValue = newValue;
            }
        }

        private void DrawCodeInjectionTasksField()
        {
            var oldTasks = (InjectionTasks)m_CodeInjectionTasks.intValue;
            var newTasks = (InjectionTasks)EditorGUILayout.EnumFlagsField("Tasks", oldTasks);
            m_CodeInjectionTasks.intValue = (int)newTasks;
        }

        private void DrawCodeInjectionLogLevelField()
        {
            var oldLevel = (LogLevel)m_CodeInjectionLogLevel.intValue;
            var newLevel = (LogLevel)EditorGUILayout.EnumFlagsField("Log Level", oldLevel);
            m_CodeInjectionLogLevel.intValue = (int)newLevel;
        }

        private void DrawCodeInjectionAssemblyWhiteListField()
        {
            m_CIAssemblyWhiteList ??= new ReorderableList(m_SerializedObject, m_CodeInjectionAssemblyWhiteList, false, true, true, false)
            {
                multiSelect = false,
                elementHeight = EditorGUIUtility.singleLineHeight,
                drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Assembly White List"),
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    const float removeButtonWidth = 20;

                    GUIStyle buttonStyle = ReorderableList.defaultBehaviours.preButton;
                    float buttonHeightDelta = rect.height - buttonStyle.lineHeight;
                    Rect buttonRect = new Rect(rect.x, rect.y + buttonHeightDelta / 2, removeButtonWidth, rect.height - buttonHeightDelta);
                    GUIContent buttonIcon = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove from the list");

                    if (GUI.Button(buttonRect, buttonIcon, buttonStyle))
                    {
                        m_CodeInjectionAssemblyWhiteList.DeleteArrayElementAtIndex(index);
                        return;
                    }

                    // I dont known why, but sometimes this will happen.
                    if (index >= m_CodeInjectionAssemblyWhiteList.arraySize)
                    {
                        return;
                    }

                    var prop = m_CodeInjectionAssemblyWhiteList.GetArrayElementAtIndex(index);
                    rect.xMin += removeButtonWidth + 2;
                    EditorGUI.LabelField(rect, new GUIContent(prop.stringValue, EditorGUIUtility.FindTexture("Assembly Icon")));
                },
                onSelectCallback = (ReorderableList list) =>
                {
                    int index = list.selectedIndices[0];
                    SerializedProperty prop = list.serializedProperty.GetArrayElementAtIndex(index);
                    string path = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(prop.stringValue);

                    if (path is not null)
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                        EditorGUIUtility.PingObject(asset);
                    }
                },
                onAddDropdownCallback = (Rect buttonRect, ReorderableList list) =>
                {
                    IEnumerable<string> assemblies = (
                        from assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player)
                        orderby assembly.name ascending
                        select assembly.name
                    );

                    var menu = new GenericMenu();
                    var whitelist = new HashSet<string>(UIProjectSettings.instance.CodeInjectionAssemblyWhiteList);

                    foreach (var assemblyName in assemblies)
                    {
                        string name = assemblyName;
                        bool selected = whitelist.Contains(name);
                        menu.AddItem(new GUIContent(name), selected, selected ? null : () =>
                        {
                            var array = list.serializedProperty;
                            array.InsertArrayElementAtIndex(array.arraySize);

                            var prop = array.GetArrayElementAtIndex(array.arraySize - 1);
                            prop.stringValue = name;

                            array.serializedObject.ApplyModifiedProperties();
                            GUI.changed = true;
                        });
                    }

                    menu.DropDown(buttonRect);
                }
            };

            // 自己获取的 rect 比 ReorderableList 获取的 rect 宽度稍窄一点
            Rect rect = EditorGUILayout.GetControlRect(false, m_CIAssemblyWhiteList.GetHeight());
            m_CIAssemblyWhiteList.DoList(EditorGUI.IndentedRect(rect));
        }

        private static void FocusOnObject(UnityEngine.Object obj)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        [SettingsProvider]
        public static SettingsProvider CreateAssetProjectSettingsProvider()
        {
            return new UIProjectSettingsProvider(UIProjectSettings.PathInProjectSettings, SettingsScope.Project);
        }
    }
}