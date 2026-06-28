using System.Collections.Concurrent;

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
    private readonly ConcurrentDictionary<string, byte> _addresses = new(StringComparer.OrdinalIgnoreCase);

    public void Increment()
        => Interlocked.Increment(ref _value);

    public bool TryAdd(string address)
    {
        if (!_addresses.TryAdd(address, 0))
        {
            return false;
        }

        Increment();
        return true;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{Value:n0}";
}
