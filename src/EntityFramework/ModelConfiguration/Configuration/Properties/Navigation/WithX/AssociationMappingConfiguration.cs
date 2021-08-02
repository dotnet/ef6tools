// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Reflection;

    /// <summary>
    /// Base class for performing configuration of a relationship.
    /// This configuration functionality is available via the Code First Fluent API, see <see cref="DbModelBuilder" />.
    /// </summary>
    public abstract class AssociationMappingConfiguration
    {
        internal abstract void Configure(
            AssociationSetMapping associationSetMapping,
            EdmModel database,
            PropertyInfo navigationProperty);

        internal abstract AssociationMappingConfiguration Clone();
    }
}
