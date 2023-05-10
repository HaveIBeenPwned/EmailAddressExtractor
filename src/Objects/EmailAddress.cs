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

                this.Modified = true;
            }
        }
        private string? _Full = null;

        /// <summary>The username part of the address.</summary>
        /// <example>username</example>
        public string Username {
            get => this._Username ??= this.Full[..this.Separator];
            set {
                this._Username = value ?? throw new NullReferenceException();
                var domain = this.Domain;

                this._Full = $"{this._Username}@{domain}";

                // Clear the cache
                this._Separator = null;
                this._Domain = null;

                this.Modified = true;
            }
        }
        private string? _Username = null;

        /// <summary>The domain part of the address.</summary>
        /// <example>test.com</example>
        public string Domain {
            get => this._Domain ??= this.Full[(this.Separator + 1)..];
            set {
                this._Domain = value ?? throw new NullReferenceException();
                var username = this.Username;

                this._Full = $"{username}@{this._Domain}";

                // Clear the cache
                this._Username = null;

                this.Modified = true;
            }
        }
        private string? _Domain = null;

        /// <summary>Cache where the '@' separate is for 'Username' and 'Domain' so we don't keep calling 'IndexOf()'</summary>
        private int Separator => this._Separator ??= this.Full.LastIndexOf('@');
        private int? _Separator = null;

        /// <summary>The Length of the Full Address</summary>
        public int Length => this._Full?.Length ?? this.Match.Length;

        /// <summary>If the <see cref="Full"/> was manually overridden</summary>
        public bool Modified { get; private set; } = false;

        public EmailAddress(Match match)
        {
            this.Match = match;
        }
    }
}
