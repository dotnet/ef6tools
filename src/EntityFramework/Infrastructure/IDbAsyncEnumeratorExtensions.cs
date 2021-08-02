// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.


#if !NET40

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Entity.Utilities;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class IDbAsyncEnumeratorExtensions
    {
        // <summary>
        // Advances the enumerator to the next element in the sequence, returning the result asynchronously.
        // </summary>
        // <returns> A Task containing the result of the operation: true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the sequence. </returns>
        public static Task<bool> MoveNextAsync(this IDbAsyncEnumerator enumerator)
        {
            Check.NotNull(enumerator, "enumerator");

            return enumerator.MoveNextAsync(CancellationToken.None);
        }

        internal static IDbAsyncEnumerator<TResult> Cast<TResult>(this IDbAsyncEnumerator source)
        {
            DebugCheck.NotNull(source);

            return new CastDbAsyncEnumerator<TResult>(source);
        }

        private class CastDbAsyncEnumerator<TResult> : IDbAsyncEnumerator<TResult>
        {
            private readonly IDbAsyncEnumerator _underlyingEnumerator;

            public CastDbAsyncEnumerator(IDbAsyncEnumerator sourceEnumerator)
            {
                DebugCheck.NotNull(sourceEnumerator);

                _underlyingEnumerator = sourceEnumerator;
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                return _underlyingEnumerator.MoveNextAsync(cancellationToken);
            }

            public TResult Current
            {
                get { return (TResult)_underlyingEnumerator.Current; }
            }

            object IDbAsyncEnumerator.Current
            {
                get { return _underlyingEnumerator.Current; }
            }

            public void Dispose()
            {
                _underlyingEnumerator.Dispose();
            }
        }
    }
}

#endif
