using UnityEditor;
using UnityEngine;
using VentiCola.UI.Internal;

namespace VentiColaEditor.UI.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(LayerPopupAttribute))]
    internal class LayerPopupAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
            EditorGUI.EndProperty();
        }
    }
}