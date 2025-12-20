using UnityEngine;

namespace Expecto
{
	[CreateAssetMenu(fileName = "CodeAnalyzerSettings", menuName = "Expecto/CodeAnalyzerSettings")]
	public class CodeAnalyzerSettings : ScriptableObject
	{
		[Header("XML Generation")]
		[Tooltip("Output directory for generated XML files")]
		public string outputDirectory = "AIContexts";

		[Tooltip("CodeAnalyzer will analyze only classes from these namespaces")]
		public string[] namespaceFilters;

		[System.Serializable]
		public struct CombinedNamespaceFilter
		{
			public string[] namespaceFilters;
			public string outputFileName;
		}

		[Tooltip("CodeAnalyzer will combine classes from these namespaces into single XML file")]
		public CombinedNamespaceFilter[] combinedNamespaceFilters;

		[Header("Markdown Generation")]
		[Tooltip("Make markdown files with summary of all AIContexts")]
		public bool generateMarkdown = true;

		[Tooltip("Output directory for generated markdown files")]
		public string markdownOutputDirectory = "Docs";
	}

}