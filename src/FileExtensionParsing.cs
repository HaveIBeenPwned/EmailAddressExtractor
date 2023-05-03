using System.Collections.Concurrent;
using System.Reflection;
using MyAddressExtractor.Objects.Attributes;
using MyAddressExtractor.Objects.Readers;

namespace MyAddressExtractor {
    internal sealed class FileExtensionParsing
    {
        private static readonly IReadOnlyDictionary<string, FileExtensionParsing> FILE_EXTENSIONS;
        private static readonly FileExtensionParsing UNKNOWN = new() { Error = "Unknown Extension" };
        static FileExtensionParsing() {
            var extensions = new ConcurrentDictionary<string, FileExtensionParsing>(StringComparer.OrdinalIgnoreCase);

            // Archives
            extensions.AddAll(
                new[] { ".tar", ".gz", ".zip", ".rar", ".7z" },
                new FileExtensionParsing { Error = "Archive files" }
            );

            // Images
            extensions.AddAll(
                new[] { ".png", ".tif", ".jpg", ".jpeg", ".gif", ".bmp", ".ai", ".psd", ".svg", ".ico" },
                new FileExtensionParsing { Error = "Image files" }
            );

            // AudioVideo
            extensions.AddAll(
                new[] { ".rec", ".mp3", ".wav", ".mp4", ".mpg", ".mov", ".wmv", ".avi", ".m4v" },
                new FileExtensionParsing { Error = "Audio/Video files" }
            );

            // Sql
            extensions.AddAll(
                new[] { ".frm", ".ibd", ".myi", ".myd" },
                new FileExtensionParsing { Error = "Sql files" }
            );

            // Code
            extensions.AddAll(
                new[] { ".go", ".py", ".js", ".yml", ".php", ".c", ".sh", ".css", ".less", ".npmignore", ".groovy", ".scala", ".sass", ".ascx", ".markdown", ".bash", ".sln", ".h", ".ts", ".cs", ".aspx", ".csproj", ".nupk", ".suo", ".asax", ".resx", ".refesh", ".ipch" },
                new FileExtensionParsing { Error = "Code files" }
            );

            // Source Control
            extensions.AddAll(
                new[] { ".svn-base", ".gitignore", ".gitattributes", ".pack" },
                new FileExtensionParsing { Error = "Source-control files" }
            );

            // Executables
            extensions.AddAll(
                new[] { ".exe", ".dll", ".apk", ".jar", ".java",  "bin" },
                new FileExtensionParsing { Error = "Executables" }
            );

            // Other
            extensions.AddAll(
                new[] { ".msi", ".flv", ".swf", ".pdb", ".brd", ".hprof", ".lock", ".docker", ".ttf", ".woff", ".woff2", ".pem", ".crt" },
                new FileExtensionParsing { Error = "Unsupported" }
            );

            // A set of all extensions that should be eventually supported
            // These are overriden using Reflection and do not need to be removed
            extensions.AddAll(
                new[] { ".log", ".json", ".txt", ".sql", ".xml", ".sample", ".csv", ".tsv", ".odt", ".docx", ".pptx", ".xls", ".doc", ".ppt", ".pdf", ".rdb" },
                new FileExtensionParsing { Error = "Not currently supported" }
            );

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(ILineReader)));
            foreach (var type in types)
            {
                if (type.GetCustomAttribute<ExtensionTypesAttribute>() is {} attribute)
                {
                    var parsing = new FileExtensionParsing {
                        Reader = file => FileExtensionParsing.CreateInstance(type, file)
                    };

                    // Use the setter to override any other instances
                    foreach (string extension in attribute.Extensions)
                        extensions[extension] = parsing;
                }
            }

            FileExtensionParsing.FILE_EXTENSIONS = extensions;
        }

        public bool Read => this.Error is null;
        public string? Error { get; init; } = null;

        private ReaderDelegate? Reader { get; init; } = null;

        public ILineReader GetReader(string path)
        {
            if (!this.Read)
                throw new Exception("Cannot read files of this type");
            return this.Reader?.Invoke(path) ?? throw new NullReferenceException($"A {typeof(ILineReader)} could not be created for files of this type");
        }

        #region Static Accessors

        public static FileExtensionParsing Get(string extension)
            => FileExtensionParsing.FILE_EXTENSIONS.GetValueOrDefault(extension, FileExtensionParsing.UNKNOWN);

        public static FileExtensionParsing Get(FileInfo info)
            => FileExtensionParsing.FILE_EXTENSIONS.GetValueOrDefault(info.Extension, FileExtensionParsing.UNKNOWN);

        public static FileExtensionParsing GetFromPath(string path)
            => FileExtensionParsing.FILE_EXTENSIONS.GetValueOrDefault(Path.GetExtension(path), FileExtensionParsing.UNKNOWN);

        private static ILineReader CreateInstance(Type type, string path)
        {
            ConstructorInfo? info = type.GetConstructor(new []{ typeof(string) });

            if (info?.Invoke(new object[] { path }) is ILineReader readerWithPath)
                return readerWithPath;

            info = type.GetConstructor(Array.Empty<Type>());
            if (info?.Invoke(Array.Empty<object>()) is ILineReader reader)
                return reader;

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
