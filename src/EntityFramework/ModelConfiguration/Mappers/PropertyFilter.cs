﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Mappers
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using System.Reflection;

    internal sealed class PropertyFilter
    {
        private readonly DbModelBuilderVersion _modelBuilderVersion;

        public PropertyFilter(DbModelBuilderVersion modelBuilderVersion = DbModelBuilderVersion.Latest)
        {
            _modelBuilderVersion = modelBuilderVersion;
        }

        public IEnumerable<PropertyInfo> GetProperties(
            Type type,
            bool declaredOnly,
            IEnumerable<PropertyInfo> explicitlyMappedProperties = null,
            IEnumerable<Type> knownTypes = null,
            bool includePrivate = false)
        {
            DebugCheck.NotNull(type);

            explicitlyMappedProperties = explicitlyMappedProperties ?? Enumerable.Empty<PropertyInfo>();
            knownTypes = knownTypes ?? Enumerable.Empty<Type>();

            ValidatePropertiesForModelVersion(type, explicitlyMappedProperties);

            var propertyInfos
                = from p in declaredOnly ? type.GetDeclaredProperties() : type.GetNonHiddenProperties()
                  where !p.IsStatic() && p.IsValidStructuralProperty()
                  let m = p.Getter()
                  where (includePrivate || (m.IsPublic || explicitlyMappedProperties.Contains(p) || knownTypes.Contains(p.PropertyType)))
                        && (!declaredOnly || type.BaseType().GetInstanceProperties().All(bp => bp.Name != p.Name))
                        && (EdmV3FeaturesSupported || (!IsEnumType(p.PropertyType) && !IsSpatialType(p.PropertyType)))
                        && (Ef6FeaturesSupported || !p.PropertyType.IsNested)
                  select p;

            return propertyInfos;
        }

        public void ValidatePropertiesForModelVersion(Type type, IEnumerable<PropertyInfo> explicitlyMappedProperties)
        {
            if (_modelBuilderVersion == DbModelBuilderVersion.Latest)
            {
                return;
            }

            if (!EdmV3FeaturesSupported)
            {
                var firstBadProperty =
                    explicitlyMappedProperties.FirstOrDefault(
                        p => IsEnumType(p.PropertyType) || IsSpatialType(p.PropertyType));
                if (firstBadProperty != null)
                {
                    throw Error.UnsupportedUseOfV3Type(type.Name, firstBadProperty.Name);
                }
            }
        }

        public bool EdmV3FeaturesSupported
        {
            get { return _modelBuilderVersion.GetEdmVersion() >= XmlConstants.EdmVersionForV3; }
        }

        public bool Ef6FeaturesSupported
        {
            get
            {
                return _modelBuilderVersion == DbModelBuilderVersion.Latest
                       || _modelBuilderVersion >= DbModelBuilderVersion.V6_0;
            }
        }

        private static bool IsEnumType(Type type)
        {
            type.TryUnwrapNullableType(out type);

            return type.IsEnum();
        }

        private static bool IsSpatialType(Type type)
        {
            type.TryUnwrapNullableType(out type);

            return type == typeof(DbGeometry) || type == typeof(DbGeography);
        }
    }
}
