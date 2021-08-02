// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.DependencyResolution
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;

    internal class DatabaseInitializerResolver : IDbDependencyResolver
    {
        private readonly ConcurrentDictionary<Type, object> _initializers =
            new ConcurrentDictionary<Type, object>();

        public virtual object GetService(Type type, object key)
        {
            var contextType = type.TryGetElementType(typeof(IDatabaseInitializer<>));
            if (contextType != null)
            {
                object initializer;
                if (_initializers.TryGetValue(contextType, out initializer))
                {
                    return initializer;
                }
            }

            return null;
        }

        public virtual void SetInitializer(Type contextType, object initializer)
        {
            DebugCheck.NotNull(contextType);
            DebugCheck.NotNull(initializer);

            _initializers.AddOrUpdate(contextType, initializer, (c, i) => initializer);
        }

        public IEnumerable<object> GetServices(Type type, object key)
        {
            return this.GetServiceAsServices(type, key);
        }
    }
}
