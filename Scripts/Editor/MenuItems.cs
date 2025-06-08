using System.IO;
using UnityEditor;
using UnityEngine;

namespace Expecto
{
    static class MenuItems
    {

        [MenuItem("Expecto/AI Context/Generate Context", validate = true)]
        private static bool ValidateAnalyzeCodeMenuItem()
        {
            var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
            if (settings.Length == 0)
            {
                return false;
            }
            return true;
        }

        [MenuItem("Expecto/AI Context/Generate Context", priority = 1, secondaryPriority = 1000)]
        private static void AnalyzeCodeMenuItem()
        {
            CodeAnalyzer.RunCodeAnalysis();
        }

        [MenuItem("Expecto/AI Context/Open Settings", validate = true)]
        private static bool ValidateOpenSettings()
        {
            var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
            if (settings.Length == 0)
            {
                return false;
            }
            return true;
        }

        [MenuItem("Expecto/AI Context/Open Settings", priority = 1, secondaryPriority = 10001)]
        private static void OpenSettings()
        {
            var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
            if (settings.Length == 0)
            {
                return;
            }
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CodeAnalyzerSettings>(AssetDatabase.GUIDToAssetPath(settings[0]));
        }

        [MenuItem("Expecto/AI Context/Create Settings", validate = true)]
        private static bool ValidateCreateSettings()
        {
            var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
            if (settings.Length == 0)
            {
                return true;
            }
            return false;
        }

        [MenuItem("Expecto/AI Context/Create Settings", secondaryPriority = 10002)]
        private static void CreateSettings()
        {
            var settings = ScriptableObject.CreateInstance<CodeAnalyzerSettings>();

            var path = Path.Combine("Assets", "Expecto", "CodeAnalyzerSettings.asset");
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(path)))
            {
                AssetDatabase.CreateFolder("Assets", "Expecto");
            }
            AssetDatabase.CreateAsset(settings, path);
            Selection.activeObject = settings;

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}