using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hypersycos.Utils
{
    public static class EditorExtensions
    {
        public static void ShowPolymorphicList<T>(this Editor editor, SerializedProperty list, GUIContent buttonLabel)
        {
            void handleItemClicked(object parameter)
            {
                Type type = (Type)parameter;
                T instance = (T)Activator.CreateInstance(type);
                list.GetArrayElementAtIndex(list.arraySize++).managedReferenceValue = instance;
                editor.serializedObject.ApplyModifiedProperties();
            }

            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, list.displayName, EditorStyles.foldoutHeader);
            EditorGUI.indentLevel += 1;
            if (list.isExpanded)
            {
                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(i);
                    string name = element.type;
                    int start = name.IndexOf("<") + 1;
                    name = name.Substring(start, name.Length - start - 1);
                    EditorGUILayout.PropertyField(element, new GUIContent(name), true);
                }
            }
            EditorGUI.indentLevel -= 1;
            editor.DrawDropdown<T>(buttonLabel, list, handleItemClicked);
        }

        public static void DrawDropdown<T>(this Editor editor, GUIContent label, SerializedProperty field, GenericMenu.MenuFunction2 handleItemClicked)
        {
            if (!EditorGUILayout.DropdownButton(label, FocusType.Passive))
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(t => t.GetInterfaces().Contains(typeof(T)) || t.IsSubclassOf(typeof(T)) && !t.IsAbstract);
            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, handleItemClicked, type);
            }
            menu.ShowAsContext();
        }

        public static SerializedProperty FindPropertyByAutoPropertyName(this Editor editor, string propName)
        {
            return editor.serializedObject.FindProperty(string.Format("<{0}>k__BackingField", propName));
        }
    }
}
