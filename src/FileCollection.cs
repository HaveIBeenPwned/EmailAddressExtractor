using System.Collections;
using System.Collections.Concurrent;

namespace MyAddressExtractor {
    internal sealed class FileCollection : IEnumerable<string>
    {
        private readonly CommandLineProcessor Cli;
        private readonly ISet<string> Files;

        public int Count => this.Files.Count;

        public FileCollection(CommandLineProcessor cli, IEnumerable<string> inputs)
        {
            this.Cli = cli;
            this.Files = this.CreateSystemSet();
            this.Files.UnionWith(this.GatherFiles(inputs));
            this.Log();
        }

        private IEnumerable<string> GatherFiles(IEnumerable<string> inputs, bool recursed = false)
        {
            foreach (string file in inputs) {
                FileAttributes attributes = File.GetAttributes(file);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (!recursed || this.Cli.OperateRecursively)
                    {
                        foreach (string enumerated in this.GatherFiles(Directory.EnumerateFileSystemEntries(file), recursed: true))
                        {
                            yield return enumerated;
                        }
                    }
                }
                else if (File.Exists(file))
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// Gather our <see cref="IEnumerable{String}"/> as a Set so that we don't have duplicates
        /// Windows uses a Case-Insensitive File system, so on it we can mostly ignore casing
        /// </summary>
        private ISet<string> CreateSystemSet()
        {
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform is PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return new HashSet<string>();
        }

        private void Log()
        {
            var infos = new ConcurrentDictionary<string, ExtensionInfo>(StringComparer.OrdinalIgnoreCase);
            var count = this.Files.Count; // Cache the count before removing
            foreach (string path in this)
            {
                var file = new FileInfo(path);
                if (file.Extension is {Length: >0} extension)
                {
                    var info = infos.GetOrAdd(extension, _ => new ExtensionInfo(extension.ToLower()));
                    info.AddFile(file);
                    
                    // Remove ignored files
                    if (!info.Parsing.Read)
                    {
                        this.Files.Remove(path);
                    }
                }
            }

            var sorted = infos.Values
                .OrderBy(info => info.Parsing.Read ? -info.Count : 0);
            Console.WriteLine($"Found {count:n0} files:");
            foreach (ExtensionInfo info in sorted)
            {
                Console.WriteLine($"- {info.Extension}: {info.Count:n0} files : {info.Bytes:n0} bytes{(info.Parsing.Read ? string.Empty : $", Skipping ({info.Parsing.Error})")}");
            }
        }

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator()
            => this.Files.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        private class ExtensionInfo {
            public readonly string Extension;
            public readonly FileExtensionParsing Parsing;

            public int Count { get; private set; }
            public long Bytes { get; private set; }

            public ExtensionInfo(string extension)
            {
                this.Extension = extension;
                this.Parsing = FileExtensionParsing.Get(extension);
            }

            public void AddFile(FileInfo info)
            {
                this.Count++;
                this.Bytes += info.Length;
            }
        }
    }
}
