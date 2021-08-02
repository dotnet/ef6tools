// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    internal static class StorageAssociationSetMappingExtensions
    {
        public static AssociationSetMapping Initialize(this AssociationSetMapping associationSetMapping)
        {
            DebugCheck.NotNull(associationSetMapping);

            associationSetMapping.SourceEndMapping = new EndPropertyMapping();
            associationSetMapping.TargetEndMapping = new EndPropertyMapping();

            return associationSetMapping;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static object GetConfiguration(this AssociationSetMapping associationSetMapping)
        {
            DebugCheck.NotNull(associationSetMapping);

            return associationSetMapping.Annotations.GetConfiguration();
        }

        public static void SetConfiguration(this AssociationSetMapping associationSetMapping, object configuration)
        {
            DebugCheck.NotNull(associationSetMapping);

            associationSetMapping.Annotations.SetConfiguration(configuration);
        }
    }
}
