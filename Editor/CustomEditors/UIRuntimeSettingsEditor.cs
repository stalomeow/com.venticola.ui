using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private SerializedProperty m_BlurFallbackTexture;
        private SerializedProperty m_Shaders;
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
            DrawPrefabSettings();

            EditorGUILayout.Space();
            DrawPlatformSettings();

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
                    if (!shaders.propertyPath.StartsWith($"{nameof(m_Shaders)}."))
                    {
                        break;
                    }

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