using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using MyAddressExtractor.Objects.Attributes;
using MyAddressExtractor.Objects.Readers;

namespace MyAddressExtractor.Objects {
    public sealed class Runtime {
        /// <summary>The Runtime Configuration</summary>
        public readonly Config Config;

        /// <summary>Filters created for filtering <see cref="EmailAddress"/>es</summary>
        public readonly IEnumerable<AddressFilter.BaseFilter> Filters;

        /// <summary>File Extension Parsers created for Reading file types</summary>
        private readonly IReadOnlyDictionary<string, FileExtensionParsing> FileExtensions;
        private readonly FileExtensionParsing UnknownFileExtension = new() { Error = "Unknown Extension" };

        #region Asynchronous Waiting

        private readonly UserPromptLock PromptLock;
        private readonly CancellationTokenSource ProgramCancellation;
        public CancellationToken CancellationToken => this.ProgramCancellation.Token;

        #endregion

        public Runtime(Config? config = null)
        {
            this.ProgramCancellation = new CancellationTokenSource();
            this.PromptLock = new UserPromptLock(this.ProgramCancellation);

            this.Config = config ?? new Config();
            this.Filters = this.CreateFilters();
            this.FileExtensions = this.CreateExtensions();
        }

        #region CLI Waiters

        internal bool WaitInput(FileCollection files)
        {
            // If silent output don't prompt
            if (this.Config.SkipPrompts)
                return true;

            while (true)
            {
                Console.Write("Press ANY KEY to continue ['Q' to Quit; 'I' for info]: ");
                var read = Console.ReadKey(intercept: true);
                Console.WriteLine();

                // No modifiers (shift/ctrl/alt)
                if (read.Modifiers is 0)
                {
                    switch (read.Key)
                    {
                        case ConsoleKey.Q:
                            Output.Write("Exiting");
                            return false;
                        case ConsoleKey.I:
                            Output.Write($"Reading the following {files.Count} files:");
                            foreach (var file in files)
                            {
                                Output.Write($"  [{ByteExtensions.Format(file.Length).PadRight(10)}] {file.FullName}");
                            }
                            Console.WriteLine();
                            continue;
                        default:
                            return true;
                    }
                }
            }
        }

        internal ValueTask<bool> WaitOnExceptionAsync()
            => this.WaitOnExceptionAsync(this.CancellationToken);

        internal async ValueTask<bool> WaitOnExceptionAsync(CancellationToken cancellation)
            => this.Config.SkipExceptions // If silent output don't prompt
            || await this.PromptLock.PromptAsync(cancellation);

        internal Task AwaitContinuationAsync(CancellationToken cancellation = default)
            => this.PromptLock.WaitAsync(cancellation);

        #endregion
        #region Filters

        private FilterCollection CreateFilters()
        {
            var filters = new FilterCollection();
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(AddressFilter.BaseFilter)))
                .OrderByDescending(type => type.GetCustomAttribute<AddressFilterAttribute>()?.Priority ?? -1);
            foreach (Type type in types)
            {
                if (Activator.CreateInstance(type) is AddressFilter.BaseFilter filter)
                    filters.Add(filter);
            }

            return filters;
        }

        #endregion
        #region File Extensions

        private IReadOnlyDictionary<string, FileExtensionParsing> CreateExtensions()
        {
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
                    var parsing = new FileExtensionParsing(type);

                    // Use the setter to override any other instances
                    foreach (string extension in attribute.Extensions)
                        extensions[extension] = parsing;
                }
            }

            return extensions;
        }

        internal FileExtensionParsing GetExtension(string extension)
            => this.FileExtensions.GetValueOrDefault(extension, this.UnknownFileExtension);

        internal FileExtensionParsing GetExtension(FileInfo info)
            => this.FileExtensions.GetValueOrDefault(info.Extension, this.UnknownFileExtension);

        internal FileExtensionParsing GetExtensionFromPath(string path)
            => this.FileExtensions.GetValueOrDefault(Path.GetExtension(path), this.UnknownFileExtension);

        #endregion
        #region Debugging

        public bool ShouldDebug(Exception ex)
            => this.Config.Debug && ex is not NotImplementedException;

        #endregion
        #region Inner classes

        private sealed class FilterCollection : IEnumerable<AddressFilter.BaseFilter>
        {
            /// <summary>Use a List so that we maintain entry order</summary>
            private readonly IList<AddressFilter.BaseFilter> Filters = new List<AddressFilter.BaseFilter>();

            public void Add(AddressFilter.BaseFilter filter)
                => this.Filters.Add(filter);

            /// <inheritdoc />
            public IEnumerator<AddressFilter.BaseFilter> GetEnumerator()
                => this.Filters.GetEnumerator();

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
                => this.GetEnumerator();
        }
        
        #endregion
    }
}
