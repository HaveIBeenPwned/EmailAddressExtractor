using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace HaveIBeenPwned.AddressExtractor.Objects.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class CommandLineOptionAttribute(params string[] args) : Attribute
{
    /// <summary>Arguments that activate the command option</summary>
    public readonly string[] Args = args;

    /// <summary>The Option description that is printed in the Help message</summary>
    public required string Description { get; init; }

    /// <summary>If the Method returns a <see cref="Config.Writer"/>, the type of information to expect (Printed in the Help message)</summary>
    public string? Expects { get; init; }

    /// <summary>If the Option must be used exclusively by itself in order to work</summary>
    public bool Exclusive { get; init; }
}

internal sealed class CommandLineOption
{
    /// <inheritdoc cref="CommandLineOptionAttribute.Args"/>
    public readonly string[] Args;

    /// <inheritdoc cref="CommandLineOptionAttribute.Description"/>
    public readonly string Description;

    /// <inheritdoc cref="CommandLineOptionAttribute.Expects"/>
    public readonly string? Expects;

    /// <inheritdoc cref="CommandLineOptionAttribute.Exclusive"/>
    public readonly bool IsExclusive;

    /// <summary>If the Option expects an input</summary>
    public bool ExpectsArgument => MemberInfo switch
    {
        null => false,
        MethodInfo method => method.ReturnType == typeof(Config.Writer),
        PropertyInfo property => property.CanWrite && property.PropertyType != typeof(bool),
        _ => false
    };

    private readonly MemberInfo? MemberInfo;

    public CommandLineOption(MemberInfo method, CommandLineOptionAttribute attribute)
    {
        Args = MapArgs(attribute.Args);
        Description = GetDescription(attribute.Description);
        Expects = attribute.Expects;
        IsExclusive = attribute.Exclusive;
        MemberInfo = method;
    }
    public CommandLineOption(string argument, string description)
    {
        Args = [argument];
        Description = description;
        Expects = null;
        IsExclusive = false;
        MemberInfo = null;
    }

    public bool IsMatch(string arg)
        => Args.Contains(arg);

    public Config.Writer? Invoke(Config config)
    {
        if (MemberInfo is null)
        {
            throw new NotImplementedException("Cannot invoke Options that don't provide a Method");
        }

        if (MemberInfo is MethodInfo method)
        {
            if (ExpectsArgument)
            {
                var result = method.Invoke(config, Array.Empty<object>());
                return result as Config.Writer;
            }

            method.Invoke(config, Array.Empty<object>());
        }
        else if (MemberInfo is PropertyInfo { CanWrite: true } property)
        {
            return CreateWriter(config, property);
        }

        return null;
    }

    public string JoinArgs()
    {
        var val = string.Join(", ", Args);
        if (ExpectsArgument)
        {
            val += $" <{Expects ?? GetExpects()}>";
        }

        return val;
    }

    #region Helpers

    private static string[] MapArgs(string[] input)
    {
        var args = new string[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var val = input[i];
            var r = val.Length is 1 ? 1 : 2;

            args[i] = $"{new string('-', r)}{val}";
        }

        return args;
    }

    private static Config.Writer? CreateWriter(Config config, PropertyInfo property)
    {
        var type = property.PropertyType;

        if (type == typeof(int))
        {
            return input =>
            {
                int? min = null;
                int? max = null;

                if (property.GetCustomAttribute<RangeAttribute>() is { } range)
                {
                    min = range.Minimum as int?;
                    max = range.Maximum as int?;
                }

                var val = ParseInt(input, min, max);
                property.SetValue(config, val);
            };
        }

        if (type == typeof(string))
        {
            return input =>
            {
                property.SetValue(config, input);
            };
        }

        if (type.IsEnum)
        {
            return input =>
            {
                if (!Enum.TryParse(type, input, ignoreCase: true, out var value))
                {
                    throw new ArgumentException($"Invalid value \"{input}\"");
                }

                property.SetValue(config, value);
            };
        }

        if (type == typeof(bool))
        {
            var val = property.GetValue(config) as bool?;
            property.SetValue(config, val is not true);

            // Bools don't return a writer
            return null;
        }

        return property.CanWrite ? throw new NotImplementedException($"Creating Writer for \"{type}\" not implemented") : null;
    }

    private string GetExpects()
    {
        if (MemberInfo is PropertyInfo property)
        {
            var type = property.PropertyType;
            if (type == typeof(int))
            {
                return "#";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type.IsEnum)
            {
                return type.Name.ToLowerInvariant();
            }
        }
        return "input";
    }

    private string GetDescription(string input)
    {
        if (MemberInfo is PropertyInfo { PropertyType: Type type } && type.IsEnum)
        {
            var values = Enum.GetNames(type).Select(val => val.ToLowerInvariant());
            input += $" ({string.Join(", ", values)})";
        }
        return input;
    }

    #endregion
    #region Parsers

    private static int ParseInt(string value, int? min = null, int? max = null)
    {
        if (!int.TryParse(value, out var i))
        {
            throw new ArgumentException("Value must be a number");
        }

        if (i < min)
        {
            throw new ArgumentException($"Value cannot be less than {min}");
        }

        if (i > max)
        {
            throw new ArgumentException($"Value cannot be more than {max}");
        }

        return i;
    }

    #endregion
}
