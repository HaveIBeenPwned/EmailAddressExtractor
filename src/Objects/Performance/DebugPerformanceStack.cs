using System.Diagnostics;

namespace HaveIBeenPwned.AddressExtractor.Objects.Performance;

public sealed class DebugPerformanceStack : IPerformanceStack
{
    private readonly DebugPerformanceStack? Parent;
    private readonly NodeAverage Node;
    private readonly ReaderWriterLockSlim Lock = new();

    public string Name => Node.Name;

    /// <summary>An ordered list of <see cref="Nodes"/>.Values and <see cref="Children"/>.Values</summary>
    private readonly List<object> Entries = [];

    /// <summary>A set of averages that are grouped within this <see cref="DebugPerformanceStack"/></summary>
    private readonly Dictionary<string, NodeAverage> Nodes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Children <see cref="DebugPerformanceStack"/>s</summary>
    private readonly Dictionary<string, DebugPerformanceStack> Children = new(StringComparer.OrdinalIgnoreCase);

    private readonly DateTimeOffset StartTime;
    private readonly Stopwatch Stopwatch;

    /// <summary>We evenly space log names so we need to know the longest length name</summary>
    private int MaxLength;

    public DebugPerformanceStack() : this(null, string.Empty) { }
    private DebugPerformanceStack(DebugPerformanceStack? parent, string name)
    {
        Parent = parent;
        Node = new NodeAverage(name);
        StartTime = DateTimeOffset.UtcNow;
        Stopwatch = Stopwatch.StartNew();
        MaxLength = name.Length;
    }

    private void AddEntry(object entry)
    {
        Entries.Add(entry);

        string name;
        if (entry is DebugPerformanceStack stack)
        {
            name = stack.Name;
        }
        else if (entry is NodeAverage average)
        {
            name = average.Name;
        }
        else
        {
            return;
        }

        if (name.Length > MaxLength)
        {
            MaxLength = name.Length;
        }
    }

    /// <inheritdoc />
    public IPerformanceStack CreateStack(string name)
        => new DebugPerformanceStack(this, name);

    /// <inheritdoc />
    public void Step(string name)
    {
        Lock.EnterWriteLock();
        try
        {
            // Update the logging width
            var len = name.Length;
            if (len > MaxLength)
            {
                MaxLength = len;
            }

            if (!Nodes.TryGetValue(name, out var node))
            {
                node = new NodeAverage(name);

                AddEntry(node);

                Nodes[name] = node;
            }

            node.Add(Stopwatch.GetAndReset());
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    private void Write(DebugPerformanceStack child)
    {
        Lock.EnterWriteLock();
        try
        {
            if (Children.TryGetValue(child.Name, out var stack))
            {
                stack.Merge(child);
            }
            else
            {
                AddEntry(child);

                Children[child.Name] = child;
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    private void Merge(DebugPerformanceStack other)
    {
        Lock.EnterWriteLock();
        try
        {
            Node.Merge(other.Node);

            foreach (var entry in other.Entries)
            {
                if (entry is NodeAverage node)
                {
                    if (Nodes.TryGetValue(node.Name, out var val))
                    {
                        val.Merge(node);
                    }
                    else
                    {
                        AddEntry(node);

                        Nodes[node.Name] = node;
                    }
                }
                else if (entry is DebugPerformanceStack child)
                {
                    Write(child);
                }
            }
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Log()
        => Log(0);
    private void Log(int i)
    {
        Lock.EnterReadLock();
        try
        {
            var buffer = i * 2;
            foreach (var entry in Entries)
            {
                if (entry is DebugPerformanceStack stack)
                {
                    if (!string.IsNullOrEmpty(stack.Name))
                    {
                        stack.Node.Log(buffer, MaxLength);
                    }

                    stack.Log(i + 1);
                }
                else if (entry is NodeAverage node)
                {
                    node.Log(buffer, MaxLength);
                }
            }

            // If we're not recursing add a blank line at the end
            if (Parent is null)
            {
                Console.WriteLine();
            }
        }
        finally
        {
            Lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Lock.EnterWriteLock();
        try
        {
            Node.Add(DateTimeOffset.UtcNow - StartTime);
            Stopwatch.Stop();
            Parent?.Write(this);
        }
        finally
        {
            Lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// This object is used for calculating the Millisecond Average that a <see cref="NodeAverage.Name"/>d action takes
    /// </summary>
    private sealed class NodeAverage(string name)
    {
        public readonly string Name = name;

        public TimeSpan Span { get; private set; }
        public int Counter { get; private set; }

        public void Add(TimeSpan span)
        {
            Span += span;
            Counter++;
        }

        public void Merge(NodeAverage other)
        {
            // Set the sum
            Span += other.Span;

            // Combine the counters
            Counter += other.Counter;
        }

        public void Log(int left = 0, int right = 0)
        {
            Console.WriteLine($"{new string(' ', left)} - {Name.PadRight(right)} x{Counter:n0} | Took {Span.Format()} (at ~{TimeUnitExtensions.Format(Span.TotalMicroseconds / Counter)} per)");
        }
    }
}
