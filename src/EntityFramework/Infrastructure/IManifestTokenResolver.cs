// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;

    /// <summary>
    /// A service for getting a provider manifest token given a connection.
    /// The <see cref="DefaultManifestTokenResolver" /> class is used by default and makes use of the
    /// underlying provider to get the token which often involves opening the connection.
    /// A different implementation can be used instead by adding an <see cref="IDbDependencyResolver" />
    /// to <see cref="DbConfiguration" /> that may use any information in the connection to return
    /// the token. For example, if the connection is known to point to a SQL Server 2008 database then
    /// "2008" can be returned without opening the connection.
    /// </summary>
    public interface IManifestTokenResolver
    {
        /// <summary>
        /// Returns the manifest token to use for the given connection.
        /// </summary>
        /// <param name="connection"> The connection for which a manifest token is required. </param>
        /// <returns> The manifest token to use. </returns>
        string ResolveManifestToken(DbConnection connection);
    }
}
