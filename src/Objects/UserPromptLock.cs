using Microsoft.VisualStudio.Threading;

namespace HaveIBeenPwned.AddressExtractor.Objects;

internal sealed class UserPromptLock(CancellationTokenSource source)
{
    /// <summary>A Semaphore with a single handle, so multiple Tasks cannot prompt</summary>
    private readonly SemaphoreSlim Semaphore = new(1);

    private readonly AsyncManualResetEvent Holder = new(/* True by default to allow reading */ initialState: true);

    internal async ValueTask<bool> PromptAsync(CancellationToken cancellation = default)
    {
        // Wait to enter the semaphore
        await Semaphore.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            // If cancelled already, exit
            if (source.IsCancellationRequested)
            {
                return false;
            }

            // Reset allowed-continue to false
            Holder.Reset();
            try
            {
                Console.Write("Continue? [y/n]: ");

                // Wait for a Y/N keypress and ignore any others
                while (true)
                {
                    var read = Console.ReadKey(intercept: true);

                    // No modifiers (shift/ctrl/alt)
                    if (read.Modifiers is 0)
                    {
                        switch (read.Key)
                        {
                            // Allow continuing
                            case ConsoleKey.Y:
                                return true;

                            // Exit
                            case ConsoleKey.N:
                            case ConsoleKey.Escape:
                                await source.CancelAsync().ConfigureAwait(false);
                                return false;
                        }
                    }
                }

            }
            finally
            {
                Console.WriteLine();

                // Set allowed-continue to true (Only if not exiting)
                if (!source.IsCancellationRequested)
                {
                    Holder.Set();
                }
            }
        }
        finally
        {
            // Release the semaphore lock
            Semaphore.Release();
        }
    }

    public Task WaitAsync(CancellationToken cancellation = default)
        => Holder.WaitAsync(cancellation);
}
