using System.Globalization;

namespace HaveIBeenPwned.AddressExtractor.Objects;

public static class Output
{
    public static void Write(string line)
        => Write(Console.Out, line);

    public static void Exception(Exception ex, bool trace = true)
        => Write(Console.Error, trace ? ex : ex.Message);

    public static void Notice(string line)
        => Write($"[NOTICE] {line}");

    /// <summary>Writes the the provided <paramref name="line"/> the first time Dispose is called, and never any time after</summary>
    public static IDisposable SingleNotice(string line)
        => new OnceNotices(line);

    public static void Error(string line)
        => Write(Console.Error, line);

    private static void Write(TextWriter to, object? e)
    {
        if (e is null)
        {
            return;
        }

        to.WriteLine($"[{DateTime()}] {e}");
    }

    private static string DateTime()
        => System.DateTime.Now.ToString(CultureInfo.CurrentCulture);

    /// <summary>
    /// Used for printing warnings to the output only one time
    /// </summary>
    private sealed class OnceNotices(string output) : IDisposable
    {
        private bool HasLogged;
        private object? Dummy;
        private object? Lock;

        /// <inheritdoc/>
        public void Dispose()
            => LazyInitializer.EnsureInitialized(
                ref Dummy,
                ref HasLogged,
                ref Lock,
                () =>
                {
                    Notice(output);
                    return null;
                }
            );
    }
}
