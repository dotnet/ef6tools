// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.Data.Entity.Core.EntityClient;

    internal interface ICancelableEntityConnectionInterceptor : IDbInterceptor
    {
        bool ConnectionOpening(EntityConnection connection, DbInterceptionContext interceptionContext);
    }
}
