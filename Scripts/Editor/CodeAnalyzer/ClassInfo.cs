using System.Collections.Generic;

namespace Expecto
{
    internal class ClassInfo
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string BaseClass { get; set; }
        public List<FieldData> Fields { get; set; }
        public List<MethodData> Methods { get; set; }
        public string Context { get; set; }
    }
}