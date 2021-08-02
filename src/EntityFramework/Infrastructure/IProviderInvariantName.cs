// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;

    /// <summary>
    /// Used by <see cref="IDbDependencyResolver" /> and <see cref="DbConfiguration" /> when resolving
    /// a provider invariant name from a <see cref="DbProviderFactory" />.
    /// </summary>
    public interface IProviderInvariantName
    {
        /// <summary>Gets the name of the provider.</summary>
        /// <returns>The name of the provider.</returns>
        string Name { get; }
    }
}
