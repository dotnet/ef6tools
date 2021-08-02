// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.Linq
{
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure;

    // <summary>
    // An interface implemented by <see cref="InternalQuery{TElement}" />.
    // </summary>
    // <typeparam name="TElement"> The type of the element. </typeparam>
    internal interface IInternalQuery<out TElement> : IInternalQuery
    {
        IInternalQuery<TElement> Include(string path);
        IInternalQuery<TElement> AsNoTracking();
        IInternalQuery<TElement> AsStreaming();
        IInternalQuery<TElement> WithExecutionStrategy(IDbExecutionStrategy executionStrategy);

#if !NET40
        new IDbAsyncEnumerator<TElement> GetAsyncEnumerator();
#endif

        new IEnumerator<TElement> GetEnumerator();
    }
}
