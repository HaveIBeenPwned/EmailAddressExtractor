using System.Reflection;
using MyAddressExtractor.Objects.Readers;

namespace MyAddressExtractor {
    internal sealed class FileExtensionParsing
    {
        public bool Read => this.Error is null;
        public string? Error { get; init; } = null;

        private ReaderDelegate? Reader { get; init; } = null;

        public FileExtensionParsing() {}
        public FileExtensionParsing(Type type)
        {
            this.Reader = FileExtensionParsing.CreateInstance(type);
        }

        public ILineReader GetReader(string path)
        {
            if (!this.Read)
                throw new Exception("Cannot read files of this type");
            return this.Reader?.Invoke(path) ?? throw new NullReferenceException($"A {typeof(ILineReader)} could not be created for files of this type");
        }

        #region Static Accessors

        private static ReaderDelegate CreateInstance(Type type)
        {
            ConstructorInfo? info = type.GetConstructor(new []{ typeof(string) });
            if (info is not null)
                return path => info.Invoke(new object[] { path }) as ILineReader ?? throw new Exception("Failed to construct Reader");

            info = type.GetConstructor(Array.Empty<Type>());
            if (info is not null)
                return _ => info.Invoke(Array.Empty<object>()) as ILineReader ?? throw new Exception("Failed to construct Reader");

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
