namespace HaveIBeenPwned.AddressExtractor;

/// <summary>
/// A simple glorified <see cref="long"/> wrapper.
/// Allows storing the <see cref="Count"/> inside of any <see cref="IDictionary{TKey,TValue}"/> and accesing the value by reference
/// (Used as the int Reference is updated as a file is read through).
/// </summary>
public sealed class Count
{
    public long Value => _value;
    private long _value;

    public void Increment()
        => Interlocked.Increment(ref _value);

    /// <inheritdoc />
    public override string ToString()
        => $"{Value:n0}";
}
