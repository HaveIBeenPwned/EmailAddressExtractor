namespace MyAddressExtractor {
    public static class Extensions {
        public static void AddAll<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, IEnumerable<TKey> keys, TVal value)
        {
            foreach (TKey key in keys)
            {
                dictionary[key] = value;
            }
        }
    }
}
