namespace Expecto
{
    internal class FieldData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string AccessModifier { get; set; }
        public string GetterModifier { get; set; }
        public string SetterModifier { get; set; }
        public bool IsProperty { get; set; }
        public string Context { get; set; }
    }
}