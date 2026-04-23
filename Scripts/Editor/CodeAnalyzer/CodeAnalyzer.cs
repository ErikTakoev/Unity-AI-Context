using System;
using System.Collections.Generic;
using System.IO;
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
		private const string LogPrefix = "CodeAnalyzer: ";
		private static string outputDirectory;
		private static string markdownOutputDirectory;
		private static bool generateMarkdown;
		private static bool generateXML;

		private class AnalysisFilter
		{
			public string OutputFileName;
			public string[] NamespaceFilters;
			public string[] NameFilters;
			public List<ClassInfo> Classes = new List<ClassInfo>();
			public HashSet<string> ProcessedClassNames = new HashSet<string>();

			public bool Matches(Type type)
			{
				if (NamespaceFilters != null)
				{
					for (int i = 0; i < NamespaceFilters.Length; i++)
					{
						if (type.Namespace == NamespaceFilters[i]) return true;
					}
				}

				if (NameFilters != null)
				{
					for (int i = 0; i < NameFilters.Length; i++)
					{
						if (type.Name == NameFilters[i] || type.FullName == NameFilters[i]) return true;
					}
				}

				return false;
			}
		}

		// Static constructor that is called when Unity is started or scripts are recompiled
		static CodeAnalyzer()
		{
			EditorApplication.delayCall += static () =>
			{
				if (EditorApplication.isPlayingOrWillChangePlaymode)
				{
					return;
				}

				RunCodeAnalysis();
			};
		}


		public static void RunCodeAnalysis()
		{
			Debug.Log(LogPrefix + "Running code analysis...");
			string[] settings = AssetDatabase.FindAssets("t:CodeAnalyzerSettings");
			if (settings.Length == 0)
			{
				Debug.LogError(LogPrefix + "CodeAnalyzerSettings not found");
				return;
			}

			string path = AssetDatabase.GUIDToAssetPath(settings[0]);
			CodeAnalyzerSettings settingsAsset = AssetDatabase.LoadAssetAtPath<CodeAnalyzerSettings>(path);
			outputDirectory = settingsAsset.outputDirectory;
			markdownOutputDirectory = settingsAsset.markdownOutputDirectory;
			generateMarkdown = settingsAsset.generateMarkdown;
			generateXML = settingsAsset.generateXML;

			_ = Task.Run(() =>
			{
				List<AnalysisFilter> filters = new List<AnalysisFilter>();

				// Add regular namespace filters
				if (settingsAsset.namespaceFilters != null)
				{
					foreach (string ns in settingsAsset.namespaceFilters)
					{
						filters.Add(new AnalysisFilter
						{
							OutputFileName = ns,
							NamespaceFilters = new[] { ns }
						});
					}
				}

				// Add combined namespace filters
				if (settingsAsset.combinedNamespaceFilters != null)
				{
					foreach (var combined in settingsAsset.combinedNamespaceFilters)
					{
						filters.Add(new AnalysisFilter
						{
							OutputFileName = combined.outputFileName,
							NamespaceFilters = combined.nameFilters
						});
					}
				}

				// Add combined classes filters
				if (settingsAsset.combinedClassesFilters != null)
				{
					foreach (var combined in settingsAsset.combinedClassesFilters)
					{
						if (combined.nameFilters != null && combined.nameFilters.Length > 0)
						{
							filters.Add(new AnalysisFilter
							{
								OutputFileName = combined.outputFileName,
								NameFilters = combined.nameFilters
							});
						}
					}
				}

				if (filters.Count == 0) return;

				AnalyzeCode(filters);

				string generatedFormats = (generateXML ? "XML " : "") + (generateMarkdown ? "Markdown" : "");
				Debug.Log(LogPrefix + $"Code analysis completed. {generatedFormats} results saved.");
			});
		}

		private static void AnalyzeCode(List<AnalysisFilter> filters)
		{
			// Get all loaded assemblies
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			// First pass: Process all regular (non-generic) types
			foreach (Assembly assembly in assemblies)
			{
				Type[] types = assembly.GetTypes();

				foreach (Type type in types)
				{
					if (ShouldSkipType(type)) continue;

					foreach (var filter in filters)
					{
						if (filter.Matches(type))
						{
							ExtractTypeMetadata(type, filter.Classes, filter.ProcessedClassNames);
						}
					}
				}
			}

			// Second pass: Find generic types used in fields, properties, and methods
			HashSet<Type> genericTypesToProcess = FindGenericTypesInAssemblies(assemblies, filters);

			// Process the collected generic types
			foreach (Type genericType in genericTypesToProcess)
			{
				foreach (var filter in filters)
				{
					if (filter.Matches(genericType))
					{
						ExtractTypeMetadata(genericType, filter.Classes, filter.ProcessedClassNames);
					}
				}
			}

			// Export each filter
			foreach (var filter in filters)
			{
				if (filter.Classes.Count > 0)
				{
					if (generateXML)
					{
						ExportToXmlByNamespace(filter.Classes, filter.OutputFileName);
					}

					if (generateMarkdown)
					{
						ExportToMarkdownByNamespace(filter.Classes, filter.OutputFileName);
					}
				}
			}
		}

		private static bool ShouldSkipType(Type type)
		{
			// Skip compiler-generated classes
			if (type.Name.Contains("<") || type.Name.StartsWith("__") ||
			    type.Name.Contains("DisplayClass") || type.Name.Contains("AnonymousType") ||
			    type.Name.Contains("$"))
			{
				return true;
			}

			if (type.IsDefined(typeof(IgnoreCodeAnalyzerAttribute), false))
			{
				return true;
			}

			// Skip types that inherit from MulticastDelegate (delegates)
			if (type.BaseType?.Name == "MulticastDelegate")
			{
				return true;
			}

			// Skip nested/inner classes
			if (type.IsNested)
			{
				return true;
			}

			// Skip generic type definitions (like List<T>)
			if (type.IsGenericTypeDefinition)
			{
				return true;
			}

			return false;
		}

		private static void CollectGenericTypes(Type type, HashSet<Type> genericTypes)
		{
			// Skip null types
			if (type == null)
			{
				return;
			}

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
					_ = genericTypes.Add(type);
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
			{
				return "+";
			}

			if (method.IsPrivate)
			{
				return "-";
			}

			if (method.IsFamily) // protected
			{
				return "~";
			}

			if (method.IsFamilyOrAssembly) // protected internal
			{
				return "~";
			}

			if (method.IsAssembly) // internal
			{
				return "~"; // using ~ for internal
			}

			return "-"; // default to private
		}

		private static string GetAccessModifierSymbol(FieldInfo field)
		{
			if (field.IsPublic)
			{
				return "+";
			}

			if (field.IsPrivate)
			{
				return "-";
			}

			if (field.IsFamily) // protected
			{
				return "~";
			}

			if (field.IsFamilyOrAssembly) // protected internal
			{
				return "~";
			}

			if (field.IsAssembly) // internal
			{
				return "~"; // using ~ for internal
			}

			return "-"; // default to private
		}


		private static void ExportToXmlByNamespace(List<ClassInfo> classes, string outputFileName)
		{
			// Group classes by namespace
			// Group classes by namespace
			Dictionary<string, List<ClassInfo>> namespaceGroups = new Dictionary<string, List<ClassInfo>>();
			foreach (var classInfo in classes)
			{
				string ns = classInfo.Namespace ?? "";
				if (!namespaceGroups.ContainsKey(ns))
				{
					namespaceGroups[ns] = new List<ClassInfo>();
				}
				namespaceGroups[ns].Add(classInfo);
			}

			// Create output directory if it doesn't exist
			string outputDirPath = Path.Combine(Application.dataPath + "/..", outputDirectory);
			if (!Directory.Exists(outputDirPath))
			{
				_ = Directory.CreateDirectory(outputDirPath);
			}

			// Create safe filename from namespace
			string safeFilename = outputFileName.Replace(".", "_") + ".xml";
			string filePath = Path.Combine(outputDirPath, safeFilename);
			
			List<string> namespaces = new List<string>();
			foreach (var group in namespaceGroups)
			{
				if (!namespaces.Contains(group.Key))
				{
					namespaces.Add(group.Key);
				}
			}
			string namespaceString = string.Join(";", namespaces);

			// Create XML document
			XmlDocument doc = new();
			// Create root element
			XmlElement root = doc.CreateElement("CodeAnalysis");
			root.SetAttribute("Namespace", namespaceString);
			_ = doc.AppendChild(root);

			XmlComment comment =
				doc.CreateComment(
					"XML attribute abbreviations: n — class name; b — base class name; c — context; v — value");
			_ = root.AppendChild(comment);
			comment = doc.CreateComment(
				$"Modifiers: '++' and '+' - public, '+-' - public getter, private setter, '~' - protected, '-' - private");
			_ = root.AppendChild(comment);

			// Create and save a file for each namespace
			foreach (var pair in namespaceGroups)
			{
				string namespaceName = pair.Key;
				List<ClassInfo> namespaceGroupClasses = pair.Value;

				// Sort classes by name
				namespaceGroupClasses.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

				// Add classes
				foreach (ClassInfo classInfo in namespaceGroupClasses)
				{
					ExportToXmlByClass(doc, root, classInfo);
				}
			}

			// Save to file
			doc.Save(filePath);
		}
	}
}