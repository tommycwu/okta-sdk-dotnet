﻿// <copyright file="CollectionAsyncEnumerator{T}.cs" company="Okta, Inc">
// Copyright (c) 2014 - present Okta, Inc. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Okta.Sdk.Internal
{
    /// <summary>
    /// Enumerates an Okta API collection for use with LINQ.
    /// This is an internal class; use <see cref="PagedCollectionEnumerator{T}"/> if you need to page collections yourself.
    /// </summary>
    /// <remarks>See <a href="https://developer.okta.com/docs/api/getting_started/design_principles.html#pagination">the API documentation on pagination</a>.</remarks>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public sealed class CollectionAsyncEnumerator<T> : IAsyncEnumerator<T>
        where T : IResource
    {
        private readonly PagedCollectionEnumerator<T> _pagedEnumerator;

        private bool _initialized = false;
        private int _localPageIndex;

        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionAsyncEnumerator{T}"/> class.
        /// </summary>
        /// <param name="dataStore">The <see cref="IDataStore">DataStore</see> to use.</param>
        /// <param name="initialRequest">The initial HTTP request options.</param>
        /// <param name="requestContext">The request context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public CollectionAsyncEnumerator(
            IDataStore dataStore,
            HttpRequest initialRequest,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _pagedEnumerator = new PagedCollectionEnumerator<T>(dataStore, initialRequest, requestContext, cancellationToken);
        }

        /// <inheritdoc/>
        public T Current => _pagedEnumerator.CurrentPage.Items.ElementAt(_localPageIndex);

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose DataStore?
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public async ValueTask<bool> MoveNextAsync()
        {
            var hasMoreLocalItems = _initialized
                && _pagedEnumerator.CurrentPage.Items.Any()
                && (_localPageIndex + 1) < _pagedEnumerator.CurrentPage.Items.Count();

            if (hasMoreLocalItems)
            {
                _localPageIndex++;
                return true;
            }

            var movedNext = await _pagedEnumerator.MoveNextAsync().ConfigureAwait(false);
            if (!movedNext)
            {
                return false;
            }

            _initialized = true;
            _localPageIndex = 0;

            return _pagedEnumerator.CurrentPage.Items.Any();
        }

        /// <inheritdoc/>
#pragma warning disable AvoidAsyncSuffix // Avoid Async suffix
        public ValueTask DisposeAsync()
#pragma warning restore AvoidAsyncSuffix // Avoid Async suffix
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            return default(ValueTask);
        }
    }
}
