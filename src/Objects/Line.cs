namespace HaveIBeenPwned.AddressExtractor.Objects;

/// <summary>
/// A line read from a file
/// </summary>
public struct Line
{
    /// <summary>The file the line was read from</summary>
    public required string File { get; init; }

    /// <summary>The value of the Line</summary>
    public required string Value { get; init; }

    /// <summary>The count of extracted Email Addresses</summary>
    public required Count Counter { get; init; }

    /// <summary>The current line number</summary>
    public required long Number { get; init; }
}
