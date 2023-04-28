namespace MyAddressExtractor.Objects {
    public enum TimeUnit : long {
        MICROSECONDS = 1,
        MILLISECONDS = MICROSECONDS * 1000,
        SECONDS = MILLISECONDS * 1000,
        MINUTES = SECONDS * 60,
        HOURS = MINUTES * 60,
        DAYS = HOURS * 24
    }
    
    public static class TimeUnitExtensions
    {
        public static double Convert(this TimeUnit to, double fromAmount, TimeUnit from = TimeUnit.MILLISECONDS)
        {
            double micro = fromAmount * (long)from;
            return micro / (long)to;
        }

        public static string Format(this TimeUnit time) => time switch {
            TimeUnit.MICROSECONDS => "μs",
            TimeUnit.MILLISECONDS => "ms",
            TimeUnit.SECONDS => "s",
            TimeUnit.MINUTES => "m",
            TimeUnit.HOURS => "h",
            TimeUnit.DAYS => "d",
            _ => throw new ArgumentOutOfRangeException(nameof(time), time, null)
        };

        public static string Format(double fromAmount, TimeUnit from = TimeUnit.MICROSECONDS)
        {
            var size = from;
            var result = fromAmount;

            foreach (TimeUnit unit in Enum.GetValues<TimeUnit>().OrderDescending()) {
                if (unit <= from || fromAmount < (long)unit)
                    continue;
                var conversion = unit.Convert(fromAmount, from);

                if (conversion > 0) {
                    size = unit;
                    result = conversion;
                    break;
                }
            }

            return $"{result:n0}{size.Format()}";
        }
    }
}
