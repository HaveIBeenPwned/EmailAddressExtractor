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

# Contributions

[I've reached out and asked for support](https://twitter.com/troyhunt/status/1637966167548780544) and will get things kicked off via one or two key people then seek broader input. I'm particularly interested in optimising the service across larger data sets and non text-based files, especially with the uptick of documents being dumped by ransomware crews. I'll start creating issues for the bits that need building.

# Test data

I'll generate some test data in different formats and drop those into this repository shortly.

# Running the Address Extractor

Syntax: `AddressExtractor.exe -?`
Syntax: `AddressExtractor.exe -v`
Syntax: `AddressExtractor.exe <input [[... input]]> [-o output] [-r report]`

| Option | Description |
| ------ | ----------- |
| -? | Prints the command line syntax and options |
| -v | Prints the application version number |
| input  | One or more input filenames |
| -o output | Path and filename of the output file. Defaults to 'addresses_output.txt' |
| -r report | Path and filename of the report file. Defaults to 'report.txt' |

