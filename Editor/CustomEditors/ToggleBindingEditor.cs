using UnityEditor;
using VentiCola.UI.Bindings;

namespace VentiColaEditor.UI
{
    [CustomEditor(typeof(ToggleBinding))]
    public class ToggleBindingEditor : TwoWayBindingEditor
    {
        private SerializedProperty m_Value;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Value = serializedObject.FindProperty("m_Value");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawBindingModeField();

            EditorGUILayout.PropertyField(m_Value);

            DrawChangedCallbackField();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
