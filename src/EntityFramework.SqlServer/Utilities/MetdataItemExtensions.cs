// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.Utilities
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;

    internal static class MetdataItemExtensions
    {
        public static T GetMetadataPropertyValue<T>(this MetadataItem item, string propertyName)
        {
            DebugCheck.NotNull(item);

            var property = item.MetadataProperties.FirstOrDefault(p => p.Name == propertyName);
            return property == null ? default(T) : (T)property.Value;
        }
    }
}
