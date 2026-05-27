using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Hypersycos.SaveSystem
{
    class SaveSystemSettingsProvider : SettingsProvider
    {
        private SerializedObject m_CustomSettings;

        public static string customSettingsPath => SaveSystemSettings.customSettingsPath;
        public SaveSystemSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_CustomSettings = SaveSystemSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("files"));
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new SaveSystemSettingsProvider("Project/Save System", SettingsScope.Project);
        }
    }
}