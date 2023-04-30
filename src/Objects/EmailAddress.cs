using System.Text.RegularExpressions;

namespace MyAddressExtractor.Objects {
    public struct EmailAddress
    {
        private readonly Match Match;

        /// <summary>
        /// <para>The full email address.</para>
        /// 
        /// This value can be set by <see cref="AddressFilter.BaseFilter"/>s, to trim or otherwise make tweaks to the address.
        /// <b>Be careful changing this</b>, filters are not rerun. The new value may not have passed previous filters.
        /// </summary>
        /// <example>username@test.com</example>
        public string Full {
            get => this._Full ??= this.Match.Value;
            set {
                // Update the full address to something else
                this._Full = value ?? throw new NullReferenceException();

                // Clear the cache
                this._Separator = null;
                this._Username = null;
                this._Domain = null;
            }
        }
        private string? _Full = null;

        /// <summary>The username part of the address.</summary>
        /// <example>username</example>
        public string Username => this._Username ??= this.Full[..this.Separator];
        private string? _Username = null;

        /// <summary>The domain part of the address.</summary>
        /// <example>test.com</example>
        public string Domain => this._Domain ??= this.Full[this.Separator..];
        private string? _Domain = null;

        /// <summary>Cache where the '@' separate is for 'Username' and 'Domain' so we don't keep calling 'IndexOf()'</summary>
        private int Separator => this._Separator ??= this.Full.LastIndexOf('@');
        private int? _Separator = null;

        /// <summary>The Length of the Full Address</summary>
        public int Length => this.Full.Length;

        public EmailAddress(Match match)
        {
            this.Match = match;
        }
    }
}
