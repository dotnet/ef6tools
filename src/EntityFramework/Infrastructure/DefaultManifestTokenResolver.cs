// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Collections.Concurrent;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Utilities;

    /// <summary>
    /// A default implementation of <see cref="IManifestTokenResolver" /> that uses the
    /// underlying provider to get the manifest token.
    /// Note that to avoid multiple queries, this implementation using caching based on the actual type of
    /// <see cref="DbConnection" /> instance, the <see cref="DbConnection.DataSource" /> property,
    /// and the <see cref="DbConnection.Database" /> property.
    /// </summary>
    public class DefaultManifestTokenResolver : IManifestTokenResolver
    {
        private readonly ConcurrentDictionary<Tuple<Type, string, string>, string> _cachedTokens
            = new ConcurrentDictionary<Tuple<Type, string, string>, string>();

        /// <inheritdoc />
        public string ResolveManifestToken(DbConnection connection)
        {
            Check.NotNull(connection, "connection");

            var interceptionContext = new DbInterceptionContext();
            var key = Tuple.Create(connection.GetType(),
                DbInterception.Dispatch.Connection.GetDataSource(connection, interceptionContext),
                DbInterception.Dispatch.Connection.GetDatabase(connection, interceptionContext));

            return _cachedTokens.GetOrAdd(
                key,
                k => DbProviderServices.GetProviderServices(connection).GetProviderManifestTokenChecked(connection));
        }
    }
}
