using System.Collections.Generic;

namespace Expecto
{
    internal class MethodData
    {
        public string Name { get; set; }
        public string AccessModifier { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterData> Parameters { get; set; }
        public string Context { get; set; }
    }

    internal class ParameterData
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}