namespace MyAddressExtractor.Objects.Attributes {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ExtensionTypesAttribute : Attribute
    {
        public readonly string[] Extensions;

        public ExtensionTypesAttribute(params string[] extensions)
        {
            this.Extensions = extensions;
        }
    }
}
