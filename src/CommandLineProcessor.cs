using System.Reflection;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor;

public class CommandLineProcessor
{
    internal static readonly IEnumerable<CommandLineOption> _cliOptions;
    static CommandLineProcessor()
    {
        List<CommandLineOption> options = [];

        var methods = typeof(Config).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var member in methods)
        {
            if (member.GetCustomAttribute<CommandLineOptionAttribute>() is not { } attribute)
            {
                continue;
            }

            if (member is MethodInfo method && method.GetParameters().Length is not 0)
            {
                throw new ArgumentException($"Option {method.Name} should not have input parameters");
            }

            options.Add(new CommandLineOption(member, attribute));
        }

        _cliOptions = options.Order(new CommandLineOptionSorter());
    }

    internal static Config Parse(IReadOnlyCollection<string> args, out IList<string> inputFilePaths)
    {
        if (args.Count == 0)
        {
            throw new ArgumentException("Please provide at least one input file path.");
        }

        Config config = new();
        Config.Writer? handle = null;
        var previous = string.Empty;

        inputFilePaths = [];
        foreach (var arg in args)
        {
            if (handle is not null)
            {
                // Is it an option?
                if (arg.Length > 0 && arg[0] == '-')
                {
                    throw new ArgumentException($"Missing value for '{previous}' option");
                }

                try
                {
                    handle(arg);
                    handle = null;
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Error reading option '{previous}' ({e.Message})");
                }
            }
            else
            {
                // Is it an option?
                if (arg.Length > 0 && arg[0] == '-')
                {
                    if (arg.Length <= 1)
                    {
                        throw new ArgumentException($"Invalid argument '{arg}'");
                    }

                    CommandLineOption? option = _cliOptions.FirstOrDefault(option => option.IsMatch(arg)) ?? throw new ArgumentException($"Unexpected option '{arg}'");

                    // Exclusive options (eg; -h or -v)
                    if (option.IsExclusive && args.Count > 1)
                    {
                        throw new ArgumentException($"'{arg}' must be the only argument when it is used");
                    }

                    handle = option.Invoke(config);

                    // Return since the option is exclusive
                    if (option.IsExclusive)
                    {
                        return config;
                    }
                }
                else // No option or not expecting an option file path, so assume it to be an input file path
                {
                    inputFilePaths.Add(arg);
                }
            }

            previous = arg;
        }

        // If there are no more arguments but we were expecting a option file path, alert the user
        if (handle is not null)
        {
            throw new ArgumentException($"Missing output file path after {previous} option");
        }

        // Make sure we have at least one input file path
        if (inputFilePaths.Count == 0)
        {
            throw new ArgumentException("No input file paths specified");
        }

        return config;
    }

    private class CommandLineOptionSorter : IComparer<CommandLineOption>
    {
        /// <inheritdoc />
        public int Compare(CommandLineOption? x, CommandLineOption? y)
            => x is null || y is null ? 0 : Compare(GetChar(x), GetChar(y));

        private static int Compare(char x, char y)
            => x.CompareTo(y);

        private static char GetChar(CommandLineOption option)
        {
            var c = default(char);

            foreach (var arg in option.Args)
            {
                if (arg.StartsWith("--"))
                {
                    c = arg[2];
                }
                else if (arg.StartsWith('-'))
                {
                    c = arg[1];
                }
                else
                {
                    c = arg[0];
                }

                if (char.IsLetter(c))
                {
                    return c;
                }
            }

            return c;
        }
    }
}
