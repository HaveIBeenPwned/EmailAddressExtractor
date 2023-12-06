namespace HaveIBeenPwned.AddressExtractor.Objects.Performance {
    /// <summary>
    /// A performance stack object for debugging performance
    /// </summary>
    public interface IPerformanceStack : IDisposable {
        static readonly IPerformanceStack DEFAULT = new DefaultPerformanceStack();

        /// <summary>
        /// Create a new nested stack
        /// </summary>
        IPerformanceStack CreateStack(string name);

        /// <summary>
        /// Add a new measured step in the current stack
        /// </summary>
        void Step(string name);

        void Log();

        private sealed class DefaultPerformanceStack : IPerformanceStack {
            /// <inheritdoc />
            public IPerformanceStack CreateStack(string name)
                => this;

            /// <inheritdoc />
            public void Step(string name) {}

            public void Log() {}

            void IDisposable.Dispose() {}
        }
    }
}
