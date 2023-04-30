using System.Text.RegularExpressions;

namespace MyAddressExtractor.Objects {
    public struct EmailAddress {
        private readonly Match Match;

        public string Full => this._Full ??= this.Match.Value;
        private string? _Full = null;

        public string Username => this._Username ??= this.Full[..this.Separator];
        private string? _Username = null;

        public string Domain => this._Domain ??= this.Full[this.Separator..];
        private string? _Domain = null;

        /// <summary>Cache where the '@' separate is for 'Username' and 'Domain' so we don't keep calling 'IndexOf()'</summary>
        private int Separator => this._Separator ??= this.Full.LastIndexOf('@');
        private int? _Separator = null;

        public int Length => this.Match.Length - this.Match.Index;

        public EmailAddress(Match match)
        {
            this.Match = match;
        }
    }
}
