using Azure.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{
    public class Page<T>
        where T : Model, new()
    {
        private T[] _items;
        private Uri _nextLink;
        private Func<Uri, CancellationToken, Task<Response<Page<T>>>> _nextPageCallback;

        public Page(Func<Uri, CancellationToken, Task<Response<Page<T>>>> nextPageCallback = null, CancellationToken cancellation = default)
        {
            _nextPageCallback = nextPageCallback;
        }

        public ReadOnlySpan<T> Items { get => _items.AsSpan(); }

        public Uri NextLink { get => _nextLink; }

        internal void Deserialize(Stream content)
        {
            using (JsonDocument json = JsonDocument.Parse(content, default))
            {
                if (json.RootElement.TryGetProperty("value", out JsonElement value))
                {
                    _items = new T[value.GetArrayLength()];

                    int i = 0;

                    foreach (var elem in value.EnumerateArray())
                    {
                        _items[i] = new T();

                        _items[i].ReadProperties(elem);

                        i++;
                    }
                }

                if (json.RootElement.TryGetProperty("nextLink", out JsonElement nextLink))
                {
                    var nextLinkUrl = nextLink.GetString();

                    if (!string.IsNullOrEmpty(nextLinkUrl))
                    {
                        _nextLink = new Uri(nextLinkUrl);
                    }
                }
            }
        }
    }
}
