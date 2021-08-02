// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Mapping
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;

    internal class PropertyMappingSpecification
    {
        private readonly EntityType _entityType;
        private readonly IList<EdmProperty> _propertyPath;
        private readonly IList<ConditionPropertyMapping> _conditions;
        private readonly bool _isDefaultDiscriminatorCondition;

        public PropertyMappingSpecification(
            EntityType entityType,
            IList<EdmProperty> propertyPath,
            IList<ConditionPropertyMapping> conditions,
            bool isDefaultDiscriminatorCondition)
        {
            DebugCheck.NotNull(entityType);

            _entityType = entityType;
            _propertyPath = propertyPath;
            _conditions = conditions;
            _isDefaultDiscriminatorCondition = isDefaultDiscriminatorCondition;
        }

        public EntityType EntityType
        {
            get { return _entityType; }
        }

        public IList<EdmProperty> PropertyPath
        {
            get { return _propertyPath; }
        }

        public IList<ConditionPropertyMapping> Conditions
        {
            get { return _conditions; }
        }

        public bool IsDefaultDiscriminatorCondition
        {
            get { return _isDefaultDiscriminatorCondition; }
        }
    }
}
