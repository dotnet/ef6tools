// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.Utilities;
    using System.Linq;

    /// <summary>
    /// Convention to discover foreign key properties whose names are a combination
    /// of the dependent navigation property name and the principal type primary key property name(s).
    /// </summary>
    public class NavigationPropertyNameForeignKeyDiscoveryConvention : ForeignKeyDiscoveryConvention
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

            var otherEnd = associationType.GetOtherEnd(dependentAssociationEnd);

            var navigationProperty
                = dependentAssociationEnd.GetEntityType().NavigationProperties
                                         .SingleOrDefault(n => n.ResultEnd == otherEnd);

            if (navigationProperty == null)
            {
                return false;
            }

            return string.Equals(
                dependentProperty.Name, navigationProperty.Name + principalKeyProperty.Name,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        protected override bool SupportsMultipleAssociations
        {
            get { return true; }
        }
    }
}
