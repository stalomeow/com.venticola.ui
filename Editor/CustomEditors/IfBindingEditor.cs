using UnityEditor;
using UnityEngine;
using VentiCola.UI.Bindings;

// TODO: Clear useless reference.

namespace VentiColaEditor.UI
{
    [CustomEditor(typeof(IfBinding), true)]
    public class IfBindingEditor : Editor
    {
        private SerializedProperty m_Condition;
        private SerializedProperty m_NegateCondition;
        private SerializedProperty m_Callback;
        private SerializedProperty m_Transitions;

        private UITransitionEditor m_TransitionEditor = new UITransitionEditor();

        private void OnEnable()
        {
            m_Condition = serializedObject.FindProperty("m_Condition");
            m_NegateCondition = serializedObject.FindProperty("m_NegateCondition");
            m_Callback = serializedObject.FindProperty("m_Callback");
            m_Transitions = serializedObject.FindProperty("m_Transitions");

            m_TransitionEditor.Init(m_Transitions);
        }

        private void OnDisable()
        {
            m_TransitionEditor.Disable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox("*This binding will control the active state of this GameObject using GameObject.SetActive(bool) based on the runtime value of <Condition>.", MessageType.None);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Condition", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_Condition, new GUIContent("Value"));
            EditorGUILayout.PropertyField(m_NegateCondition, new GUIContent("Negation"));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Callback, new GUIContent("Callback"));
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

            m_TransitionEditor.DrawInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}