# Welcome!

Thanks for your interest in contributing to the [HIBP](https://haveibeenpwned.com/) Email Address Extractor!

## Contributing

Lorem ipsum

### Test Methods

There are a number of TestMethods available in the `AddressExtractorTest` namespace. You can use these to validate that the program is still running a-okay, and that emails are properly being parsed.

### Performance

Please make sure to use the `--debug` command-line option to test your changes. Being able to quickly run through a data dump is best!

### File Extensions

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

You can create a new Reader class like so:

```csharp
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

After we create our `ILineReader` class, we need to add it to our `~/FileExtensionParsing.cs`:

```csharp
static FileExtensionParsing()
{
    /* ... */
    
    extensions.AddAll(
        // Provide all of the supported extension types our reader can handle
        new[] { ".txt", ".json", ".html" },
        
        // Here we create the information object
        new FileExtensionParsing {
            // The information object holds a 'Reader' Function
            // This is the constructor that returns your 'ILineReader'
            // A new Reader is created for every 'path'
            Reader = path => new MyFileReader(path)
        }
    );
    
    /* ... */
}
```
