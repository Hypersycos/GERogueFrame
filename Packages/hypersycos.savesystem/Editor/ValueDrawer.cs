using UnityEditor;
using UnityEngine;

namespace Hypersycos.SaveSystem
{
    [CustomPropertyDrawer(typeof(Value))]
    public class ValueDrawer : PropertyDrawer
    {
        // Name.Space Value/FloatValue
        static readonly string PrefixString = typeof(Value).Assembly.GetName().Name + " " + nameof(Value) + "/";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(GetDisplayProperty(property), label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, GetDisplayProperty(property), label, true);
        }

        static SerializedProperty GetDisplayProperty(SerializedProperty valuePropertry)
        {
            var managedRefProperty = valuePropertry.FindPropertyRelative(Value.SerializedPropertyName);
            if (managedRefProperty.managedReferenceFullTypename.StartsWith(PrefixString))
            {
                // It's wrapped primitive
                return managedRefProperty.FindPropertyRelative(Value.InternalValueName);
            }
            return managedRefProperty;
        }
    }
}
