// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Data.Entity.Design.CodeGeneration.Extensions;

    internal class ForeignKeyDiscoverer : INavigationPropertyConfigurationDiscoverer
    {
        public IFluentConfiguration Discover(NavigationProperty navigationProperty, DbModel model)
        {
            Debug.Assert(navigationProperty != null, "navigationProperty is null.");
            Debug.Assert(model != null, "model is null.");

            var associationType = (AssociationType)navigationProperty.RelationshipType;

            if (!associationType.IsForeignKey
                || (navigationProperty.ToEndMember.RelationshipMultiplicity != RelationshipMultiplicity.Many
                    && navigationProperty.FromEndMember.RelationshipMultiplicity != RelationshipMultiplicity.Many))
            {
                // Doesn't apply
                return null;
            }

            var constraint = associationType.Constraint;
            var entityType = (EntityType)navigationProperty.DeclaringType;
            var fromProperty = constraint.FromProperties.First();
            var fromPropertyName = fromProperty.Name;
            var toEntityType = navigationProperty.ToEndMember.GetEntityType();
            // NOTE: OrderBy works around a bug in primary key column ordering (Work Item 868)
            var toProperties = constraint.ToProperties.OrderBy(
                p => entityType.KeyMembers.IndexOf(constraint.FromProperties[constraint.ToProperties.IndexOf(p)]));
            var toPropertyName = toProperties.First().Name;

            if (!toProperties.MoreThan(1)
                && (toPropertyName.EqualsIgnoreCase(navigationProperty.Name + fromPropertyName)
                    || (!entityType.NavigationProperties.Where(p => p.ToEndMember.GetEntityType() == toEntityType)
                            .MoreThan(1)
                        && (toPropertyName.EqualsIgnoreCase(fromPropertyName)
                            || toPropertyName.EqualsIgnoreCase(fromProperty.DeclaringType.Name + fromPropertyName)))))
            {

                // By convention
                return null;
            }

            var configuration = new ForeignKeyConfiguration();

            foreach (var property in toProperties)
            {
                configuration.Properties.Add(property);
            }

            return configuration;
        }
    }
}

