using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Threading;

namespace HaveIBeenPwned.AddressExtractor.Objects.Filters {
    public sealed class TldFilter : AddressFilter.BaseFilter {
        private const string IANA = "https://data.iana.org/TLD/tlds-alpha-by-domain.txt";
        private const string PATH = "tld.json";

        private readonly IDisposable EmptySetNotice = Output.SingleNotice("Failed to read a set of TLDs from provider IANA, TLD filtering disabled during this run");

        public override string Name => "TLD Filter";

        private readonly Lazy<JoinableTask<ISet<string>>> List;

        public TldFilter()
        {
            // Wrapped in a Lazy because 'Runtime' is an injected property set after the constructor
            this.List = new Lazy<JoinableTask<ISet<string>>>(() => this.Runtime!.ExecuteAsync(() => this.FetchAsync(CancellationToken.None)));
        }

        /// <inheritdoc />
        public override ValueTask<Result> ValidateEmailAddressAsync(ref EmailAddress address, CancellationToken cancellation = default)
            => this.ValidateDomainAsync(address.Domain);

        private async ValueTask<Result> ValidateDomainAsync(string domain)
        {
            var list = await this.List.Value;
            var tld = domain[(domain.LastIndexOf('.') + 1)..];

            if (list.Count is 0) {
                this.EmptySetNotice.Dispose();
                return Result.CONTINUE;
            }

            return this.Continue(list.Contains(tld));
        }

        private async Task<ISet<string>> FetchAsync(CancellationToken cancellation)
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var data = await this.ReadFileAsync(cancellation);
            var save = false;

            var now = DateTimeOffset.UtcNow;

            if (
                // If nothing was read
                data is null
                // If the TLD list is empty
                || data.List.Count is 0
                // Cache for 24 hours
                || (now - data.LastFetch).TotalHours > 24
            ) {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, TldFilter.IANA))
                    {
                        // If we have cached data check for a 304 (Not modified)
                        if (data is not null) {
                            request.Headers.IfModifiedSince = data.LastFetch;
                            if (data.ETag is not "") // Ignore the default cached etag
                                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(data.ETag));
                        }

                        using (var response = await client.SendAsync(request, cancellation))
                        {
                            if (response.StatusCode is HttpStatusCode.OK) {
                                save = true;
                                using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellation)))
                                {
                                    while (
                                        !reader.EndOfStream
                                        && await reader.ReadLineAsync(cancellation) is string line
                                    ) {
                                        // Ignore comments (eg; LastModified line)
                                        if (line.StartsWith('#'))
                                            continue;

                                        // Add the line to the list of TLDs
                                        list.Add(line);
                                    }

                                    data = new DataSet {
                                        List = list,
                                        LastFetch = now,
                                        ETag = response.Headers.ETag?.Tag ?? string.Empty
                                    };
                                }
                            }
                        }
                    }
                }
            } else {
                // Combine 
                list.UnionWith(data.List);
            }

            // Cache the information
            if (data is not null && save)
                await this.WriteFileAsync(data, cancellation);

            return list;
        }

        private async ValueTask<DataSet?> ReadFileAsync(CancellationToken cancellation)
        {
            // Check that our cached JSON file exists
            if (!File.Exists(TldFilter.PATH))
                return null;

            try {
                await using (var filestream = new FileStream(TldFilter.PATH, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                {
                    return await JsonSerializer.DeserializeAsync<DataSet>(filestream, this.Runtime.Json, cancellation);
                }
            } catch (IOException e) {
                // Reading the cache isn't a huge deal, notify of failure if debugging
                if (this.Config.Debug)
                    Output.Exception(new Exception("Failed to read the cached Tld file from disk", e));
                
            } catch (JsonException e) {
                // Reading the cache isn't a huge deal, notify of failure if debugging
                if (this.Config.Debug)
                    Output.Exception(new Exception("Failed to parse the cached Tld file on disk", e));
            }

            // Return null that we failed to read the file due to an exception
            return null;
        }

        private async ValueTask WriteFileAsync(DataSet set, CancellationToken cancellation)
        {
            try {
                await using (var filestream = new FileStream(TldFilter.PATH, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(filestream, set, this.Runtime.Json, cancellation);
                }
            } catch (IOException e) {
                if (this.Config.Debug)
                    Output.Exception(new Exception("Failed to cache IANA-Tlds", e));
                else
                    Output.Error($"Failed to cache IANA-Tlds to disk: {e.Message}");
                
            } catch (JsonException e) {
                if (this.Config.Debug)
                    Output.Exception(new Exception("Failed to cache IANA-Tlds", e));
                else
                    Output.Error($"Failed to cache IANA-Tlds due to a Json Exception: {e.Message}");
                
            }
        }

        [Serializable]
        private class DataSet {
            [JsonPropertyName("e-tag")]
            public string ETag { get; set; } = string.Empty;

            [JsonPropertyName("updated")]
            public DateTimeOffset LastFetch { get; set; } = DateTimeOffset.UtcNow;

            [JsonPropertyName("tld")]
            public ICollection<string> List { get; set; } = Array.Empty<string>();
        }
    }
}
