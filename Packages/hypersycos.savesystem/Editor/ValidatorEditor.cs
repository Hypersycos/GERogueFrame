/*using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.SaveSystem
{
    [CustomEditor(typeof(ValidatorSO<>))]
    public class ValidatorEditor : Editor
    {
        SerializedProperty validatorType;
        SerializedProperty validator;

        void OnEnable()
        {
            validatorType = serializedObject.FindProperty("validatorType");
            validator = serializedObject.FindProperty("validator");
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement myInspector = new VisualElement();
            myInspector.Add(new PropertyField(validatorType));
            myInspector.Add(new PropertyField(validator));

            // Return the finished Inspector UI.
            return myInspector;
    }
    }
}*/