// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.Linq
{
    using System.Collections;
    using System.Data.Entity.Infrastructure;

    // <summary>
    // A non-generic interface implemented by <see cref="InternalSet{TEntity}" /> that allows operations on
    // any set object without knowing the type to which it applies.
    // </summary>
    internal interface IInternalSet : IInternalQuery
    {
        void Attach(object entity);
        void Add(object entity);
        void AddRange(IEnumerable entities);
        void RemoveRange(IEnumerable entities);
        void Remove(object entity);
        void Initialize();
        void TryInitialize();
        IEnumerator ExecuteSqlQuery(string sql, bool asNoTracking, bool? streaming, object[] parameters);

#if !NET40

        IDbAsyncEnumerator ExecuteSqlQueryAsync(string sql, bool asNoTracking, bool? streaming, object[] parameters);

#endif
    }
}
