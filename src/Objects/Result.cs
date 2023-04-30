namespace MyAddressExtractor.Objects {
    public enum Result {
        /// <summary>Your <see cref="AddressFilter.BaseFilter"/> is certain the the <see cref="EmailAddress"/> is VALID</summary>
        ALLOW,

        /// <summary>Your <see cref="AddressFilter.BaseFilter"/> is uncertain of the validity of the address</summary>
        CONTINUE,

        /// <summary>Your <see cref="AddressFilter.BaseFilter"/> is certain that the <see cref="EmailAddress"/> is NOT VALID</summary>
        DENY
    }
}
