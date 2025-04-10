namespace HaveIBeenPwned.AddressExtractor;

/// <summary>
/// A simple glorified <see cref="int"/> wrapper.
/// Allows storing the <see cref="Count"/> inside of any <see cref="IDictionary{TKey,TValue}"/> and accesing the value by reference
/// (Used as the int Reference is updated as a file is read through).
/// </summary>
public sealed class Count
{
    public int Value => _Value;
    private int _Value;

    public void Increment()
        => Interlocked.Increment(ref _Value);

    /// <inheritdoc />
    public override string ToString()
        => $"{Value:n0}";
}
