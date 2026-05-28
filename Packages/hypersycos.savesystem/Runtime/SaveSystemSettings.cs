using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hypersycos.SaveSystem
{
    public class SaveSystemSettings : ScriptableObject
    {
        public const string customSettingsPath = "Assets/Resources/SaveSystemSettings.asset";
        public const string resourcesPath = "SaveSystemSettings";
        static SaveSystemSettings singleton;

        [SerializeField]
        public List<RegisteredFileSOBase> files = new();

        internal static SaveSystemSettings GetOrCreateSettings()
        {
#if UNITY_EDITOR
            var settings = AssetDatabase.LoadAssetAtPath<SaveSystemSettings>(customSettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<SaveSystemSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(settings, customSettingsPath);
                AssetDatabase.SaveAssets();
            }
            singleton = settings;
            return settings;
#else
            if (singleton is null)
                singleton = Resources.Load<SaveSystemSettings>(resourcesPath);
            return singleton;
#endif
        }

#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#endif
    }
}