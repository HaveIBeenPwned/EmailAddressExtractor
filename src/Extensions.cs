using System.Diagnostics;

namespace MyAddressExtractor {
    public static class Extensions {
        public static void AddAll<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, IEnumerable<TKey> keys, TVal value)
        {
            foreach (TKey key in keys)
            {
                dictionary[key] = value;
            }
        }

        public static TimeSpan GetAndReset(this Stopwatch stopwatch)
        {
            var millis = stopwatch.Elapsed;
            stopwatch.Restart();
            return millis;
        }
    }
}
