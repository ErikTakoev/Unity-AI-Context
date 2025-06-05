using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Expecto
{
    public static partial class CodeAnalyzer
    {

        private static void ExportToXmlByFields(XmlDocument doc, ClassInfo classInfo, XmlElement classElement)
        {
            // Sort fields - public fields first
            var sortedFields = classInfo.Fields
                .OrderBy(f =>
                {
                    // Order by visibility:
                    // 1. Public properties
                    // 2. Public fields
                    // 3. Public properties with private/protected setter
                    // 4. All others

                    if (f.IsProperty)
                    {
                        if (f.GetterModifier == "+")
                        {
                            // Public properties with public getter
                            if (f.SetterModifier == "+")
                            {
                                // Public property with public setter
                                return 0;
                            }
                            else
                            {
                                // Public properties with private/protected setter
                                return 2;
                            }
                        }
                        else
                        {
                            // Non-public properties
                            return 3;
                        }
                    }
                    else
                    {
                        // Regular fields
                        if (f.AccessModifier == "+")
                        {
                            // Public fields
                            return 1;
                        }
                        else
                        {
                            // Non-public fields
                            return 4;
                        }
                    }
                })
                .ThenBy(f => f.Name); // Then sort by name

            // Add fields
            XmlElement fieldsElement = doc.CreateElement("Fields");
            classElement.AppendChild(fieldsElement);

            foreach (FieldData field in sortedFields)
            {
                XmlElement fieldElement = doc.CreateElement("Field");
                string fieldText;

                if (field.IsProperty)
                {
                    // Use combined modifier for properties (e.g. "+-" for public getter, private setter)
                    string combinedModifier = field.GetterModifier + field.SetterModifier;
                    fieldText = $"{combinedModifier} {field.Name}: {field.Type}";
                }
                else
                {
                    fieldText = $"{field.AccessModifier} {field.Name}: {field.Type}";
                }

                fieldElement.SetAttribute("v", fieldText);
                if (field.Context != null)
                {
                    fieldElement.SetAttribute("c", field.Context);
                }
                fieldsElement.AppendChild(fieldElement);
            }
        }

        private static void ExtractFieldsMetadata(Type type, ClassInfo classInfo)
        {
            // Get properties first and remember their names to avoid showing similar fields
            HashSet<string> propertyNames = new HashSet<string>();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (PropertyInfo property in properties)
            {
                // Only include properties that are declared in this type (not inherited)
                if (property.DeclaringType != type)
                {
                    continue;
                }

                string getterModifier = "-";
                string setterModifier = "-";

                // Determine access modifier for getter
                if (property.GetMethod != null)
                {
                    getterModifier = GetAccessModifierSymbol(property.GetMethod);
                }

                // Determine access modifier for setter
                if (property.SetMethod != null)
                {
                    setterModifier = GetAccessModifierSymbol(property.SetMethod);
                }

                string typeName = GetFormattedTypeName(property.PropertyType);

                classInfo.Fields.Add(new FieldData
                {
                    Name = property.Name,
                    Type = typeName,
                    GetterModifier = getterModifier,
                    SetterModifier = setterModifier,
                    IsProperty = true
                });

                // Store both exact name and lowercase version for case-insensitive comparison
                propertyNames.Add(property.Name);
                propertyNames.Add(property.Name.ToLowerInvariant());
            }

            // Get fields
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (FieldInfo field in fields)
            {
                // Skip fields declared in parent classes
                if (field.DeclaringType != type)
                {
                    continue;
                }

                // Skip backing fields for auto-properties
                if (field.Name.Contains("k__BackingField"))
                {
                    continue;
                }

                if (field.IsDefined(typeof(IgnoreCodeAnalyzerAttribute), false))
                {
                    continue;
                }

                string fieldName = field.Name;

                // Skip fields that match property names or case-insensitive versions
                // Check if there's a property with the same name or a capitalized version of this field
                string capitalizedFieldName = char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
                if (propertyNames.Contains(fieldName) || propertyNames.Contains(capitalizedFieldName))
                {
                    continue;
                }

                string accessModifier = GetAccessModifierSymbol(field);
                string typeName = GetFormattedTypeName(field.FieldType);
                string context = null;
                if (field.IsDefined(typeof(ContextCodeAnalyzerAttribute), false))
                {
                    context = field.GetCustomAttribute<ContextCodeAnalyzerAttribute>().Context;
                }

                classInfo.Fields.Add(new FieldData
                {
                    Name = fieldName,
                    Type = typeName,
                    AccessModifier = accessModifier,
                    Context = context
                });
            }
        }
    }
}