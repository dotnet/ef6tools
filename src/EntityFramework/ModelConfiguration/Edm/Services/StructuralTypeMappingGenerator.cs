// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm.Services
{
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Linq;

    internal abstract class StructuralTypeMappingGenerator
    {
        protected readonly DbProviderManifest _providerManifest;

        protected StructuralTypeMappingGenerator(DbProviderManifest providerManifest)
        {
            DebugCheck.NotNull(providerManifest);

            _providerManifest = providerManifest;
        }

        protected EdmProperty MapTableColumn(
            EdmProperty property,
            string columnName,
            bool isInstancePropertyOnDerivedType)
        {
            DebugCheck.NotNull(property);
            DebugCheck.NotEmpty(columnName);

            var underlyingTypeUsage
                = TypeUsage.Create(property.UnderlyingPrimitiveType, property.TypeUsage.Facets);

            var storeTypeUsage = _providerManifest.GetStoreType(underlyingTypeUsage);

            var tableColumnMetadata
                = new EdmProperty(columnName, storeTypeUsage)
                    {
                        Nullable = isInstancePropertyOnDerivedType || property.Nullable
                    };

            if (tableColumnMetadata.IsPrimaryKeyColumn)
            {
                tableColumnMetadata.Nullable = false;
            }

            var storeGeneratedPattern = property.GetStoreGeneratedPattern();

            if (storeGeneratedPattern != null)
            {
                tableColumnMetadata.StoreGeneratedPattern = storeGeneratedPattern.Value;
            }

            MapPrimitivePropertyFacets(property, tableColumnMetadata, storeTypeUsage);

            return tableColumnMetadata;
        }

        internal static void MapPrimitivePropertyFacets(
            EdmProperty property, EdmProperty column, TypeUsage typeUsage)
        {
            DebugCheck.NotNull(property);
            DebugCheck.NotNull(column);
            DebugCheck.NotNull(typeUsage);

            if (IsValidFacet(typeUsage, XmlConstants.FixedLengthElement)
                && property.IsFixedLength != null)
            {
                column.IsFixedLength = property.IsFixedLength;
            }

            if (IsValidFacet(typeUsage, XmlConstants.MaxLengthElement))
            {
                column.IsMaxLength = property.IsMaxLength;

                if (!column.IsMaxLength || property.MaxLength != null)
                {
                    column.MaxLength = property.MaxLength;
                }
            }

            if (IsValidFacet(typeUsage, XmlConstants.UnicodeElement)
                && property.IsUnicode != null)
            {
                column.IsUnicode = property.IsUnicode;
            }

            if (IsValidFacet(typeUsage, XmlConstants.PrecisionElement)
                && property.Precision != null)
            {
                column.Precision = property.Precision;
            }

            if (IsValidFacet(typeUsage, XmlConstants.ScaleElement)
                && property.Scale != null)
            {
                column.Scale = property.Scale;
            }
        }

        private static bool IsValidFacet(TypeUsage typeUsage, string name)
        {
            DebugCheck.NotNull(typeUsage);
            DebugCheck.NotEmpty(name);

            Facet facet;

            return typeUsage.Facets.TryGetValue(name, false, out facet)
                   && !facet.Description.IsConstant;
        }

        protected static EntityTypeMapping GetEntityTypeMappingInHierarchy(
            DbDatabaseMapping databaseMapping, EntityType entityType)
        {
            DebugCheck.NotNull(databaseMapping);
            DebugCheck.NotNull(entityType);

            var entityTypeMapping = databaseMapping.GetEntityTypeMapping(entityType);

            if (entityTypeMapping == null)
            {
                var entitySetMapping =
                    databaseMapping.GetEntitySetMapping(databaseMapping.Model.GetEntitySet(entityType));

                if (entitySetMapping != null)
                {
                    entityTypeMapping = entitySetMapping
                        .EntityTypeMappings
                        .First(
                            etm => entityType.DeclaredProperties.All(
                                dp => etm.MappingFragments.First()
                                         .ColumnMappings.Select(pm => pm.PropertyPath.First()).Contains(dp)));
                }
            }

            Debug.Assert(entityTypeMapping != null);

            return entityTypeMapping;
        }
    }
}
