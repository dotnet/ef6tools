// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.Utilities
{
    using System.Data.Entity.Core.Metadata.Edm;

    internal static class PrimitiveTypeExtensions
    {
        internal static bool IsSpatialType(this PrimitiveType type)
        {
            DebugCheck.NotNull(type);

            var kind = type.PrimitiveTypeKind;

            return kind >= PrimitiveTypeKind.Geometry && kind <= PrimitiveTypeKind.GeographyCollection;
        }
    }
}
