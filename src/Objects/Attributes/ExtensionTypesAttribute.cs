namespace HaveIBeenPwned.AddressExtractor.Objects.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ExtensionTypesAttribute(params string[] extensions) : Attribute
{
    public readonly string[] Extensions = extensions;
}
