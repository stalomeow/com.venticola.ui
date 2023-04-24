using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private SerializedProperty m_AutoCodeInjection;
        private SerializedProperty m_EnableCodeInjectionLog;
        private SerializedProperty m_CodeInjectionAssemblyWhiteList;

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

            using (CustomEditorGUIUtils.NewSettingsWindowGUIScope())
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

                EditorGUILayout.LabelField("Code Injection", EditorStyles.boldLabel);
                DrawEnableCodeInjectionLogField();
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
            settings = UIRuntimeSettings.Instance;

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

        private void DrawEditorOptionsFields()
        {
            var oldValue = m_AutoCodeInjection.boolValue;
            var newValue = EditorGUILayout.Toggle("Execute Automatically", oldValue);
            m_AutoCodeInjection.boolValue = newValue;
        }

        private void DrawEnableCodeInjectionLogField()
        {
            var oldValue = m_EnableCodeInjectionLog.boolValue;
            var newValue = EditorGUILayout.Toggle("Enable Log", oldValue);
            m_EnableCodeInjectionLog.boolValue = newValue;
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