﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure;

    internal interface IDbEnumerator<out T> : IEnumerator<T>
#if !NET40
                                              , IDbAsyncEnumerator<T>
#endif
    {
        new T Current { get; }
    }
}
