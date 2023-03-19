using UnityEditor;
using UnityEngine;
using VentiCola.UI;
using VentiCola.UI.Transitions;

namespace VentiColaEditor.UI
{
    [CustomEditor(typeof(UIPage), true)]
    public class UIPageEditor : Editor
    {
        private UITransitionEditor m_TransitionEditor = new UITransitionEditor();

        private void OnEnable()
        {
            m_TransitionEditor.Init(serializedObject.FindProperty("m_Transitions"));
        }

        private void OnDisable()
        {
            m_TransitionEditor.Disable();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            serializedObject.Update();

            m_TransitionEditor.DrawInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}