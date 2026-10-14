# Repository guidance

## Purpose and behavior

This is the Have I Been Pwned email address extractor: a .NET 10 console
application for extracting addresses from breach-data files. It normalizes
addresses to lowercase, deduplicates them, saves sorted output, and reports
per-file counts.

Validation deliberately favors practical addresses over full RFC compliance.
Preserve existing extraction and validation behavior unless a change explicitly
calls for different rules. Read `README.md` for intent, but check the current
implementation and tests before relying on documented details.

## Repository map

- `AddressExtractor.sln`: application and test solution.
- `src\Program.cs`: CLI entry point, orchestration, error handling, and exit codes.
- `src\CommandLineProcessor.cs` and `src\Objects\Config.cs`: argument parsing,
  option definitions, and configuration.
- `src\FileCollection.cs`: input-file discovery.
- `src\AddressExtractor.cs`: non-backtracking regex matching, validation,
  lowercase normalization, and per-call deduplication.
- `src\EmailValidation.cs`: alias/domain validation and IANA TLD lookup.
- `src\AddressExtractorMonitor.cs`: channel-based batch processing, worker tasks,
  quick file scans, counts, progress, and saving results.
- `src\Objects\Runtime.cs` and `src\FileExtensionParsing.cs`: reader discovery,
  construction, cancellation, and interactive prompts.
- `src\Objects\Readers\`: format-specific readers implementing `ILineReader`.
- `test\`: MSTest tests for extraction, the monitor, CLI parsing, quick scans,
  and legacy behavior.
- `TestData\`, `valid.txt`, and `invalid.txt`: checked-in test fixtures.

`CONTRIBUTING.md` explains reader extension points, but its filter guidance is
outdated: the current extractor validates through `EmailValidation`, not a
filter pipeline.

## Build and run

Use the .NET 10 SDK. Run commands from the repository root:

```powershell
dotnet build AddressExtractor.sln
dotnet test test\AddressExtractorTest.csproj
dotnet test test\AddressExtractorTest.csproj --filter "FullyQualifiedName~CommandLineProcessorTests"
dotnet run --project src\AddressExtractor.csproj -- --help
```

For extraction, pass input paths after `--`; use `--yes` to avoid the initial
interactive confirmation and `--debug` when investigating performance.
Use explicit output/report paths outside fixture directories for smoke runs.

Email validation lazily fetches the IANA TLD list over HTTP on first use.
Tests exercising valid domains therefore require network access; report
network-related failures rather than weakening validation to make tests pass.

## Change conventions

Follow `.editorconfig`: four-space C# indentation, file-scoped namespaces,
braces on separate lines, `var` for locals, and system usings first. Both
projects enable nullable reference types and implicit usings.

Keep the streaming, asynchronous design. Forward cancellation tokens, dispose
readers and streams, and follow surrounding `ConfigureAwait(false)` usage.
Avoid loading whole large inputs into memory. In hot paths, preserve span-based
matching, low allocations, batching, and per-worker regex ownership.

New readers belong in `src\Objects\Readers\`, implement `ILineReader` and
`IAsyncDisposable`, and declare supported extensions with `ExtensionTypes`.
Reader discovery uses reflection; follow existing constructor patterns using
`string`, `Runtime`, and/or `Config`. Include format-specific extraction tests
and cover cancellation/resource cleanup where applicable.

Use the existing `Output` helpers for notices and errors. Preserve established
prompt, quiet-mode, debug-mode, and exit-code behavior when changing the CLI.
Update related option definitions, parsing tests, and README usage together.

Add focused regression tests alongside the existing MSTest tests. Use unique
temporary paths and clean them up in `finally` blocks; do not overwrite fixtures.
Run the smallest relevant test selection first and broader tests when warranted.
For performance changes, compare representative input with `--debug` and verify
that extracted addresses and per-file counts remain unchanged.

Do not commit real breach data, generated extraction results, build artifacts,
or local IDE settings. Keep changes scoped to the requested behavior.
