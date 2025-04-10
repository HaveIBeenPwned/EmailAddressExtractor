using System.Globalization;

namespace HaveIBeenPwned.AddressExtractor.Objects;

public enum Bytes
{
    BYTE = 0,
    KB = 1,
    MB = 2,
    GB = 3,
    TB = 4,
    PB = 5,
    EB = 6,
    ZB = 7,
    YB = 8
}

public static class ByteExtensions
{
    public static double Convert(this Bytes to, long fromAmount, Bytes from = Bytes.BYTE)
    {
        if (to == from)
        {
            return fromAmount;
        }

        var diff = Math.Abs(to - from);
        var multiplier = Math.Pow(1000, diff);

        double result;
        if (to < from)
        {
            result = fromAmount * multiplier;
        }
        else
        {
            result = fromAmount / multiplier;
        }

        return result;
    }

    public static string Format(this Bytes bytes) => bytes switch
    {
        Bytes.BYTE => "Bytes",
        Bytes.KB => "Kb",
        Bytes.MB => "Mb",
        Bytes.GB => "Gb",
        Bytes.TB => "Tb",
        Bytes.PB => "Pb",
        Bytes.EB => "Eb",
        Bytes.ZB => "Zb",
        Bytes.YB => "Yb",
        _ => throw new ArgumentOutOfRangeException(nameof(bytes), bytes, null)
    };

    public static string Format(long fromAmount, Bytes from = Bytes.BYTE)
    {
        var size = from;
        var result = (double)fromAmount;

        foreach (var bytes in Enum.GetValues<Bytes>().OrderDescending())
        {
            if (bytes <= from)
            {
                continue;
            }

            var conversion = bytes.Convert(fromAmount, from);

            if (conversion >= 0.1d)
            {
                size = bytes;
                result = conversion;
                break;
            }
        }

        return $"{string.Format(CultureInfo.InvariantCulture, $"{{0:F{size.Decimals()}}}", result)} {size.Format()}";
    }

    public static int Decimals(this Bytes unit)
        => unit > Bytes.BYTE ? 1 : 0;
}
