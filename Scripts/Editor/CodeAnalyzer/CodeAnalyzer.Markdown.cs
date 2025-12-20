using System;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace Expecto
{
	public static partial class CodeAnalyzer
	{
		private static void GenerateMarkdownFromXml(string xmlFilePath)
		{
			if (!File.Exists(xmlFilePath))
			{
				Debug.LogError($"CodeAnalyzer: XML file not found at {xmlFilePath}");
				return;
			}

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(xmlFilePath);

				string fileName = Path.GetFileNameWithoutExtension(xmlFilePath) + ".md";
				string outputDirPath = Path.Combine(Application.dataPath + "/..", markdownOutputDirectory);
				if (!Directory.Exists(outputDirPath))
				{
					_ = Directory.CreateDirectory(outputDirPath);
				}
				string mdFilePath = Path.Combine(outputDirPath, fileName);
				StringBuilder md = new StringBuilder();

				XmlElement root = doc.DocumentElement;
				string namespaceName = root.GetAttribute("Namespace");

				md.AppendLine($"# Namespace: {namespaceName}");
				md.AppendLine();

				XmlNodeList classes = root.SelectNodes("Class");

				// Table of Contents
				if (classes.Count > 0)
				{
					md.AppendLine("## Table of Contents");
					foreach (XmlNode classNode in classes)
					{
						if (classNode is XmlElement classElement)
						{
							string className = classElement.GetAttribute("n");
							// Assuming simple class names without spaces/special chars as is typical for C# classes
							// If headers have spaces, they are usually replaced by '-' in markdown anchors.
							// Since these are class names, ToLowerInvariant() is sufficient.
							md.AppendLine($"- [{className}](#{className.ToLowerInvariant()})");
						}
					}
					md.AppendLine();
					md.AppendLine("---");
					md.AppendLine();
				}

				foreach (XmlNode classNode in classes)
				{
					if (classNode is XmlElement classElement)
					{
						string className = classElement.GetAttribute("n");
						string baseClass = classElement.GetAttribute("b");
						string context = classElement.GetAttribute("c");

						md.AppendLine($"## {className}");

						if (!string.IsNullOrEmpty(baseClass))
						{
							md.AppendLine($"**Inherits**: `{baseClass}`");
						}

						if (!string.IsNullOrEmpty(context))
						{
							md.AppendLine();
							AppendFormattedContext(md, context, "> - ");
						}

						// Fields
						XmlNodeList fields = classElement.SelectNodes("Fields/Field");
						if (fields.Count > 0)
						{
							md.AppendLine("#### Fields");
							foreach (XmlNode fieldNode in fields)
							{
								if (fieldNode is XmlElement fieldElement)
								{
									string value = fieldElement.GetAttribute("v");
									string fieldContext = fieldElement.GetAttribute("c");

									md.AppendLine($"- `{value}`");
									if (!string.IsNullOrEmpty(fieldContext))
									{
										AppendFormattedContext(md, fieldContext, "    - ");
									}
								}
							}
						}

						// Methods
						XmlNodeList methods = classElement.SelectNodes("Methods/Method");
						if (methods.Count > 0)
						{
							md.AppendLine("#### Methods");
							foreach (XmlNode methodNode in methods)
							{
								if (methodNode is XmlElement methodElement)
								{
									string value = methodElement.GetAttribute("v");
									string methodContext = methodElement.GetAttribute("c");

									md.AppendLine($"- `{value}`");
									if (!string.IsNullOrEmpty(methodContext))
									{
										AppendFormattedContext(md, methodContext, "    - ");
									}
								}
							}
						}

						md.AppendLine("---");
						md.AppendLine();
					}
				}

				File.WriteAllText(mdFilePath, md.ToString());
				Debug.Log($"CodeAnalyzer: Markdown generated at {mdFilePath}");
			}
			catch (Exception e)
			{
				Debug.LogError($"CodeAnalyzer: Failed to generate Markdown from {xmlFilePath}. Error: {e.Message}");
			}
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
