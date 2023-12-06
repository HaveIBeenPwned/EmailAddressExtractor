using Microsoft.VisualStudio.Threading;

namespace HaveIBeenPwned.AddressExtractor.Objects {
    internal sealed class UserPromptLock {
        /// <summary>A Semaphore with a single handle, so multiple Tasks cannot prompt</summary>
        private readonly SemaphoreSlim Semaphore = new(1);

        private readonly AsyncManualResetEvent Holder = new(/* True by default to allow reading */ initialState: true);

        private readonly CancellationTokenSource Source;

        public UserPromptLock(CancellationTokenSource source)
        {
            this.Source = source;
        }

        internal async ValueTask<bool> PromptAsync(CancellationToken cancellation = default)
        {
            // Wait to enter the semaphore
            await this.Semaphore.WaitAsync(cancellation);
            try {
                // If cancelled already, exit
                if (this.Source.IsCancellationRequested)
                    return false;

                // Reset allowed-continue to false
                this.Holder.Reset();
                try {
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
                                    await this.Source.CancelAsync().ConfigureAwait(false);
                                    return false;
                            }
                        }
                    }

                } finally {
                    Console.WriteLine();

                    // Set allowed-continue to true (Only if not exiting)
                    if (!this.Source.IsCancellationRequested)
                        this.Holder.Set();
                }
            } finally {
                // Release the semaphore lock
                this.Semaphore.Release();
            }
        }

        public Task WaitAsync(CancellationToken cancellation = default)
            => this.Holder.WaitAsync(cancellation);
    }
}
