// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Specifies a structural type mapping.
    /// </summary>
    public abstract class StructuralTypeMapping : MappingItem
    {
        /// <summary>
        /// Gets a read-only collection of property mappings.
        /// </summary>
        public abstract ReadOnlyCollection<PropertyMapping> PropertyMappings { get; }

        /// <summary>
        /// Gets a read-only collection of property mapping conditions.
        /// </summary>
        public abstract ReadOnlyCollection<ConditionPropertyMapping> Conditions { get; }

        /// <summary>
        /// Adds a property mapping.
        /// </summary>
        /// <param name="propertyMapping">The property mapping to be added.</param>
        public abstract void AddPropertyMapping(PropertyMapping propertyMapping);

        /// <summary>
        /// Removes a property mapping.
        /// </summary>
        /// <param name="propertyMapping">The property mapping to be removed.</param>
        public abstract void RemovePropertyMapping(PropertyMapping propertyMapping);

        /// <summary>
        /// Adds a property mapping condition.
        /// </summary>
        /// <param name="condition">The property mapping condition to be added.</param>
        public abstract void AddCondition(ConditionPropertyMapping condition);

        /// <summary>
        /// Removes a property mapping condition.
        /// </summary>
        /// <param name="condition">The property mapping condition to be removed.</param>
        public abstract void RemoveCondition(ConditionPropertyMapping condition);
    }
}
