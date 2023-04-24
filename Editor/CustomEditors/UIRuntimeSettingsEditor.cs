using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using VentiCola.UI;
using VentiColaEditor.UI.Settings;
using Object = UnityEngine.Object;

namespace VentiColaEditor.UI.CustomEditors
{
    [CustomEditor(typeof(UIRuntimeSettings))]
    internal class UIRuntimeSettingsEditor : Editor
    {
        private SerializedProperty m_LRUCacheSize;
        private SerializedProperty m_UIRootPrefab;
        private SerializedProperty m_Shaders;
        private SerializedProperty m_EnableMainCameraOverrideSettings;
        private SerializedProperty m_EnableMainCameraRendererSettings;
        private SerializedProperty m_MainCameraOverrideSettings;
        private SerializedProperty m_MainCameraRendererSettings;
        private SerializedProperty m_AdditionalData;
        private Editor m_AdditionalDataEditor;

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
            if (UIRuntimeSettings.Instance != target)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"This {target.GetType().Name} object is invalid. It will not be included in build.", MessageType.Error);
                return;
            }

            serializedObject.Update();

            EditorGUILayout.Space();
            DrawEditProjectSettingsButton();

            EditorGUILayout.Space();
            DrawPrefabSettings();

            EditorGUILayout.Space();
            DrawGeneralSettings();

            EditorGUILayout.Space();
            DrawMainCameraOverrides();

            EditorGUILayout.Space();
            DrawMainCameraRenderers();

            EditorGUILayout.Space();
            DrawShadersSettings();

            EditorGUILayout.Space();
            DrawAdditionalData();

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
                EditorGUILayout.PropertyField(m_LRUCacheSize, EditorGUIUtility.TrTextContent("LRU Cache Size"));
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
                    EditorGUILayout.HelpBox("UIRoot can not be None.", MessageType.Error);
                }
                else
                {
                    int passedCheckCount = 0;
                    Canvas[] activeCanvases = uiRoot.GetComponentsInChildren<Canvas>(false);
                    EventSystem[] activeEventSystems = uiRoot.GetComponentsInChildren<EventSystem>(false);

                    if (activeCanvases.Length == 0)
                    {
                        EditorGUILayout.HelpBox("No active Canvas in the UIRoot.", MessageType.Error);
                    }
                    else if (activeCanvases.Length > 1)
                    {
                        EditorGUILayout.HelpBox("Multiple active Canvases in the UIRoot.", MessageType.Error);
                    }
                    else if (activeCanvases[0].renderMode != RenderMode.ScreenSpaceCamera)
                    {
                        EditorGUILayout.HelpBox("The RenderMode of Canvas should be ScreenSpaceCamera.", MessageType.Error);
                    }
                    else
                    {
                        passedCheckCount++;
                    }

                    if (activeEventSystems.Length == 0)
                    {
                        EditorGUILayout.HelpBox("No active EventSystem in the UIRoot.", MessageType.Warning);
                    }
                    else if (activeEventSystems.Length > 1)
                    {
                        EditorGUILayout.HelpBox("Multiple active EventSystems in the UIRoot.", MessageType.Warning);
                    }
                    else
                    {
                        passedCheckCount++;
                    }

                    if (passedCheckCount == 2)
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

        private void DrawMainCameraOverrides()
        {
            EditorGUILayout.LabelField("Main Camera Overrides", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    EditorGUILayout.PropertyField(m_EnableMainCameraOverrideSettings, EditorGUIUtility.TrTextContent("Enable"));
                    EditorGUILayout.HelpBox("If enabled, when a UI covers the entire screen, these settings will be temporarily applied to the Main Camera to reduce overdraw.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(!m_EnableMainCameraOverrideSettings.boolValue))
                {
                    SerializedProperty settings = m_MainCameraOverrideSettings.Copy();

                    while (settings.NextVisible(true))
                    {
                        if (!settings.propertyPath.StartsWith($"{nameof(m_MainCameraOverrideSettings)}."))
                        {
                            break;
                        }

                        EditorGUILayout.PropertyField(settings);
                    }
                }
            }
        }

        private void DrawMainCameraRenderers()
        {
            EditorGUILayout.LabelField("Main Camera Renderers", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    EditorGUILayout.PropertyField(m_EnableMainCameraRendererSettings, EditorGUIUtility.TrTextContent("Enable"));
                    EditorGUILayout.HelpBox("If enabled, when a UI covers the entire screen, the Light Weight Renderer will be temporarily applied to Main Camera to improve performance.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(!m_EnableMainCameraRendererSettings.boolValue))
                {
                    SerializedProperty settings = m_MainCameraRendererSettings.Copy();

                    var asset = UniversalRenderPipeline.asset;
                    var defaultRendererIndex = (int)asset.GetType().GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);
                    var renderers = (ScriptableRendererData[])asset.GetType().GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);

                    while (settings.NextVisible(true))
                    {
                        if (!settings.propertyPath.StartsWith($"{nameof(m_MainCameraRendererSettings)}."))
                        {
                            break;
                        }

                        Rect rect = EditorGUILayout.GetControlRect(true);
                        rect = EditorGUI.PrefixLabel(rect, EditorGUIUtility.TrTextContent(settings.displayName));

                        int selectedIndex = settings.intValue;
                        GUIContent popupLabel = GetRendererDisplayName(renderers, selectedIndex, defaultRendererIndex);

                        if (EditorGUI.DropdownButton(rect, popupLabel, FocusType.Keyboard, EditorStyles.popup))
                        {
                            var menu = new GenericMenu();
                            var propPath = settings.propertyPath;

                            menu.AddItem(GetRendererDisplayName(renderers, -1, defaultRendererIndex), selectedIndex == -1, () =>
                            {
                                serializedObject.FindProperty(propPath).intValue = -1;
                                serializedObject.ApplyModifiedProperties();
                            });

                            for (int i = 0; i < renderers.Length; i++)
                            {
                                int index = i;
                                menu.AddItem(GetRendererDisplayName(renderers, i, defaultRendererIndex), selectedIndex == i, () =>
                                {
                                    serializedObject.FindProperty(propPath).intValue = index;
                                    serializedObject.ApplyModifiedProperties();
                                });
                            }

                            menu.DropDown(rect);
                        }
                    }
                }
            }

            static GUIContent GetRendererDisplayName(ScriptableRendererData[] renderers, int index, int defaultIndex)
            {
                if (index == -1)
                {
                    return EditorGUIUtility.TrTextContent($"Default Renderer ({renderers[defaultIndex].name})");
                }

                return EditorGUIUtility.TrTextContent($"{index}: {renderers[index].name}");
            }
        }

        private void DrawShadersSettings()
        {
            EditorGUILayout.LabelField("Shaders", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                SerializedProperty shaders = m_Shaders.Copy();

                while (shaders.NextVisible(true))
                {
                    if (!shaders.propertyPath.StartsWith($"{nameof(m_Shaders)}."))
                    {
                        break;
                    }

                    EditorGUILayout.PropertyField(shaders);
                }
            }
        }

        private void DrawAdditionalData()
        {
            EditorGUILayout.LabelField("Additional Data", EditorStyles.boldLabel);

            Object targetDataObj;
            bool hasAdditionalData = true;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_AdditionalData, GUIContent.none);
                targetDataObj = m_AdditionalData.objectReferenceValue;

                if (targetDataObj == null)
                {
                    hasAdditionalData = false;
                    EditorGUILayout.HelpBox("You can extend the runtime settings by assigning a custom ScriptableObject.", MessageType.Info);
                }
                else if (targetDataObj == target)
                {
                    m_AdditionalData.objectReferenceValue = null;
                    Debug.LogError("Assigning self to Additional Data is not allowed!");
                }
            }

            if (hasAdditionalData)
            {
                EditorGUILayout.Space();
                CustomEditorGUIUtils.DrawSplitter();
                EditorGUILayout.Space();

                if (m_AdditionalDataEditor == null || m_AdditionalDataEditor.target != targetDataObj)
                {
                    m_AdditionalDataEditor = CreateEditor(targetDataObj);
                }

                m_AdditionalDataEditor.OnInspectorGUI();
            }
            else
            {
                m_AdditionalDataEditor = null;
            }
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }
    }
}