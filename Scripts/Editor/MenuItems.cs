using System.IO;
using UnityEditor;
using UnityEngine;

namespace Expecto
{
	static class MenuItems
	{
		[MenuItem("Tools/AI Context/Generate Context", validate = true)]
		private static bool ValidateAnalyzeCodeMenuItem()
		{
			var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
			if (settings.Length == 0)
			{
				return false;
			}

			return true;
		}

		[MenuItem("Tools/AI Context/Generate Context", priority = 1, secondaryPriority = 1000)]
		private static void AnalyzeCodeMenuItem()
		{
			CodeAnalyzer.RunCodeAnalysis();
		}

		[MenuItem("Tools/AI Context/Open Settings", validate = true)]
		private static bool ValidateOpenSettings()
		{
			var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
			if (settings.Length == 0)
			{
				return false;
			}

			return true;
		}

		[MenuItem("Tools/AI Context/Open Settings", priority = 1, secondaryPriority = 10001)]
		private static void OpenSettings()
		{
			var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
			if (settings.Length == 0)
			{
				return;
			}

			Selection.activeObject =
				AssetDatabase.LoadAssetAtPath<CodeAnalyzerSettings>(AssetDatabase.GUIDToAssetPath(settings[0]));
		}

		[MenuItem("Tools/AI Context/Create Settings", validate = true)]
		private static bool ValidateCreateSettings()
		{
			var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
			if (settings.Length == 0)
			{
				return true;
			}

			return false;
		}

		[MenuItem("Tools/AI Context/Create Settings", secondaryPriority = 10002)]
		private static void CreateSettings()
		{
			var settings = ScriptableObject.CreateInstance<CodeAnalyzerSettings>();

			var path = Path.Combine("Assets", "Expecto", "Settings", "CodeAnalyzerSettings.asset");
			if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(path)))
			{
				AssetDatabase.CreateFolder("Assets", "Expecto");
				AssetDatabase.CreateFolder("Assets/Expecto", "Settings");
			}

			AssetDatabase.CreateAsset(settings, path);
			Selection.activeObject = settings;

			EditorUtility.SetDirty(settings);
			AssetDatabase.SaveAssets();
		}
	}
}