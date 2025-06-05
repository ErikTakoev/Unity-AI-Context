using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Expecto
{
    public static partial class CodeAnalyzer
    {

        private static void ExportToXmlByMethods(XmlDocument doc, ClassInfo classInfo, XmlElement classElement)
        {

            // Sort methods - public methods first
            var sortedMethods = classInfo.Methods
                .OrderBy(m => m.AccessModifier != "+") // Public methods first
                .ThenBy(m => m.Name); // Then by name

            // Add methods
            XmlElement methodsElement = doc.CreateElement("Methods");
            classElement.AppendChild(methodsElement);

            foreach (MethodData method in sortedMethods)
            {
                XmlElement methodElement = doc.CreateElement("Method");

                // Format parameters as "Type name, Type name, ..."
                string parameters = method.Parameters != null && method.Parameters.Any()
                    ? string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))
                    : "";

                methodElement.SetAttribute("v", $"{method.AccessModifier} {method.Name}({parameters}): {method.ReturnType}");
                if (method.Context != null)
                {
                    methodElement.SetAttribute("c", method.Context);
                }
                methodsElement.AppendChild(methodElement);
            }
        }


        private static void ExtractMethodsMetadata(Type type, ClassInfo classInfo)
        {
            // Get methods
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (MethodInfo method in methods)
            {
                string methodName = method.Name;

                // Skip property accessors
                if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
                {
                    continue; // Skip property accessor methods
                }

                // Skip event accessors
                if (methodName.StartsWith("add_") || methodName.StartsWith("remove_"))
                {
                    continue; // Skip event accessor methods
                }
                if (method.IsDefined(typeof(IgnoreCodeAnalyzerAttribute), false))
                {
                    continue;
                }

                // Skip anonymous methods and compiler-generated methods
                if (methodName.Contains("<") && (methodName.Contains(">b__") || methodName.Contains(">c__")))
                {
                    continue; // Skip this method
                }

                string accessModifier = GetAccessModifierSymbol(method);
                var parameters = method.GetParameters();
                var paramList = parameters != null && parameters.Length > 0
                    ? parameters.Select(p => new ParameterData
                    {
                        Type = GetFormattedTypeName(p.ParameterType),
                        Name = string.IsNullOrEmpty(p.Name) ? "param" : p.Name
                    }).ToList()
                    : new List<ParameterData>();

                string context = null;
                if (method.IsDefined(typeof(ContextCodeAnalyzerAttribute), false))
                {
                    context = method.GetCustomAttribute<ContextCodeAnalyzerAttribute>().Context;
                }

                classInfo.Methods.Add(new MethodData
                {
                    Name = methodName,
                    AccessModifier = accessModifier,
                    ReturnType = GetFormattedTypeName(method.ReturnType),
                    Parameters = paramList,
                    Context = context
                });
            }
        }
    }
}