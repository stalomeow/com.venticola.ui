using UnityEditor;
using VentiCola.UI.Bindings;

namespace VentiColaEditor.UI
{
    public abstract class TwoWayBindingEditor : Editor
    {
        private SerializedProperty m_Mode;
        private SerializedProperty m_Changed;

        protected virtual void OnEnable()
        {
            m_Mode = serializedObject.FindProperty("m_Mode");
            m_Changed = serializedObject.FindProperty("m_Changed");
        }

        protected void DrawBindingModeField()
        {
            EditorGUILayout.PropertyField(m_Mode);
        }

        protected void DrawChangedCallbackField()
        {
            BindingMode mode = (BindingMode)m_Mode.intValue;

            if (mode == BindingMode.TwoWayCallback)
            {
                EditorGUILayout.PropertyField(m_Changed);
            }
        }
    }
}