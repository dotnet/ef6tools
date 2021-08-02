// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Mapping
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("{Table.Name}")]
    internal class TableMapping
    {
        private readonly EntityType _table;
        private readonly SortedEntityTypeIndex _entityTypes;
        private readonly List<ColumnMapping> _columns;

        public TableMapping(EntityType table)
        {
            DebugCheck.NotNull(table);

            _table = table;
            _entityTypes = new SortedEntityTypeIndex();
            _columns = new List<ColumnMapping>();
        }

        public EntityType Table
        {
            get { return _table; }
        }

        public SortedEntityTypeIndex EntityTypes
        {
            get { return _entityTypes; }
        }

        public IEnumerable<ColumnMapping> ColumnMappings
        {
            get { return _columns; }
        }

        public void AddEntityTypeMappingFragment(
            EntitySet entitySet, EntityType entityType, MappingFragment fragment)
        {
            Debug.Assert(fragment.Table == Table);

            _entityTypes.Add(entitySet, entityType);

            var defaultDiscriminatorColumn = fragment.GetDefaultDiscriminator();

            foreach (var cm in fragment.ColumnMappings)
            {
                var columnMapping = FindOrCreateColumnMapping(cm.ColumnProperty);
                columnMapping.AddMapping(
                    entityType,
                    cm.PropertyPath,
                    fragment.ColumnConditions.Where(cc => cc.Column == cm.ColumnProperty),
                    defaultDiscriminatorColumn == cm.ColumnProperty);
            }

            // Add any column conditions that aren't mapped to properties
            foreach (
                var cc in
                    fragment.ColumnConditions.Where(cc => fragment.ColumnMappings.All(pm => pm.ColumnProperty != cc.Column)))
            {
                var columnMapping = FindOrCreateColumnMapping(cc.Column);
                columnMapping.AddMapping(entityType, null, new[] { cc }, defaultDiscriminatorColumn == cc.Column);
            }
        }

        private ColumnMapping FindOrCreateColumnMapping(EdmProperty column)
        {
            var columnMapping = _columns.SingleOrDefault(c => c.Column == column);
            if (columnMapping == null)
            {
                columnMapping = new ColumnMapping(column);
                _columns.Add(columnMapping);
            }

            return columnMapping;
        }
    }
}
