using System.Diagnostics;
using System.Reflection;

using HaveIBeenPwned.AddressExtractor.Objects;

namespace HaveIBeenPwned.AddressExtractor;

public static class Extensions
{
    public static void AddAll<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, IEnumerable<TKey> keys, TVal value)
    {
        foreach (var key in keys)
        {
            dictionary[key] = value;
        }
    }

    public static TimeSpan GetAndReset(this Stopwatch stopwatch)
    {
        var millis = stopwatch.Elapsed;
        stopwatch.Restart();
        return millis;
    }

    public static string Format(this Stopwatch stopwatch, TimeUnit precision = TimeUnit.MICROSECONDS)
        => stopwatch.Elapsed.Format(precision);

    public static string Format(this TimeSpan span, TimeUnit precision = TimeUnit.MICROSECONDS)
        => TimeUnitExtensions.Format(span.Total(precision), precision);

    public static double Total(this TimeSpan span, TimeUnit unit) => unit switch
    {
        TimeUnit.MICROSECONDS => span.TotalMicroseconds,
        TimeUnit.MILLISECONDS => span.TotalMilliseconds,
        TimeUnit.SECONDS => span.TotalSeconds,
        TimeUnit.MINUTES => span.TotalMinutes,
        TimeUnit.HOURS => span.TotalHours,
        TimeUnit.DAYS => span.TotalDays,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
    };

    /// <summary>
    /// A safe call to <see cref="string.StartsWith(string)"/> and <see cref="string.EndsWith(string)"/>
    /// Guarantees that the string is actually long enough for the start and end to be different
    /// (eg; `"abc".StartsAndEndsWith("abc")` will be FALSE, and `"abcabc".StartsAndEndsWith("abc" will be TRUE)`)
    /// </summary>
    public static bool StartsAndEndsWith(this string check, string value)
        => check.Length >= value.Length * 2 && check.StartsWith(value) && check.EndsWith(value);

    /// <summary>
    /// A safe call to <see cref="string.StartsWith(char)"/> and <see cref="string.EndsWith(char)"/>
    /// Guarantees that the string is actually long enough for the start and end to be different
    /// (eg; `"abc".StartsAndEndsWith("abc")` will be FALSE, and `"abcabc".StartsAndEndsWith("abc" will be TRUE)`)
    /// </summary>
    public static bool StartsAndEndsWith(this string check, char value)
        => check.Length >= 2 && check.StartsWith(value) && check.EndsWith(value);

    /// <summary>
    /// Just here for testing
    /// </summary>
    public static bool NextBool(this Random random)
        => random.NextDouble() >= 0.5d;

    #region Reflection

    public static ConstructorInfo? GetConstructorWithTypes(this Type type, params Type[] types)
    {
        ConstructorInfo? constructor = null;
        var p = -1;

        foreach (var info in type.GetConstructors())
        {
            var parameters = info.GetParameters();
            if (parameters.Length > p && parameters.All(parameter => types.Contains(parameter.ParameterType)))
            {
                constructor = info;
                p = parameters.Length;
            }
        }

        return constructor;
    }

    public static object? InvokeMatch(this ConstructorInfo constructor, params object[] values)
    {
        var mapped = values.ToDictionary(value => value.GetType());
        var parameters = constructor.GetParameters();
        var array = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (!mapped.TryGetValue(parameter.ParameterType, out var value))
            {
                return null;
            }

            array[i] = value;
        }

        return constructor.Invoke(array);
    }

    #endregion
}
