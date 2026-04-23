using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Expecto
{
	public static partial class CodeAnalyzer
	{
		private static void ExportToMarkdownByNamespace(List<ClassInfo> classes, string outputFileName)
		{
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
			string outputDirPath = Path.Combine(Application.dataPath + "/..", markdownOutputDirectory);
			if (!Directory.Exists(outputDirPath))
			{
				_ = Directory.CreateDirectory(outputDirPath);
			}

			// Create safe filename
			string safeFilename = outputFileName.Replace(".", "_") + ".md";
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

			StringBuilder md = new StringBuilder();
			md.AppendLine($"# Namespace: {namespaceString}");
			md.AppendLine();

			// Table of Contents
			List<ClassInfo> allSortedClasses = classes.OrderBy(c => c.Name).ToList();
			if (allSortedClasses.Count > 0)
			{
				md.AppendLine("## Table of Contents");
				foreach (var classInfo in allSortedClasses)
				{
					md.AppendLine($"- [{classInfo.Name}](#{classInfo.Name.ToLowerInvariant()})");
				}
				md.AppendLine();
				md.AppendLine("---");
				md.AppendLine();
			}

			foreach (var pair in namespaceGroups)
			{
				var nsClasses = pair.Value.OrderBy(c => c.Name).ToList();
				foreach (var classInfo in nsClasses)
				{
					md.AppendLine($"## {classInfo.Name}");

					if (!string.IsNullOrEmpty(classInfo.BaseClass))
					{
						// Strip the namespace from the base class name
						string baseClassName = classInfo.BaseClass;
						int lastDotIndex = baseClassName.LastIndexOf('.');
						if (lastDotIndex >= 0 && lastDotIndex < baseClassName.Length - 1)
						{
							baseClassName = baseClassName.Substring(lastDotIndex + 1);
						}

						// Skip adding Object and ValueType as a base class
						if (baseClassName != "Object" && baseClassName != "ValueType")
						{
							md.AppendLine($"**Inherits**: `{baseClassName}`");
						}
					}

					if (!string.IsNullOrEmpty(classInfo.Context))
					{
						md.AppendLine();
						AppendFormattedContext(md, classInfo.Context, "> - ");
					}

					// Fields
					if (classInfo.Fields.Count > 0)
					{
						md.AppendLine("#### Fields");
						var sortedFields = classInfo.Fields
							.OrderBy(f =>
							{
								if (f.IsProperty)
								{
									return (f.GetterModifier == "+") ? (f.SetterModifier == "+" ? 0 : 2) : 3;
								}
								return (f.AccessModifier == "+") ? 1 : 4;
							})
							.ThenBy(f => f.Name);

						foreach (var field in sortedFields)
						{
							md.AppendLine($"- `{FormatField(field)}`");
							if (!string.IsNullOrEmpty(field.Context))
							{
								AppendFormattedContext(md, field.Context, "    - ");
							}
						}
					}

					// Methods
					if (classInfo.Methods.Count > 0)
					{
						md.AppendLine("#### Methods");
						var sortedMethods = classInfo.Methods
							.OrderBy(m => m.AccessModifier != "+")
							.ThenBy(m => m.Name);

						foreach (var method in sortedMethods)
						{
							md.AppendLine($"- `{FormatMethod(method)}`");
							if (!string.IsNullOrEmpty(method.Context))
							{
								AppendFormattedContext(md, method.Context, "    - ");
							}
						}
					}

					md.AppendLine("---");
					md.AppendLine();
				}
			}

			File.WriteAllText(filePath, md.ToString());
			Debug.Log($"CodeAnalyzer: Markdown generated at {filePath}");
		}

		private static void AppendFormattedContext(StringBuilder md, string context, string linePrefix)
		{
			if (string.IsNullOrEmpty(context))
			{
				return;
			}

			string[] parts = context.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string part in parts)
			{
				int colonIndex = part.IndexOf(':');
				if (colonIndex > 0)
				{
					string key = part.Substring(0, colonIndex);
					string value = part.Substring(colonIndex + 1);
					_ = md.AppendLine($"{linePrefix}**{key}**:{value}");
				}
				else
				{
					_ = md.AppendLine($"{linePrefix}{part}");
				}
			}
		}
	}
}
