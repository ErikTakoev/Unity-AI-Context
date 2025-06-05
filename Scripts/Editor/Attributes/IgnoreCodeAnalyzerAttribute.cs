using System;

namespace Expecto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Field)]
    public class IgnoreCodeAnalyzerAttribute : Attribute
    {
    }
}