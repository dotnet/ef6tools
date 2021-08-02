// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm.Services
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;
    using System.Linq;

    internal class TableMappingGenerator : StructuralTypeMappingGenerator
    {
        public TableMappingGenerator(DbProviderManifest providerManifest)
            : base(providerManifest)
        {
        }

        public void Generate(EntityType entityType, DbDatabaseMapping databaseMapping)
        {
            DebugCheck.NotNull(entityType);
            DebugCheck.NotNull(databaseMapping);

            var entitySet = databaseMapping.Model.GetEntitySet(entityType);

            var entitySetMapping
                = databaseMapping.GetEntitySetMapping(entitySet)
                  ?? databaseMapping.AddEntitySetMapping(entitySet);

            var entityTypeMapping =
                entitySetMapping.EntityTypeMappings.FirstOrDefault(
                    m => m.EntityTypes.Contains(entitySet.ElementType))
                ?? entitySetMapping.EntityTypeMappings.FirstOrDefault();

            var table
                = entityTypeMapping != null
                      ? entityTypeMapping.MappingFragments.First().Table
                      : databaseMapping.Database.AddTable(entityType.GetRootType().Name);

            entityTypeMapping = new EntityTypeMapping(null);

            var entityTypeMappingFragment
                = new MappingFragment(databaseMapping.Database.GetEntitySet(table), entityTypeMapping, false);

            entityTypeMapping.AddType(entityType);
            entityTypeMapping.AddFragment(entityTypeMappingFragment);
            entityTypeMapping.SetClrType(entityType.GetClrType());

            entitySetMapping.AddTypeMapping(entityTypeMapping);

            new PropertyMappingGenerator(_providerManifest)
                .Generate(
                    entityType,
                    entityType.Properties,
                    entitySetMapping,
                    entityTypeMappingFragment,
                    new List<EdmProperty>(),
                    false);
        }
    }
}
