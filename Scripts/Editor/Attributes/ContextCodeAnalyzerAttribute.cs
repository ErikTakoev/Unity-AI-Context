using System;

namespace Expecto
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property)]
    public class ContextCodeAnalyzerAttribute : Attribute
    {
#if UNITY_EDITOR
        public string Context { get; }
#endif
        public ContextCodeAnalyzerAttribute(string context)
        {
#if UNITY_EDITOR
            Context = context;
#endif
        }
    }
}