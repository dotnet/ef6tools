﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.TestHelpers
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;

    public class FakeProviderServicesResolver : IDbDependencyResolver
    {
        public object GetService(Type type, object key)
        {
            if (type == typeof(DbProviderServices))
            {
                var name = key as string;
                if (name.StartsWith("My.Generic.Provider.", StringComparison.Ordinal))
                {
                    return GenericProviderServices.Instance;
                }
            }

            return null;
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            return this.GetServiceAsServices(type, key);
        }
    }
}
