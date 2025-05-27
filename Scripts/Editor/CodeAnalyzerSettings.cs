using UnityEngine;

namespace Expecto
{
    [CreateAssetMenu(fileName = "CodeAnalyzerSettings", menuName = "Expecto/CodeAnalyzerSettings")]
    public class CodeAnalyzerSettings : ScriptableObject
    {

        [Tooltip("Output directory for generated XML files")]
        public string outputDirectory;

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
    }

}