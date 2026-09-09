namespace HaveIBeenPwned.AddressExtractor.Objects;

/// <summary>
/// A batch of lines read from a file.
/// <para>
/// Lines are handed to the parsing tasks in batches rather than individually because a
/// bounded <see cref="System.Threading.Channels.Channel{T}"/> costs about the same per
/// item regardless of how much work that item carries. At one line per item the handoff
/// cost dominated the pipeline and left the parsing tasks starved.
/// </para>
/// </summary>
public struct LineBatch
{
    /// <summary>The file the lines were read from</summary>
    public required string File { get; init; }

    /// <summary>
    /// The buffer holding the batch. Only the first <see cref="Count"/> entries are valid;
    /// the buffer is not reused once published, so readers may hold it freely.
    /// </summary>
    public required string[] Lines { get; init; }

    /// <summary>The number of valid entries in <see cref="Lines"/></summary>
    public required int Count { get; init; }

    /// <summary>The count of extracted Email Addresses</summary>
    public required Count Counter { get; init; }

    /// <summary>The line number of <see cref="Lines"/>[0] within the file</summary>
    public required long StartNumber { get; init; }
}
