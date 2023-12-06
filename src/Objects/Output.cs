using System.Globalization;

namespace HaveIBeenPwned.AddressExtractor.Objects
{
    public static class Output
    {
        public static void Write(string line)
            => Output.Write(Console.Out, line);

        public static void Exception(Exception ex, bool trace = true)
            => Output.Write(Console.Error, trace ? ex : ex.Message);

        public static void Error(string line)
            => Output.Write(Console.Error, line);

        private static void Write(TextWriter to, object? e)
        {
            if (e is null)
                return;
            to.WriteLine($"[{Output.DateTime()}] {e}");
        }

        private static string DateTime()
            => System.DateTime.Now.ToString(CultureInfo.CurrentCulture);
    }
}
