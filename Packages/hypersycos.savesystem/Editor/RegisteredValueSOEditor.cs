/*using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.SaveSystem
{
    [CustomEditor(typeof(RegisteredValueSO))]
    public class RegisteredValueSOEditor : Editor
    {
        SerializedProperty IsEphemeral;
        SerializedProperty RegisteredSerializers;
        SerializedProperty type;
        SerializedProperty Default;
        SerializedProperty DefaultGenerator;
        SerializedProperty UsesGenerator;
        SerializedProperty validators;

        void OnEnable()
        {
            IsEphemeral = serializedObject.FindProperty("IsEphemeral");
            RegisteredSerializers = serializedObject.FindProperty("RegisteredSerializers");
            type = serializedObject.FindProperty("type");
            Default = serializedObject.FindProperty("Default");
            DefaultGenerator = serializedObject.FindProperty("DefaultGenerator");
            UsesGenerator = serializedObject.FindProperty("UsesGenerator");
            validators = serializedObject.FindProperty("validators");
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();

            myInspector.Add(new PropertyField(IsEphemeral));
            myInspector.Add(new PropertyField(type));
            myInspector.Add(new PropertyField(UsesGenerator));
            if (UsesGenerator.boolValue)
                myInspector.Add(new PropertyField(DefaultGenerator));
            else
                myInspector.Add(new PropertyField(Default));

            // Return the finished Inspector UI.
            return myInspector;
    }
    }
}*/