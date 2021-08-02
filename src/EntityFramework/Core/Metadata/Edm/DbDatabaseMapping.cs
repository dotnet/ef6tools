// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Utilities;

    // TODO: METADATA: Rename or remove?
    internal class DbDatabaseMapping
    {
        private readonly List<EntityContainerMapping> _entityContainerMappings
            = new List<EntityContainerMapping>();

        public EdmModel Model { get; set; }
        public EdmModel Database { get; set; }

        public DbProviderInfo ProviderInfo
        {
            get { return Database.ProviderInfo; }
        }

        public DbProviderManifest ProviderManifest
        {
            get { return Database.ProviderManifest; }
        }

        internal IList<EntityContainerMapping> EntityContainerMappings
        {
            get { return _entityContainerMappings; }
        }

        internal void AddEntityContainerMapping(EntityContainerMapping entityContainerMapping)
        {
            Check.NotNull(entityContainerMapping, "entityContainerMapping");

            _entityContainerMappings.Add(entityContainerMapping);
        }
    }
}
