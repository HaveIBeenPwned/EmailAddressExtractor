namespace MyAddressExtractor.Objects
{
    /// <summary>
    /// A line read from a file
    /// </summary>
    public struct Line
    {
        /// <summary>The file the line was read from</summary>
        public string File { get; init; }

        /// <summary>The value of the Line</summary>
        public string Value { get; init; }

        /// <summary>The reader count</summary>
        public Count Counter { get; init; }
    }
}
