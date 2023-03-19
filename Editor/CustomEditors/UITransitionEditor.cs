using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VentiCola.UI.Transitions;
using Object = UnityEngine.Object;

namespace VentiColaEditor.UI
{
    public class UITransitionEditor
    {
        private SerializedProperty m_Transitions;
        private Dictionary<Object, Editor> m_EditorCache;

        public void Init(SerializedProperty transitions)
        {
            m_Transitions = transitions;
            m_EditorCache ??= new Dictionary<Object, Editor>();
            m_EditorCache.Clear();

            for (int i = 0; i < m_Transitions.arraySize; i++)
            {
                SerializedProperty trans = m_Transitions.GetArrayElementAtIndex(i);
                CreateEditor(trans.objectReferenceValue);
            }
        }

        private void CreateEditor(Object target)
        {
            if (m_EditorCache.ContainsKey(target))
            {
                return;
            }

            // 将 target 从 Inspector 中隐藏，需要每次都手动设置。
            target.hideFlags |= HideFlags.HideInInspector;

            // TODO: Invoke OnEnable() (if it has)
            Editor editor = Editor.CreateEditor(target);
            m_EditorCache[target] = editor;
        }

        public void Disable()
        {
            m_Transitions = null;
            m_EditorCache.Clear();
        }

        public void DrawInspector()
        {
            EditorGUILayout.Space();

            for (int i = 0; i < m_Transitions.arraySize; i++)
            {
                SerializedProperty trans = m_Transitions.GetArrayElementAtIndex(i);
                Editor editor = m_EditorCache[trans.objectReferenceValue];

                DrawSplitter();

                GUIContent title = new GUIContent(editor.serializedObject.FindProperty("m_Label").stringValue);

                if (DrawHeaderToggle(title, trans, editor.serializedObject.FindProperty("m_EnableProperty"), pos =>
                {
                    GenericMenu menu = new GenericMenu();

                    if (i == 0)
                    {
                        menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move to Top"), false);
                        menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"), false);
                    }
                    else
                    {
                        int index = i;
                        menu.AddItem(EditorGUIUtility.TrTextContent("Move to Top"), false, () => MoveTransition(index, 0));
                        menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveTransition(index, index - 1));
                    }

                    if (i == m_Transitions.arraySize - 1)
                    {
                        menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move to Bottom"), false);
                        menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"), false);
                    }
                    else
                    {
                        int index = i;
                        menu.AddItem(EditorGUIUtility.TrTextContent("Move to Bottom"), false, () => MoveTransition(index, m_Transitions.arraySize - 1));
                        menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveTransition(index, index + 1));
                    }

                    menu.AddSeparator(string.Empty);

                    menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, SetExpandStatesOfAll, false);
                    menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, SetExpandStatesOfAll, true);

                    menu.AddSeparator(string.Empty);

                    menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, ResetTransition, i);
                    menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, RemoveTransition, i);

                    menu.AddSeparator(string.Empty);

                    menu.AddItem(EditorGUIUtility.TrTextContent("Copy"), false, CopyTransition, i);

                    if (CanPaste(i))
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent("Paste"), false, PasteTransition, i);
                    }
                    else
                    {
                        menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste"), false);
                    }

                    menu.DropDown(new Rect(pos, Vector2.zero));
                }))
                {
                    editor.DrawDefaultInspector();
                    EditorGUILayout.Space();
                }
            }

            DrawSplitter();

            EditorGUILayout.Space();

            using (var scope = new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button("Add Transition"))
                {
                    AddTransitionDropdownMenu(scope.rect);
                }
            }
        }

        private void AddTransitionDropdownMenu(Rect rect)
        {
            GenericMenu menu = new GenericMenu();

            foreach (var type in TypeCache.GetTypesDerivedFrom<TransitionBase>())
            {
                var attr = type.GetCustomAttribute<CustomTransitionAttribute>();

                if (attr == null || string.IsNullOrEmpty(attr.MenuPath))
                {
                    continue;
                }

                menu.AddItem(new GUIContent(attr.MenuPath), false, AddTransition, type);
            }

            menu.DropDown(rect);
        }

        private void SetExpandStatesOfAll(object expandData)
        {
            bool expand = (bool)expandData;

            m_Transitions.serializedObject.Update();

            for (int i = 0; i < m_Transitions.arraySize; i++)
            {
                m_Transitions.GetArrayElementAtIndex(i).isExpanded = expand;
            }

            m_Transitions.serializedObject.ApplyModifiedProperties();
        }

        private void MoveTransition(int srcIndex, int dstIndex)
        {
            m_Transitions.serializedObject.Update();

            bool srcExpanded = m_Transitions.GetArrayElementAtIndex(srcIndex).isExpanded;
            bool dstExpanded = m_Transitions.GetArrayElementAtIndex(dstIndex).isExpanded;

            m_Transitions.MoveArrayElement(srcIndex, dstIndex);

            m_Transitions.GetArrayElementAtIndex(srcIndex).isExpanded = dstExpanded;
            m_Transitions.GetArrayElementAtIndex(dstIndex).isExpanded = srcExpanded;

            m_Transitions.serializedObject.ApplyModifiedProperties();
        }

        private void ResetTransition(object indexData)
        {
            int index = (int)indexData;

            m_Transitions.serializedObject.Update();

            var property = m_Transitions.GetArrayElementAtIndex(index);
            var prevTransition = property.objectReferenceValue;

            property.objectReferenceValue = null;

            GameObject go = ((Component)m_Transitions.serializedObject.targetObject).gameObject;
            Object newTransition = Undo.AddComponent(go, prevTransition.GetType());
            property.objectReferenceValue = newTransition;
            CreateEditor(newTransition);

            m_Transitions.serializedObject.ApplyModifiedProperties();

            m_EditorCache.Remove(prevTransition);
            Undo.DestroyObjectImmediate(prevTransition);
        }

        private void AddTransition(object typeData)
        {
            Type type = (Type)typeData;
            GameObject go = ((Component)m_Transitions.serializedObject.targetObject).gameObject;
            Object transObj = Undo.AddComponent(go, type);

            m_Transitions.serializedObject.Update();
            m_Transitions.InsertArrayElementAtIndex(m_Transitions.arraySize);

            SerializedProperty newTransition = m_Transitions.GetArrayElementAtIndex(m_Transitions.arraySize - 1);
            newTransition.objectReferenceValue = transObj;
            newTransition.isExpanded = true;

            m_Transitions.serializedObject.ApplyModifiedProperties();

            CreateEditor(transObj);
        }

        private void RemoveTransition(object indexData)
        {
            int index = (int)indexData;            

            m_Transitions.serializedObject.Update();

            SerializedProperty property = m_Transitions.GetArrayElementAtIndex(index);
            m_EditorCache.Remove(property.objectReferenceValue, out Editor editor);
            m_Transitions.DeleteArrayElementAtIndex(index);

            m_Transitions.serializedObject.ApplyModifiedProperties();

            // TODO: Invoke OnDisable() (if it has)
            Undo.DestroyObjectImmediate(editor.target);
            Object.DestroyImmediate(editor);
        }

        private void CopyTransition(object indexData)
        {
            int index = (int)indexData;
            Object target = m_Transitions.GetArrayElementAtIndex(index).objectReferenceValue;

            string typeName = target.GetType().AssemblyQualifiedName;
            string typeData = JsonUtility.ToJson(target);
            EditorGUIUtility.systemCopyBuffer = $"{typeName}|{typeData}";
        }

        private void PasteTransition(object indexData)
        {
            int index = (int)indexData;
            Object target = m_Transitions.GetArrayElementAtIndex(index).objectReferenceValue;

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            string typeData = clipboard[(clipboard.IndexOf('|') + 1)..];
            Undo.RecordObject(target, "Paste Settings");
            JsonUtility.FromJsonOverwrite(typeData, target);
        }

        private bool CanPaste(object indexData)
        {
            if (string.IsNullOrWhiteSpace(EditorGUIUtility.systemCopyBuffer))
            {
                return false;
            }

            string clipboard = EditorGUIUtility.systemCopyBuffer;
            int separator = clipboard.IndexOf('|');

            if (separator < 0)
            {
                return false;
            }

            int index = (int)indexData;
            Object target = m_Transitions.GetArrayElementAtIndex(index).objectReferenceValue;
            return target.GetType().AssemblyQualifiedName == clipboard[..separator];
        }


        /// <summary>Draw a splitter separator</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        /// <summary>Draw a header toggle like in Volumes</summary>
        /// <param name="title"> The title of the header </param>
        /// <param name="group"> The group of the header </param>
        /// <param name="activeField">The active field</param>
        /// <param name="contextAction">The context action</param>
        /// <returns>return the state of the foldout header</returns>
        public static bool DrawHeaderToggle(GUIContent title, SerializedProperty group, SerializedProperty activeField, Action<Vector2> contextAction)
        {
            var backgroundRect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(1f, 17f));

            var labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f + 16 + 5;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            var toggleRect = backgroundRect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            using (new EditorGUI.DisabledScope(!activeField.boolValue))
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Foldout
            group.isExpanded = GUI.Toggle(foldoutRect, group.isExpanded, GUIContent.none, EditorStyles.foldout);

            // Active checkbox
            activeField.serializedObject.Update();
            activeField.boolValue = GUI.Toggle(toggleRect, activeField.boolValue, GUIContent.none, new GUIStyle("ShurikenToggle"));
            activeField.serializedObject.ApplyModifiedProperties();

            // Context menu
            var contextMenuIcon = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");//CoreEditorStyles.contextMenuIcon.image;
            var contextMenuRect = new Rect(labelRect.xMax + 3f + 16 + 5, labelRect.y + 1f, 16, 16);

            if (contextAction != null)
            {
                if (GUI.Button(contextMenuRect, contextMenuIcon, "IconButton"))
                    contextAction(new Vector2(contextMenuRect.x, contextMenuRect.yMax));
            }

            // Handle events
            var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    // Left click: Expand/Collapse
                    if (e.button == 0)
                        group.isExpanded = !group.isExpanded;
                    // Right click: Context menu
                    else if (contextAction != null)
                        contextAction(e.mousePosition);

                    e.Use();
                }
            }

            return group.isExpanded;
        }
    }
}