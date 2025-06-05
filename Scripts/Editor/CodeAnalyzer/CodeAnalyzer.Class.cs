using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Expecto
{
    public static partial class CodeAnalyzer
    {
        private static void ExportToXmlByClass(XmlDocument doc, XmlElement root, ClassInfo classInfo)
        {
            XmlElement classElement = doc.CreateElement("Class");

            classElement.SetAttribute("n", classInfo.Name);

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
                    classElement.SetAttribute("b", baseClassName);
                }
            }

            if (classInfo.Context != null)
            {
                classElement.SetAttribute("c", classInfo.Context);
            }

            // Export fields and methods
            ExportToXmlByFields(doc, classInfo, classElement);
            ExportToXmlByMethods(doc, classInfo, classElement);

            root.AppendChild(classElement);
        }

        private static void ExtractTypeMetadata(Type type, List<ClassInfo> classes, HashSet<string> processedClassNames)
        {
            // Skip generic type definitions (like List<T>, Dictionary<TKey, TValue>)
            if (type.IsGenericTypeDefinition)
                return;

            string classContext = null;
            if (type.IsDefined(typeof(ContextCodeAnalyzerAttribute), false))
            {
                classContext = type.GetCustomAttribute<ContextCodeAnalyzerAttribute>().Context;
            }

            // Get the class name, handling generic types
            string className = type.Name;
            if (type.IsGenericType)
            {
                className = GetFormattedTypeName(type);
            }

            // Skip if we've already processed this class name
            if (processedClassNames.Contains(className))
                return;

            processedClassNames.Add(className);

            ClassInfo classInfo = new ClassInfo
            {
                Name = className,
                Namespace = type.Namespace,
                BaseClass = type.BaseType?.FullName,
                Fields = new List<FieldData>(),
                Methods = new List<MethodData>(),
                Context = classContext
            };
            ExtractFieldsMetadata(type, classInfo);
            ExtractMethodsMetadata(type, classInfo);

            classes.Add(classInfo);
        }


        /// <summary>
        /// Finds all generic types used in fields, properties, and methods in the specified assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to search in.</param>
        /// <param name="namespaceFilters">The namespace filters to apply.</param>
        /// <returns>A HashSet of generic types found in the assemblies.</returns>
        private static HashSet<Type> FindGenericTypesInAssemblies(IEnumerable<Assembly> assemblies, List<string> namespaceFilters)
        {
            HashSet<Type> genericTypesToProcess = new HashSet<Type>();

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    // Skip if not in our target namespaces
                    if (type.Namespace == null || !namespaceFilters.Any(filter => type.Namespace == filter))
                        continue;

                    // Check fields
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        CollectGenericTypes(field.FieldType, genericTypesToProcess);
                    }

                    // Check properties
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        CollectGenericTypes(property.PropertyType, genericTypesToProcess);
                    }

                    // Check method parameters and return types
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    {
                        CollectGenericTypes(method.ReturnType, genericTypesToProcess);

                        foreach (ParameterInfo param in method.GetParameters())
                        {
                            CollectGenericTypes(param.ParameterType, genericTypesToProcess);
                        }
                    }
                }
            }

            return genericTypesToProcess;
        }
    }
}
