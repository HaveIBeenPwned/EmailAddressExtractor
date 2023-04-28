namespace MyAddressExtractor.Objects.Performance {
    /// <summary>
    /// A performance stack object for debugging performance
    /// </summary>
    public interface IPerformanceStack : IDisposable {
        static readonly IPerformanceStack DEFAULT = new DefaultPerformanceStack();

        IPerformanceStack CreateStack(string name);

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
