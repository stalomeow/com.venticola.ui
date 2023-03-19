using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using VentiCola.UI;
using VentiCola.UI.Factories;
using VentiColaEditor.UI.Settings;

namespace VentiColaEditor.UI.CustomEditors
{
    [CustomEditor(typeof(UIRuntimeSettings))]
    internal class UIRuntimeSettingsEditor : Editor
    {
        private SerializedProperty m_PageFactoryTypeName;
        private SerializedProperty m_UIStackMinGrow;
        private SerializedProperty m_UIStackMaxGrow;
        private SerializedProperty m_UIRootPrefab;
        private SerializedProperty m_BlurFallbackTexture;
        private SerializedProperty m_Shaders;


        private void OnEnable()
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    SerializedProperty property = serializedObject.FindProperty(field.Name);
                    field.SetValue(this, property);
                }
            }
        }

        private void OnDisable()
        {
            FieldInfo[] fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(SerializedProperty))
                {
                    field.SetValue(this, null);
                }
            }
        }


        public override void OnInspectorGUI()
        {
            if (UIRuntimeSettings.FindInstance() != target)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"This {target.GetType().Name} object is invalid. It will not be included in build.", MessageType.Error);
                return;
            }

            serializedObject.Update();

            EditorGUILayout.Space();
            DrawEditProjectSettingsButton();

            EditorGUILayout.Space();
            DrawGeneralSettings();

            EditorGUILayout.Space();
            DrawUIStackSettings();

            EditorGUILayout.Space();
            DrawPrefabSettings();

            EditorGUILayout.Space();
            DrawPlatformSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEditProjectSettingsButton()
        {
            if (GUILayout.Button("Edit Project Settings", GUILayout.Height(30)))
            {
                UIProjectSettings.OpenInProjectSettings();
            }
        }

        private void DrawGeneralSettings()
        {
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                Rect rect = EditorGUILayout.GetControlRect();
                rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTextContent("Page Factory"));

                string selectedTypeName = m_PageFactoryTypeName.stringValue;
                Type selectedType = Type.GetType(selectedTypeName, false);
                GUIContent selectedTypeDisplayName = GetFactoryDisplayName(selectedType);

                if (selectedTypeDisplayName == null)
                {
                    // 默认值，需要保证至少有一个 Factory
                    selectedType = UIRuntimeSettings.DefaultPageFactoryType;
                    selectedTypeName = selectedType.AssemblyQualifiedName;
                    selectedTypeDisplayName = GetFactoryDisplayName(selectedType);
                }

                if (EditorGUI.DropdownButton(rect, selectedTypeDisplayName, FocusType.Keyboard, EditorStyles.popup))
                {
                    var menu = new GenericMenu();

                    foreach (Type type in TypeCache.GetTypesDerivedFrom<IAsyncPageFactory>())
                    {
                        if (type.IsAbstract || type.IsGenericType)
                        {
                            continue;
                        }

                        GUIContent displayName = GetFactoryDisplayName(type);

                        if (displayName == null)
                        {
                            continue;
                        }

                        ConstructorInfo[] constructors = type.GetConstructors();

                        if (!constructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0))
                        {
                            continue;
                        }

                        string typeFullName = type.AssemblyQualifiedName;

                        menu.AddItem(displayName, selectedTypeName == typeFullName, () =>
                        {
                            m_PageFactoryTypeName.stringValue = typeFullName;
                            m_PageFactoryTypeName.serializedObject.ApplyModifiedProperties();
                        });
                    }

                    menu.DropDown(rect);
                }
            }
        }

        private static GUIContent GetFactoryDisplayName(Type factoryType)
        {
            var attr = factoryType?.GetCustomAttribute<CustomPageFactoryAttribute>();

            if (attr is null || string.IsNullOrWhiteSpace(attr.DisplayName))
            {
                return null;
            }

            return EditorGUIUtility.TrTextContent(attr.DisplayName);
        }

        private void DrawUIStackSettings()
        {
            EditorGUILayout.LabelField("UI Stack", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_UIStackMinGrow, EditorGUIUtility.TrTextContent("Min Grow"));
                m_UIStackMinGrow.intValue = Mathf.Min(m_UIStackMinGrow.intValue, m_UIStackMaxGrow.intValue);

                EditorGUILayout.PropertyField(m_UIStackMaxGrow, EditorGUIUtility.TrTextContent("Max Grow"));
                m_UIStackMaxGrow.intValue = Mathf.Max(m_UIStackMaxGrow.intValue, m_UIStackMinGrow.intValue);
            }
        }

        private void DrawPrefabSettings()
        {
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_UIRootPrefab, EditorGUIUtility.TrTextContent("UI Root"));
                GameObject uiRoot = m_UIRootPrefab.objectReferenceValue as GameObject;

                if (uiRoot == null)
                {
                    EditorGUILayout.HelpBox("UIRoot is None.", MessageType.Error);
                }
                else
                {
                    int activeCanvasCount = uiRoot.GetComponentsInChildren<Canvas>(false).Length;
                    int activeEventSystemCount = uiRoot.GetComponentsInChildren<EventSystem>(false).Length;

                    if (activeCanvasCount == 0)
                    {
                        EditorGUILayout.HelpBox("No active Canvas in the UIRoot.", MessageType.Error);
                    }
                    else if (activeCanvasCount > 1)
                    {
                        EditorGUILayout.HelpBox("Multiple active Canvases in the UIRoot.", MessageType.Warning);
                    }

                    if (activeEventSystemCount == 0)
                    {
                        EditorGUILayout.HelpBox("No active EventSystem in the UIRoot.", MessageType.Warning);
                    }

                    if (activeCanvasCount > 0 && activeEventSystemCount > 0)
                    {
                        Rect rect = EditorGUILayout.GetControlRect();
                        rect.xMin += EditorGUIUtility.labelWidth + 2f;

                        if (GUI.Button(rect, "New Editing Environment", EditorStyles.miniButton))
                        {
                            CreateNewUIEditingEnvironment();
                        }
                    }
                }
            }
        }

        private void CreateNewUIEditingEnvironment()
        {
            string path = EditorUtility.SaveFilePanelInProject("New UI Editing Environment",
                "[UI] Editing Environment", "unity", "Save the environment scene");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!path.EndsWith(".unity"))
            {
                path += ".unity";
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

            // 保证场景中的 UIRoot 和 Prefab 关联
            PrefabUtility.InstantiatePrefab(m_UIRootPrefab.objectReferenceValue, scene);

            if (EditorSceneManager.SaveScene(scene, path, false))
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                if (EditorUtility.DisplayDialog("Apply to EditorSettings",
                    "Set the scene as the editing environment of UI prefabs. See UnityEditor.EditorSettings.prefabUIEnvironment.",
                    "Yes", "No"))
                {
                    EditorSettings.prefabUIEnvironment = asset;
                }

                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogError("Can not save the scene!");
            }

            EditorSceneManager.UnloadSceneAsync(scene);
        }

        private void DrawPlatformSettings()
        {
            EditorGUILayout.LabelField("Shaders", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty shaders = m_Shaders.Copy();

                while (shaders.NextVisible(true))
                {
                    EditorGUILayout.PropertyField(shaders);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                m_BlurFallbackTexture.objectReferenceValue = EditorGUILayout.ObjectField(
                    EditorGUIUtility.TrTextContent("Blur Fallback"), m_BlurFallbackTexture.objectReferenceValue,
                    typeof(Texture2D), false);

                //EditorGUILayout.PropertyField(m_BlurFallbackTexture, EditorGUIUtility.TrTextContent("Fallback Texture"));

            }
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }
    }
}