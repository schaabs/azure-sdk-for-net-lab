using Azure.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault
{
    public sealed class AsyncEnumerator<T>
        where T : Model, new()
    {
        private Page<T> _currentPage = null;
        private int _currentIdx = 0;
        private T _current = default;
        private AsyncPageEnumerator<T> _pageEnumerator;

        internal AsyncEnumerator(Uri firstLink, Func<Uri, CancellationToken, Task<Response<Page<T>>>> getPageAsync = null, CancellationToken cancellation = default)
        {
            _pageEnumerator = new AsyncPageEnumerator<T>(firstLink, getPageAsync, cancellation);
        }

        public T Current => _current;

        public async ValueTask<bool> MoveNextAsync()
        {
            while (_currentPage == null || _currentIdx >= _currentPage.Items.Length)
            {
                if (!await _pageEnumerator.MoveNextAsync())
                {
                    return false;
                }

                _currentPage = _pageEnumerator.Current.Result;

                _currentIdx = 0;
            }

            _current = _currentPage.Items[_currentIdx++];

            return true;
        }

        public AsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
        {
            _pageEnumerator.Cancellation = cancellation != default(CancellationToken) ? cancellation : _pageEnumerator.Cancellation;

            return this;
        }

        public AsyncPageEnumerator<T> ByPage() => _pageEnumerator;
    }

    public sealed class AsyncPageEnumerator<T>
        where T : Model, new()
    {
        private Response<Page<T>> _current;
        private Uri _nextLink;
        private CancellationToken _cancellation;
        private Func<Uri, CancellationToken, Task<Response<Page<T>>>> _getPageAsync;

        internal AsyncPageEnumerator(Uri firstLink, Func<Uri, CancellationToken, Task<Response<Page<T>>>> getPageAsync, CancellationToken cancellation = default)
        {
            _nextLink = firstLink;
            _getPageAsync = getPageAsync;
            _cancellation = cancellation;
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_nextLink == null)
            {
                return false;
            }

            _current = await _getPageAsync(_nextLink, _cancellation);

            _nextLink = _current.Result.NextLink;

            return true;
        }

        public Response<Page<T>> Current => _current;

        public AsyncPageEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default)
        {
            _cancellation = cancellation != default(CancellationToken) ? cancellation : _cancellation;

            return this;
        }

        internal CancellationToken Cancellation { get => _cancellation; set => _cancellation = value; }
    }
}
