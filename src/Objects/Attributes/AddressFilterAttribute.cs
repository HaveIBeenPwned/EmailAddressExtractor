namespace HaveIBeenPwned.AddressExtractor.Objects.Attributes {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AddressFilterAttribute : Attribute {
        public int Priority { get; set; } = 0;
    }
}
