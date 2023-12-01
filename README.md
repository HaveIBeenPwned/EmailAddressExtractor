# The Have I Been Pwned Email Address Extractor
A project to rapidly extract all email addresses from any files in a given path

# Background

This project is intended to be a brand new open source version of a basic codebase I've used for the better part of a decade to extract email addresses from data breaches before loading them into HIBP. Most breaches are in a .sql or .csv format either in a single file or multiple files within a folder and extraction follows a simple process:

1. Extract all addresses via regex
2. Convert them to lowercase
3. Order them alphabetically
4. Save them to an output file
5. Create a report of how many unique addresses were in each file

The regex I've used is as follows: `\b[a-zA-Z0-9\.\-_\+]+@[a-zA-Z0-9\.\-_]+\.[a-zA-Z]+\b`

[Email address validation via regex is hard](https://www.troyhunt.com/dont-trust-net-web-forms-email-regex/), but it also doesn't need to be perfect for this use case. False positives are extremely rare and the impact is negligible, namely that a string that isn't a genuine address gets loaded into HIBP *or* a genuine address of an unusual format gets loaded. For the most part, this regex can be summarised as "stuff either side of an @ symbol with a TLD of alphas characters".

# Practical Considerations

Inevitably discussion has led to compliance with RFC versus the practical uses of certain characters when considering parsing rules. There are 2 primary considerations here:

1. Where the RFC allows a character such as a double quote, it should be considered a valid character and permitted in addresses
2. A character such as a double quote is more likely to be present as the result of a parsing error rather than legitimate use

Anecdotally, point 1 is rarely ever true in comparison to point 2. The impact of falsely rejecting a legitimate spec-compliant address is that it doesn't end up in HIBP (i.e. low impact). The impact of allowing addresses that don't actually exist is that junk records are introduced into HIBP (also low impact). Especially when considering the likelihood of an address with obscure characters being practically used (for example, accepted into a registration form and not rejected), on balance it is preferable to reject characters that are likely the result of parsing errors.

1. When extracting addresses 

Realistically, obscure patterns are unlikely to be used in email addresses

# Contributions

[I've reached out and asked for support](https://twitter.com/troyhunt/status/1637966167548780544) and will get things kicked off via one or two key people then seek broader input. I'm particularly interested in optimising the service across larger data sets and non text-based files, especially with the uptick of documents being dumped by ransomware crews. I'll start creating issues for the bits that need building.

# Test data

Using Red Gate's SQL Data Generator, [a sample file containing 10M records of typical breach data is available to download from Mega](https://mega.nz/file/Xk91ETzb#UYklfa84pLs5OzrysEGNFVMbFb5OC0KU7rlnugF_Aps). This file results in exactly 10M email addresses being extracted with the current version of this app. Note: the test data file is presently in V2, with the earlier version resulting in slightly less than 10M unique addresses due to the presence of invalid domain name patterns.

# Running the Address Extractor

Syntax: `AddressExtractor.exe -?`
Syntax: `AddressExtractor.exe -v`
Syntax: `AddressExtractor.exe <input [[... input]]> [-o output] [-r report]`

### Main Options

| Option                  | Description                                                                |
|-------------------------|----------------------------------------------------------------------------|
| `-?`, `-h`, `--help`    | Prints the command line syntax and options                                 |
| `-v`, `--version`       | Prints the application version number                                      |
| input                   | One or more input filenames or directories                                 |
| `-o`, `--output` output | Path and filename of the output file. Defaults to 'addresses_output.txt'   |
| `-r`, `--report` report | Path and filename of the report file. Defaults to 'report.txt'             |
| `--recursive`           | Enable recursive mode for directories, which will search child directories |
| `-y`, `--yes`           | Automatically confirm prompts to CONTINUE without asking                   |
| `-q`, `--quiet`         | Run with less verbosity, progress messages aren't shown                    |

### Performance / Debugging

| Option              | Description                                                                                                                                      |
|---------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|
| `--debug`           | Enable debug mode for fine-tuned performance checking                                                                                            |
| `--threads` num     | Uses multiple threads with [channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) for reading from files. Defaults to `4` |
| `--skip-exceptions` | Automatically prompts on CONTINUE when an exception occurs                                                                                       |
