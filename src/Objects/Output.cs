using System.Globalization;

namespace HaveIBeenPwned.AddressExtractor.Objects
{
    public static class Output
    {
        public static void Write(string line)
            => Output.Write(Console.Out, line);

        public static void Exception(Exception ex, bool trace = true)
            => Output.Write(Console.Error, trace ? ex : ex.Message);

        public static void Notice(string line)
            => Output.Write($"[NOTICE] {line}");

        /// <summary>Writes the the provided <paramref name="line"/> the first time Dispose is called, and never any time after</summary>
        public static IDisposable SingleNotice(string line)
            => new OnceNotices(line);

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

        /// <summary>
        /// Used for printing warnings to the output only one time
        /// </summary>
        private sealed class OnceNotices : IDisposable {
            private bool HasLogged = false;
            private object? Dummy;
            private object? Lock;
            
            private readonly string Message;
            
            public OnceNotices(string output) {
                this.Message = output;
            }
            
            /// <inheritdoc/>
            public void Dispose()
                => LazyInitializer.EnsureInitialized(
                    ref this.Dummy,
                    ref this.HasLogged,
                    ref this.Lock,
                    () => {
                        Output.Notice(this.Message);
                        return null;
                    }
                );
        }
    }
}
