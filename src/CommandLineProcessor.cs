using MyAddressExtractor.Objects;

namespace MyAddressExtractor
{
    public class CommandLineProcessor
    {
        internal static Config Parse(IReadOnlyCollection<string> args, out IList<string> inputFilePaths)
        {
            if (args.Count == 0)
                throw new ArgumentException("Please provide at least one input file path.");

            Config config = new();
            Config.Writer? handle = null;
            string previous = string.Empty;

            inputFilePaths = new List<string>();
            foreach (var arg in args)
            {
                if (handle is not null)
                {
                    // Is it an option?
                    if (arg.Length > 0 && arg[0] == '-')
                        throw new ArgumentException($"Missing value for '{previous}' option");

                    try {
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
                    if (arg[0] == '-')
                    {
                        if (arg.Length > 1)
                            handle = config.Set(args, arg);
                        else
                            throw new ArgumentException($"Invalid argument '{arg}'");
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
                throw new ArgumentException($"Missing output file path after {previous} option");

            // Make sure we have at least one input file path
            if (inputFilePaths.Count == 0)
                throw new ArgumentException("No input file paths specified");

            return config;
        }
    }
}
