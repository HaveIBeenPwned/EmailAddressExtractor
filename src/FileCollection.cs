using System.Collections;
using System.Collections.Concurrent;
using MyAddressExtractor.Objects;

namespace MyAddressExtractor {
    internal sealed class FileCollection : IEnumerable<FileInfo>
    {
        private readonly Config Config;
        private readonly IDictionary<string, FileInfo> Files;

        public int Count => this.Files.Count;

        public FileCollection(Config config, IEnumerable<string> inputs)
        {
            this.Config = config;
            this.Files = this.CreateSystemSet();
            foreach (var file in this.GatherFiles(inputs))
                this.Files[file.FullName] = file;
            this.Log();
        }

        private IEnumerable<FileInfo> GatherFiles(IEnumerable<string> inputs, bool recursed = false)
        {
            foreach (string file in inputs) {
                FileAttributes attributes = File.GetAttributes(file);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (!recursed || this.Config.OperateRecursively)
                    {
                        foreach (var enumerated in this.GatherFiles(Directory.EnumerateFileSystemEntries(file), recursed: true))
                        {
                            yield return enumerated;
                        }
                    }
                }
                else if (File.Exists(file))
                {
                    yield return new FileInfo(file);
                }
            }
        }

        /// <summary>
        /// Gather our <see cref="IEnumerable{String}"/> as a Set so that we don't have duplicates
        /// Windows uses a Case-Insensitive File system, so on it we can mostly ignore casing
        /// </summary>
        private IDictionary<string, FileInfo> CreateSystemSet()
        {
            OperatingSystem os = Environment.OSVersion;
            if (os.Platform is PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE)
            {
                return new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
            }

            return new Dictionary<string, FileInfo>();
        }

        private void Log()
        {
            var infos = new ConcurrentDictionary<string, ExtensionInfo>(StringComparer.OrdinalIgnoreCase);
            var count = this.Files.Count; // Cache the count before removing

            foreach (var file in this)
            {
                if (file.Extension is {Length: >0} extension)
                {
                    var info = infos.GetOrAdd(extension, _ => new ExtensionInfo(extension.ToLower()));
                    info.AddFile(file);

                    // Remove ignored files
                    if (!info.Parsing.Read)
                        this.Files.Remove(file.FullName);
                }
            }

            var sorted = infos.Values
                .OrderBy(info => info.Parsing.Read ? -info.Count : 0);
            Output.Write($"Found {count:n0} files:");
            foreach (ExtensionInfo info in sorted)
            {
                Output.Write($"  {info.Extension.PadRight(6)} {info.Count:n0} files: {ByteExtensions.Format(info.Bytes)}{(info.Parsing.Read ? string.Empty : $", Skipping ({info.Parsing.Error})")}");
            }

            string output = this.Config.OutputFilePath;
            string report = this.Config.ReportFilePath;
            Output.Write($"Output will {(string.IsNullOrWhiteSpace(output) ? "not be saved" : $"be saved to \"{output}\"")}.");
            Output.Write($"Report will {(string.IsNullOrWhiteSpace(report) ? "not be saved" : $"be saved to \"{report}\"")}.");
        }

        /// <inheritdoc />
        public IEnumerator<FileInfo> GetEnumerator()
            => this.Files.Values.GetEnumerator();

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
