namespace MyAddressExtractor {
    /// <summary>
    /// A simple glorified <see cref="int"/> wrapper.
    /// Allows storing the <see cref="Count"/> inside of any <see cref="IDictionary{TKey,TValue}"/> and accesing the value by reference
    /// (Used as the int Reference is updated as a file is read through).
    /// </summary>
    public sealed class Count
    {
        public int Value => this._Value;
        private int _Value = 0;

        public void Increment()
            => Interlocked.Increment(ref this._Value);

        /// <inheritdoc />
        public override string ToString()
            => $"{this.Value:n0}";
    }
}
