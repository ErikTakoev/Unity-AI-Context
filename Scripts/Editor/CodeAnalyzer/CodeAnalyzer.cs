using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Expecto
{
    [InitializeOnLoad]
    public static partial class CodeAnalyzer
    {
        const string LogPrefix = "CodeAnalyzer: ";
        private static string outputDirectory;

        // Static constructor that is called when Unity is started or scripts are recompiled
        static CodeAnalyzer()
        {
            EditorApplication.delayCall += () =>
            {
                RunCodeAnalysis();
            };
        }



        public static void RunCodeAnalysis()
        {
            Debug.Log(LogPrefix + "Running code analysis...");
            var settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
            if (settings.Length == 0)
            {
                Debug.LogError(LogPrefix + "CodeAnalyzerSettings not found");
                return;
            }
            var path = AssetDatabase.GUIDToAssetPath(settings[0]);
            var settingsAsset = AssetDatabase.LoadAssetAtPath<CodeAnalyzerSettings>(path);
            outputDirectory = settingsAsset.outputDirectory;

            Task.Run(() =>
            {
                foreach (var namespaceFilter in settingsAsset.namespaceFilters)
                {
                    AnalyzeCode(new[] { namespaceFilter }, namespaceFilter);
                }

                if (settingsAsset.combinedNamespaceFilters != null)
                {
                    foreach (var combinedNamespaceFilter in settingsAsset.combinedNamespaceFilters)
                    {
                        AnalyzeCode(combinedNamespaceFilter.namespaceFilters, combinedNamespaceFilter.outputFileName);
                    }
                }


                Debug.Log(LogPrefix + "Code analysis completed. Results saved to " + outputDirectory + " directory");
            });
        }

        private static void AnalyzeCode(string[] namespaceFilters, string outputFileName)
        {
            List<ClassInfo> classes = new List<ClassInfo>();
            HashSet<string> processedClassNames = new HashSet<string>();

            // Get all loaded assemblies
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // First pass: Process all regular (non-generic) types
            foreach (Assembly assembly in assemblies)
            {
                // Get all types in the assembly
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    // Skip compiler-generated classes
                    if (type.Name.Contains("<") || type.Name.StartsWith("__") ||
                        type.Name.Contains("DisplayClass") || type.Name.Contains("AnonymousType") ||
                        type.Name.Contains("$"))
                    {
                        continue; // Skip this type
                    }

                    if (type.IsDefined(typeof(IgnoreCodeAnalyzerAttribute), false))
                    {
                        continue;
                    }

                    // Skip types that inherit from MulticastDelegate (delegates)
                    if (type.BaseType?.Name == "MulticastDelegate")
                    {
                        continue; // Skip delegates
                    }

                    // Skip nested/inner classes
                    if (type.IsNested)
                    {
                        continue; // Skip nested classes
                    }

                    // Skip generic type definitions (like List<T>)
                    if (type.IsGenericTypeDefinition)
                    {
                        continue;
                    }

                    // Filter by namespace if needed
                    if (type.Namespace != null && namespaceFilters.Any(filter => type.Namespace == filter))
                    {
                        ExtractTypeMetadata(type, classes, processedClassNames);
                    }
                }
            }

            // Second pass: Find generic types used in fields, properties, and methods
            HashSet<Type> genericTypesToProcess = FindGenericTypesInAssemblies(assemblies, namespaceFilters.ToList());

            // Process the collected generic types
            foreach (Type genericType in genericTypesToProcess)
            {
                // Skip if not in our target namespaces
                if (genericType.Namespace == null || !namespaceFilters.Any(filter => genericType.Namespace == filter))
                    continue;

                ExtractTypeMetadata(genericType, classes, processedClassNames);
            }

            // Export to XML, split by namespace
            ExportToXmlByNamespace(classes, outputFileName);
        }

        private static void CollectGenericTypes(Type type, HashSet<Type> genericTypes)
        {
            // Skip null types
            if (type == null)
                return;

            // If this is a generic type (like List<string>), add it
            // But only if it's not a generic type definition (like List<T>)
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // Check if all generic arguments are concrete types (not type parameters)
                bool allArgumentsAreConcrete = true;
                foreach (Type argType in type.GetGenericArguments())
                {
                    if (argType.IsGenericParameter)
                    {
                        allArgumentsAreConcrete = false;
                        break;
                    }
                }

                // Only add types with concrete generic arguments
                if (allArgumentsAreConcrete)
                {
                    genericTypes.Add(type);
                }

                // Also process its generic arguments
                foreach (Type argType in type.GetGenericArguments())
                {
                    CollectGenericTypes(argType, genericTypes);
                }
            }

            // If it's an array, process its element type
            if (type.IsArray)
            {
                CollectGenericTypes(type.GetElementType(), genericTypes);
            }
        }


        private static string GetAccessModifierSymbol(MethodInfo method)
        {
            if (method.IsPublic)
                return "+";
            if (method.IsPrivate)
                return "-";
            if (method.IsFamily) // protected
                return "~";
            if (method.IsFamilyOrAssembly) // protected internal
                return "~";
            if (method.IsAssembly) // internal
                return "~"; // using ~ for internal

            return "-"; // default to private
        }

        private static string GetAccessModifierSymbol(FieldInfo field)
        {
            if (field.IsPublic)
                return "+";
            if (field.IsPrivate)
                return "-";
            if (field.IsFamily) // protected
                return "~";
            if (field.IsFamilyOrAssembly) // protected internal
                return "~";
            if (field.IsAssembly) // internal
                return "~"; // using ~ for internal

            return "-"; // default to private
        }



        private static void ExportToXmlByNamespace(List<ClassInfo> classes, string outputFileName)
        {
            // Group classes by namespace
            var namespaceGroups = classes.GroupBy(c => c.Namespace).ToList();

            // Create output directory if it doesn't exist
            string outputDirPath = Path.Combine(Application.dataPath + "/..", outputDirectory);
            if (!Directory.Exists(outputDirPath))
            {
                Directory.CreateDirectory(outputDirPath);
            }

            // Create safe filename from namespace
            string safeFilename = outputFileName.Replace(".", "_") + ".xml";
            string filePath = Path.Combine(outputDirPath, safeFilename);
            string namespaceString = string.Join(";", namespaceGroups.Select(s => s.Key));

            // Create XML document
            XmlDocument doc = new XmlDocument();
            // Create root element
            XmlElement root = doc.CreateElement("CodeAnalysis");
            root.SetAttribute("Namespace", namespaceString);
            doc.AppendChild(root);

            XmlComment comment = doc.CreateComment("XML attribute abbreviations: n — class name; b — base class name; c — context; v — value");
            root.AppendChild(comment);
            comment = doc.CreateComment($"Modifiers: '++' and '+' - public, '+-' - public getter, private setter, '~' - protected, '-' - private");
            root.AppendChild(comment);

            // Create and save a file for each namespace
            foreach (var namespaceGroup in namespaceGroups)
            {
                string namespaceName = namespaceGroup.Key;

                // Sort classes by name
                var sortedClasses = namespaceGroup.OrderBy(c => c.Name).ToList();

                // Add classes
                foreach (ClassInfo classInfo in sortedClasses)
                {
                    ExportToXmlByClass(doc, root, classInfo);
                }

            }

            // Save to file
            doc.Save(filePath);
        }
    }

}