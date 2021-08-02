// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal.Linq
{
    using System.Data.Entity.Infrastructure;

    // <summary>
    // An internal interface implemented by <see cref="DbQuery{TResult}" /> and <see cref="DbQuery" /> that allows access to
    // the internal query without using reflection.
    // </summary>
    internal interface IInternalQueryAdapter
    {
        #region Underlying internal set

        // <summary>
        // The underlying internal set.
        // </summary>
        IInternalQuery InternalQuery { get; }

        #endregion
    }
}
