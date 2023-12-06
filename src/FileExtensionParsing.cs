using System.Reflection;
using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

namespace HaveIBeenPwned.AddressExtractor {
    internal sealed class FileExtensionParsing
    {
        public bool Read => this.Error is null;
        public string? Error { get; init; } = null;

        private ReaderDelegate? Reader { get; init; } = null;

        public FileExtensionParsing() {}
        public FileExtensionParsing(Runtime runtime, Type type)
        {
            this.Reader = FileExtensionParsing.CreateInstance(runtime, type);
        }

        public ILineReader GetReader(string path)
        {
            if (!this.Read)
                throw new Exception("Cannot read files of this type");
            return this.Reader?.Invoke(path) ?? throw new NullReferenceException($"A {typeof(ILineReader)} could not be created for files of this type");
        }

        #region Static Accessors

        private static ReaderDelegate CreateInstance(Runtime runtime, Type type)
        {
            ConstructorInfo? info = type.GetConstructorWithTypes(typeof(string), typeof(Runtime), typeof(Config));
            if (info is not null)
                return path => info.InvokeMatch(path, runtime, runtime.Config) as ILineReader ?? throw new Exception("Failed to construct Reader");
            throw new NotSupportedException($"Could not find a valid constructor for {type}");
        }

        /// <summary>
        /// A delegate for constructing a <see cref="ILineReader"/> capable of reading <see cref="string"/>s from a <see cref="path"/>
        /// When called, <see cref="path"/> is an existing system File
        /// </summary>
        private delegate ILineReader ReaderDelegate(string path);

        #endregion
    }
}
