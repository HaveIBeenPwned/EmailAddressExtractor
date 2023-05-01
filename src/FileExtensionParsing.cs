using System.Collections.Concurrent;
using MyAddressExtractor.Objects.Readers;

namespace MyAddressExtractor {
    internal sealed class FileExtensionParsing
    {
        private static readonly IReadOnlyDictionary<string, FileExtensionParsing> FILE_EXTENSIONS;
        private static readonly FileExtensionParsing UNKNOWN = new() { Error = "Unknown Extension" };
        static FileExtensionParsing() {
            var extensions = new ConcurrentDictionary<string, FileExtensionParsing>(StringComparer.OrdinalIgnoreCase);
            
            // Accepted plaintext files
            extensions.AddAll(
                new[] { ".log", ".json", ".txt", ".sql", ".xml", ".sample", ".csv", ".tsv" },
                new FileExtensionParsing {
                    Reader = path => new PlainTextReader(path)
                    // No error means OK!
                }
            );
            
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
            
            // Future
            extensions.AddAll(
                new[] { ".xls", ".doc", ".docx", ".ppt", ".pptx", ".pdf", ".rdb" },
                new FileExtensionParsing { Error = "Not currently supported" }
            );
            
            // Future - OpenDoc
            extensions.AddAll(
                new[] { ".odt", ".docx" },
                new FileExtensionParsing { Reader = path => new OpenDocumentTextReader(path) }
            );
            
            FileExtensionParsing.FILE_EXTENSIONS = extensions;
        }
        
        public bool Read => this.Error is null;
        public string? Error { get; init; } = null;
        
        private ReaderDelegate? Reader { get; init; } = null;
        
        public ILineReader GetReader(string path) {
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
        
        /// <summary>
        /// A delegate for constructing a <see cref="ILineReader"/> capable of reading <see cref="string"/>s from a <see cref="path"/>
        /// When called, <see cref="path"/> is an existing system File
        /// </summary>
        private delegate ILineReader ReaderDelegate(string path);
        
        #endregion
    }
}
