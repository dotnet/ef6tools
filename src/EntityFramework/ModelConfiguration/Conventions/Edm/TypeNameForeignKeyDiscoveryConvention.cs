// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;

    /// <summary>
    /// Convention to discover foreign key properties whose names are a combination
    /// of the principal type name and the principal type primary key property name(s).
    /// </summary>
    public class TypeNameForeignKeyDiscoveryConvention : ForeignKeyDiscoveryConvention
    {
        /// <inheritdoc/>
        protected override bool MatchDependentKeyProperty(
            AssociationType associationType,
            AssociationEndMember dependentAssociationEnd,
            EdmProperty dependentProperty,
            EntityType principalEntityType,
            EdmProperty principalKeyProperty)
        {
            Check.NotNull(associationType, "associationType");
            Check.NotNull(dependentAssociationEnd, "dependentAssociationEnd");
            Check.NotNull(dependentProperty, "dependentProperty");
            Check.NotNull(principalEntityType, "principalEntityType");
            Check.NotNull(principalKeyProperty, "principalKeyProperty");

            return string.Equals(
                dependentProperty.Name, principalEntityType.Name + principalKeyProperty.Name,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
