#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Hypersycos.SaveSystem
{
    public class ScriptableObjectBuilder
    {
        public int callbackOrder => 0;

        [InitializeOnEnterPlayMode]
        public static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            BuildAssets();
        }

        static void BuildAssets()
        {
            string[] fileNames = AssetDatabase.FindAssets("t:RegisteredFileSOBase");
            string[] categoryNames = AssetDatabase.FindAssets("t:RegisteredCategorySOBase");
            string[] valueNames = AssetDatabase.FindAssets("t:RegisteredValueSOBase");
            List<SaveSystemSO<RegisteredFile>> files = new();
            List<SaveSystemSO<RegisteredCategory>> categories = new();
            foreach (string SOName in valueNames)
            {
                var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
                var SO = AssetDatabase.LoadAssetAtPath<SaveSystemSO<RegisteredValue>>(SOpath);
                try
                {
                    SO.Create();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError(SOpath);
                }
            }
            foreach (string SOName in categoryNames)
            {
                var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
                var SO = AssetDatabase.LoadAssetAtPath<SaveSystemSO<RegisteredCategory>>(SOpath);
                categories.Add(SO);
                SO.Clear();
            }
            foreach (string SOName in fileNames)
            {
                var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
                var SO = AssetDatabase.LoadAssetAtPath<SaveSystemSO<RegisteredFile>>(SOpath);
                files.Add(SO);
                SO.Clear();
            }

            foreach (var category in categories)
            {
                category.Create();
            }

            foreach (var file in files)
            {
                file.Create();
            }
        }
    }
}
#endif