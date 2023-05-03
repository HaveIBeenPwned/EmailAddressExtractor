# Welcome!

Thanks for your interest in contributing to the [HIBP](https://haveibeenpwned.com/) Email Address Extractor!

## Contributing

Lorem ipsum

### Test Methods

There are a number of TestMethods available in the `AddressExtractorTest` namespace. You can use these to validate that the program is still running a-okay, and that emails are properly being parsed.

### Performance

Please make sure to use the `--debug` command-line option to test your changes. Being able to quickly run through a data dump is best!

### File Extensions / File Types

The Email Extractor is built for extracting Email Addresses from strings, but first it must be able to read those strings from files! These files mostly come from data dumps that aren't always in the prettiest format.

Different types of File Readers can be created to support new file types.

To create a FileType reader you start by creating a new class (Preferably in `~/Objects/Readers/`) that implements the `ILineReader` interface. The interface consists of only one method which can asynchronously read Lines from a File.

```csharp
public interface ILineReader : IAsyncDisposable
{
    /// <summary>Read and return string segments to be checked for email addresses</summary>
    IAsyncEnumerable<string?> ReadLineAsync(CancellationToken cancellation = default);
}
```

You can create a new Reader class using the example below (**File readers are automatically picked up by the application using reflection**<sub>1</sub>). You can specify which file extensions your Reader class supports by using an `ExtensionTypesAttribute`, the Attribute must be provided the extensions that your reader supports in order for it to be used.

- <sub>1</sub> Reflection will pick a constructor that accepts a string for the file path.

```csharp
[ExtensionTypes(".extension", ".someotherextension")]
public sealed class MyFileReader : ILineReader
{
    private readonly string File;

    // A new instance of the reader is constructed for every file.
    // This allows us to store local variables if need be while reading through the file
    // We should accept the path here in the constructor, which we'll make use of later
    public MyFileReader(string file)
    {
        // The file when passed into the constructor is a valid system file
        // that matches the file extension for this reader
        this.File = file;
    }

    // ReadLineAsync is our reader method where we can return strings back to the EmailExtractor
    public async IAsyncEnumerable<string?> ReadLineAsync(CancellationToken cancellation = default)
    {
        // Return any addresses
        yield return "address@domain.com";
    }

    // ILineReader implemented IDisposableAsync, so we can clean up any resources
    // if need be.
    public ValueTask DisposeAsync()
        // If there is nothing to clean up, we can simply return a Completed Task.
        => ValueTask.CompletedTask;
}
```

### Regex

After reading content from files, the main address extractor in `src/AddressExtractor.cs` does a loose Regex matching of text that contains an email, and then uses Filters to update and validate the captured strings.

### Filtering

Filters can be created (and typically found in `src/Objects/Filters`) by creating a class that extends `AddressFilter.BaseFilter`.

`BaseFilter` has two primary Methods (`ValidateEmailAddress` and `ValidateEmailAddressAsync`) and a Property (`Name`). The Name Property is logged when running in `--debug` mode.

- When implementing a new Filter, either the async method or non-async can be overridden.
- Applying the `AddressFilterAttribute` to your Filter allows you to set a `Priority`, higher priority filters will run before lower priority filters
- Unlike `ILineReader` only one Filter object is constructed when the program starts

```csharp
[AddressFilter(Priority = 0)]
public sealed class MyFilter : AddressFilter.BaseFilter
{
    public override string Name => "My Filter";

    // Validating an Email Address returns a 'Result', the 'Result' controls
    //     how the filtering is handled.
    // - Returning 'CONTINUE' will continue running through the remaining filters
    //     until all filters have been passed
    // - Returning 'REVALIDATE' will cause all filters to re-run again, this
    //     is useful if you've updated the EmailAddress value
    // - Returning 'ALLOW' will end filtering, and add the EmailAddress value
    //     into the final result
    // - Returning 'DENY' means that the EmailAddress is not a valid email and
    //     will not be in the final result
    // - BaseFilter provides a 'Continue' method that will convert a bool to
    //     'Result', true is 'CONTINUE' and false is 'DENY'
    public override Result ValidateEmailAddress(ref EmailAddress address)
    {
        // Here in the body you can access part of the EmailAddress for filtering
        //   'address.Full' can also be updated if the email needs to be trimmed
        //   If you update 'address.Full' you should return 'REVALIDATE' to re-run filters
        
        return Result.CONTINUE;
    }
}
```