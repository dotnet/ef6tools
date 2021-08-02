// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Conventions
{
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.ModelConfiguration.Configuration;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Utilities;
    using System.Reflection;

    /// <summary>
    /// Base class for conventions that process CLR attributes found on properties of types in the model.
    /// </summary>
    /// <remarks>
    /// Note that the derived convention will be applied for any non-static property on the mapped type that has
    /// the specified attribute, even if it wasn't included in the model.
    /// </remarks>
    /// <typeparam name="TAttribute"> The type of the attribute to look for. </typeparam>
    public abstract class PropertyAttributeConfigurationConvention<TAttribute>
        : Convention
        where TAttribute : Attribute
    {
        private readonly AttributeProvider _attributeProvider = DbConfiguration.DependencyResolver.GetService<AttributeProvider>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyAttributeConfigurationConvention{TAttribute}"/> class.
        /// </summary>
        protected PropertyAttributeConfigurationConvention()
        {
            Types().Configure(
                ec =>
                    {
                        // PERF: this code is part of a critical section, consider its performance when refactoring
                        foreach (var propertyInfo in ec.ClrType.GetInstanceProperties())
                        {
                            var attributes = (IList<Attribute>)_attributeProvider.GetAttributes(propertyInfo);
                            // ReSharper disable once ForCanBeConvertedToForeach
                            for(var i = 0; i < attributes.Count; ++i)
                            {
                                var attribute = attributes[i] as TAttribute;
                                if (attribute != null)
                                {
                                    Apply(propertyInfo, ec, attribute);
                                }
                            }
                        }
                    });
        }

        /// <summary>
        /// Applies this convention to a property that has an attribute of type TAttribute applied.
        /// </summary>
        /// <param name="memberInfo">The member info for the property that has the attribute.</param>
        /// <param name="configuration">The configuration for the class that contains the property.</param>
        /// <param name="attribute">The attribute.</param>
        public abstract void Apply(PropertyInfo memberInfo, ConventionTypeConfiguration configuration, TAttribute attribute);
    }
}
