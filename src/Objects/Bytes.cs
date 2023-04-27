namespace MyAddressExtractor.Objects {
    public enum Bytes
    {
        BYTE = 0,
        KB = 1,
        MB = 2,
        GB = 3,
        TB = 4,
        PB = 5,
        EB = 6,
        ZB = 7,
        YB = 8
    }
    
    public static class ByteExtensions
    {
        public static long Convert(this Bytes to, long fromAmount, Bytes from = Bytes.BYTE)
        {
            if ( to == from )
            {
                return fromAmount;
            }
            
            int diff = Math.Abs(to - from);
            double multiplier = Math.Pow(1000, diff);
            
            long result;
            if ( to < from )
                result = (long)(fromAmount * multiplier);
            else
                result = (long)(fromAmount / multiplier);
            
            return result;
        }
        
        public static string Format(this Bytes bytes) => bytes switch {
            Bytes.BYTE => "Bytes",
            Bytes.KB => "Kb",
            Bytes.MB => "Mb",
            Bytes.GB => "Gb",
            Bytes.TB => "Tb",
            Bytes.PB => "Pb",
            Bytes.EB => "Eb",
            Bytes.ZB => "Zb",
            Bytes.YB => "Yb",
            _ => throw new ArgumentOutOfRangeException(nameof(bytes), bytes, null)
        };
        
        public static string Format(long fromAmount, Bytes from = Bytes.BYTE)
        {
            var size = from;
            var result = fromAmount;
            
            foreach (Bytes bytes in Enum.GetValues<Bytes>().OrderDescending()) {
                if (bytes <= from)
                    continue;
                long conversion = bytes.Convert(fromAmount, from);
                
                if (conversion > 0) {
                    size = bytes;
                    result = conversion;
                    break;
                }
            }
            
            return $"{result:n0} {size.Format()}";
        }
    }
}
