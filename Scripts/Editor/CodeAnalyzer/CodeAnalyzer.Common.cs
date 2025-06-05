using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Expecto
{
    public static partial class CodeAnalyzer
    {
        private static string GetFormattedTypeName(Type type)
        {
            // Handle array types
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                string elementTypeName = GetFormattedTypeName(elementType);
                return $"{elementTypeName}[]";
            }

            // For non-generic types, just return the C# name
            if (!type.IsGenericType)
                return GetCSharpTypeName(type.Name);

            // Get the generic type name without the `n suffix
            string baseName = type.Name;
            int backtickIndex = baseName.IndexOf('`');
            if (backtickIndex > 0)
            {
                baseName = baseName.Substring(0, backtickIndex);
            }

            // Convert base name to C# alias if applicable
            baseName = GetCSharpTypeName(baseName);

            // Get the generic arguments
            Type[] genericArgs = type.GetGenericArguments();
            string[] argNames = genericArgs.Select(GetFormattedTypeName).ToArray();

            // For XML output, use encoded brackets
            return $"{baseName}<{string.Join(", ", argNames)}>";
        }

        private static string GetCSharpTypeName(string typeName)
        {
            // Map .NET type names to C# aliases
            switch (typeName)
            {
                case "Int32": return "int";
                case "Int64": return "long";
                case "Single": return "float";
                case "Double": return "double";
                case "Boolean": return "bool";
                case "Char": return "char";
                case "Byte": return "byte";
                case "Int16": return "short";
                case "UInt16": return "ushort";
                case "UInt32": return "uint";
                case "UInt64": return "ulong";
                case "SByte": return "sbyte";
                case "Decimal": return "decimal";
                case "String": return "string";
                case "Object": return "object";
                case "Void": return "void";
                default: return typeName;
            }
        }
    }
}