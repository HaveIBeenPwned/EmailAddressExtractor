using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

using Microsoft.VisualStudio.Threading;

namespace HaveIBeenPwned.AddressExtractor.Objects;

public sealed class Runtime
{
    /// <summary>The Runtime Configuration</summary>
    public readonly Config Config;

    /// <summary>The options for reading/writing Json</summary>
    public readonly JsonSerializerOptions Json;

    /// <summary>File Extension Parsers created for Reading file types</summary>
    private readonly IReadOnlyDictionary<string, FileExtensionParsing> FileExtensions;
    private readonly FileExtensionParsing UnknownFileExtension = new() { Error = "Unknown Extension" };

    private readonly JoinableTaskFactory TaskFactory;

    #region Asynchronous Waiting

    private readonly UserPromptLock PromptLock;
    private readonly CancellationTokenSource ProgramCancellation;
    public CancellationToken CancellationToken => ProgramCancellation.Token;
    private static readonly string[] archiveExtensions = [".tar", ".gz", ".zip", ".rar", ".7z"];
    private static readonly string[] imageExtensions = [".png", ".tif", ".jpg", ".jpeg", ".gif", ".bmp", ".ai", ".psd", ".svg", ".ico"];
    private static readonly string[] mediaExtensions = [".rec", ".mp3", ".wav", ".mp4", ".mpg", ".mov", ".wmv", ".avi", ".m4v"];
    private static readonly string[] sqlExtensions = [".frm", ".ibd", ".myi", ".myd"];
    private static readonly string[] sourceCodeExtensions = [".go", ".py", ".js", ".yml", ".php", ".c", ".sh", ".css", ".less", ".npmignore", ".groovy", ".scala", ".sass", ".ascx", ".markdown", ".bash", ".sln", ".h", ".ts", ".cs", ".aspx", ".csproj", ".nupk", ".suo", ".asax", ".resx", ".refesh", ".ipch"];
    private static readonly string[] sourceControlExtensions = [".svn-base", ".gitignore", ".gitattributes", ".pack"];
    private static readonly string[] executableExtensions = [".exe", ".dll", ".apk", ".jar", ".java", "bin"];
    private static readonly string[] unsupportedExtensions = [".msi", ".flv", ".swf", ".pdb", ".brd", ".hprof", ".lock", ".docker", ".ttf", ".woff", ".woff2", ".pem", ".crt"];
    private static readonly string[] notSupportedYetExtensions = [".log", ".json", ".txt", ".sql", ".xml", ".sample", ".csv", ".tsv", ".odt", ".docx", ".pptx", ".xls", ".doc", ".ppt", ".pdf", ".rdb"];
    #endregion

    public Runtime(Config? config = null)
    {
        ProgramCancellation = new CancellationTokenSource();
        PromptLock = new UserPromptLock(ProgramCancellation);

        Config = config ?? new Config();
        FileExtensions = CreateExtensions();

        TaskFactory = new JoinableTaskFactory(new JoinableTaskContext());

        Json = new JsonSerializerOptions
        {
            // Use 'unsafe' Json parsing (We're not worried about dirty json as a client)
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,

            // Numbers can be read from strings or floating points
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,

            // Don't need to write indented to files
            WriteIndented = false,

            // Don't leave out any C# object members
            IncludeFields = true,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false
        };
    }

    #region CLI Waiters

    internal bool WaitInput(FileCollection files)
    {
        // If silent output don't prompt
        if (Config.SkipPrompts)
        {
            return true;
        }

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
                            Output.Write($"  [{ByteExtensions.Format(file.Length),-10}] {file.FullName}");
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
        => WaitOnExceptionAsync(CancellationToken);

    internal async ValueTask<bool> WaitOnExceptionAsync(CancellationToken cancellation)
        => Config.SkipExceptions // If silent output don't prompt
        || await PromptLock.PromptAsync(cancellation).ConfigureAwait(false);

    internal Task AwaitContinuationAsync(CancellationToken cancellation = default)
        => PromptLock.WaitAsync(cancellation);

    #endregion
    #region Tasks

    public JoinableTask<T> ExecuteAsync<T>(Func<Task<T>> func)
    {
        return TaskFactory.RunAsync(func);
    }

    #endregion
    #region File Extensions

    private ConcurrentDictionary<string, FileExtensionParsing> CreateExtensions()
    {
        var extensions = new ConcurrentDictionary<string, FileExtensionParsing>(StringComparer.OrdinalIgnoreCase);

        // Archives
        extensions.AddAll(
            archiveExtensions,
            new FileExtensionParsing { Error = "Archive files" }
        );

        // Images
        extensions.AddAll(
            imageExtensions,
            new FileExtensionParsing { Error = "Image files" }
        );

        // AudioVideo
        extensions.AddAll(
            mediaExtensions,
            new FileExtensionParsing { Error = "Audio/Video files" }
        );

        // Sql
        extensions.AddAll(
            sqlExtensions,
            new FileExtensionParsing { Error = "Sql files" }
        );

        // Code
        extensions.AddAll(
            sourceCodeExtensions,
            new FileExtensionParsing { Error = "Code files" }
        );

        // Source Control
        extensions.AddAll(
            sourceControlExtensions,
            new FileExtensionParsing { Error = "Source-control files" }
        );

        // Executables
        extensions.AddAll(
            executableExtensions,
            new FileExtensionParsing { Error = "Executables" }
        );

        // Other
        extensions.AddAll(
            unsupportedExtensions,
            new FileExtensionParsing { Error = "Unsupported" }
        );

        // A set of all extensions that should be eventually supported
        // These are overriden using Reflection and do not need to be removed
        extensions.AddAll(
            notSupportedYetExtensions,
            new FileExtensionParsing { Error = "Not currently supported" }
        );

        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(ILineReader)));
        foreach (var type in types)
        {
            if (type.GetCustomAttribute<ExtensionTypesAttribute>() is { } attribute)
            {
                var parsing = new FileExtensionParsing(this, type);

                // Use the setter to override any other instances
                foreach (var extension in attribute.Extensions)
                {
                    extensions[extension] = parsing;
                }
            }
        }

        return extensions;
    }

    internal FileExtensionParsing GetExtension(string extension)
        => FileExtensions.GetValueOrDefault(extension, UnknownFileExtension);

    internal FileExtensionParsing GetExtension(FileInfo info)
        => FileExtensions.GetValueOrDefault(info.Extension, UnknownFileExtension);

    internal FileExtensionParsing GetExtensionFromPath(string path)
        => FileExtensions.GetValueOrDefault(Path.GetExtension(path), UnknownFileExtension);

    #endregion
    #region Debugging

    public bool ShouldDebug(Exception ex)
        => Config.Debug && ex is not NotImplementedException;

    #endregion
}
