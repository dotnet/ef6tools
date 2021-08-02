// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;

    internal class ColumnMappingBuilder
    {
        private EdmProperty _columnProperty;
        private readonly IList<EdmProperty> _propertyPath;
        private ScalarPropertyMapping _scalarPropertyMapping;

        public ColumnMappingBuilder(EdmProperty columnProperty, IList<EdmProperty> propertyPath)
        {
            Check.NotNull(columnProperty, "columnProperty");
            Check.NotNull(propertyPath, "propertyPath");

            _columnProperty = columnProperty;
            _propertyPath = propertyPath;
        }

        public IList<EdmProperty> PropertyPath
        {
            get { return _propertyPath; }
        }

        public EdmProperty ColumnProperty
        {
            get { return _columnProperty; }
            internal set
            {
                DebugCheck.NotNull(value);

                _columnProperty = value;

                if (_scalarPropertyMapping != null)
                {
                    _scalarPropertyMapping.Column = _columnProperty;
                }
            }
        }

        internal void SetTarget(ScalarPropertyMapping scalarPropertyMapping)
        {
            _scalarPropertyMapping = scalarPropertyMapping;
        }
    }
}
