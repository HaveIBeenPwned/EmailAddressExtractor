using System.Collections.Concurrent;
using System.Diagnostics;

namespace MyAddressExtractor.Objects.Performance {
    public sealed class DebugPerformanceStack : IPerformanceStack {
        private readonly DebugPerformanceStack? Parent;
        private readonly NodeAverage Node;
        public string Name => this.Node.Name;

        /// <summary>An ordered list of <see cref="Nodes"/>.Values and <see cref="Children"/>.Values</summary>
        private readonly List<object> Entries = new();

        /// <summary>A set of averages that are grouped within this <see cref="DebugPerformanceStack"/></summary>
        private readonly ConcurrentDictionary<string, NodeAverage> Nodes = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Children <see cref="DebugPerformanceStack"/>s</summary>
        private readonly ConcurrentDictionary<string, DebugPerformanceStack> Children = new(StringComparer.OrdinalIgnoreCase);

        private readonly DateTimeOffset StartTime;
        private readonly Stopwatch Stopwatch;

        /// <summary>We evenly space log names so we need to know the longest length name</summary>
        private int MaxLength;

        public DebugPerformanceStack() : this(null, string.Empty) {}
        private DebugPerformanceStack(DebugPerformanceStack? parent, string name)
        {
            this.Parent = parent;
            this.Node = new NodeAverage(name);
            this.StartTime = DateTimeOffset.UtcNow;
            this.Stopwatch = Stopwatch.StartNew();
            this.MaxLength = name.Length;
        }

        /// <inheritdoc />
        public IPerformanceStack CreateStack(string name)
            => new DebugPerformanceStack(this, name);

        private void AddEntry(object entry)
        {
            this.Entries.Add(entry);
            
            string name;
            if (entry is DebugPerformanceStack stack)
                name = stack.Name;
            else if (entry is NodeAverage average)
                name = average.Name;
            else return;
            if (name.Length > this.MaxLength)
                this.MaxLength = name.Length;
        }

        /// <inheritdoc />
        public void Step(string name)
        {
            // Update the logging width
            var len = name.Length;
            if (len > this.MaxLength)
                this.MaxLength = len;

            var span = this.Stopwatch.GetAndReset();

            var node = this.Nodes.GetOrAdd(name, i => {
                var node = new NodeAverage(i);
                this.AddEntry(node);
                return node;
            });

            node.Add(span);
        }

        private void Write(DebugPerformanceStack child)
        {
            this.Children.AddOrUpdate(
                // Add the entry using the name
                child.Name,
                // When first adding, also add to the Entries
                _ => {
                    this.AddEntry(child);
                    return child;
                },
                // We can't have duplicate keys, so add the averages together
                (s,  stack) => {
                    stack.Node.Merge(child.Node);

                    foreach (object entry in child.Entries) {
                        if (entry is NodeAverage node)
                        {
                            stack.Nodes.AddOrUpdate(node.Name,
                                _ => {
                                    stack.AddEntry(node);
                                    return node;
                                },
                                (_,  average) => {
                                    average.Merge(node);
                                    return average;
                                }
                            );
                        }
                        else if (entry is DebugPerformanceStack grandchild)
                        {
                            stack.Write(grandchild);
                        }
                    }
                    
                    return stack;
                }
            );
        }

        /// <inheritdoc />
        public void Log()
            => this.Log(0);
        private void Log(int i)
        {
            int buffer = i * 2;
            foreach (object entry in this.Entries)
            {
                if (entry is DebugPerformanceStack stack)
                {
                    if (!string.IsNullOrEmpty(stack.Name))
                        stack.Node.Log(buffer, this.MaxLength);
                    stack.Log(i + 1);
                }
                else if (entry is NodeAverage node)
                {
                    node.Log(buffer, this.MaxLength);
                }
            }

            // If we're not recursing add a blank line at the end
            if (this.Parent is null)
                Console.WriteLine();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Node.Add(DateTimeOffset.UtcNow - this.StartTime);
            this.Stopwatch.Stop();
            this.Parent?.Write(this);
        }

        /// <summary>
        /// This object is used for calculating the Millisecond Average that a <see cref="NodeAverage.Name"/>d action takes
        /// </summary>
        private sealed class NodeAverage {
            public readonly string Name;

            public TimeSpan Span { get; private set; }
            public int Counter { get; private set; }

            public NodeAverage(string name)
            {
                this.Name = name;
            }

            public void Add(TimeSpan span) {
                this.Span += span;
                this.Counter++;
            }

            public void Merge(NodeAverage other)
            {
                // Set the sum
                this.Span += other.Span;

                // Combine the counters
                this.Counter += other.Counter;
            }

            public void Log(int left = 0, int right = 0)
            {
                Console.WriteLine($"{new string(' ', left)} - {this.Name.PadRight(right)} x{this.Counter:n0} | Took {this.Span.TotalMilliseconds:n0}ms (at ~{TimeUnitExtensions.Format(this.Span.TotalMicroseconds / this.Counter)} per)");
            }
        }
    }
}
