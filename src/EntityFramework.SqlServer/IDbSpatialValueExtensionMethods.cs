// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer
{
    using System.Data.Entity.Spatial;
    using System.Data.Entity.SqlServer.Utilities;

    internal static class IDbSpatialValueExtensionMethods
    {
        // <summary>
        // Returns an instance of <see cref="IDbSpatialValue" /> that wraps the specified <see cref="DbGeography" /> value.
        // IDbSpatialValue members are guaranteed not to throw the <see cref="NotImplementedException" />s caused by unimplemented members of their wrapped values.
        // </summary>
        // <param name="geographyValue"> The geography instance to wrap </param>
        // <returns>
        // An instance of <see cref="IDbSpatialValue" /> that wraps the specified geography value
        // </returns>
        internal static IDbSpatialValue AsSpatialValue(this DbGeography geographyValue)
        {
            DebugCheck.NotNull(geographyValue);

            return new DbGeographyAdapter(geographyValue);
        }

        // <summary>
        // Returns an instance of <see cref="IDbSpatialValue" /> that wraps the specified <see cref="DbGeometry" /> value.
        // IDbSpatialValue members are guaranteed not to throw the <see cref="NotImplementedException" />s caused by unimplemented members of their wrapped values.
        // </summary>
        // <param name="geometryValue"> The geometry instance to wrap </param>
        // <returns>
        // An instance of <see cref="IDbSpatialValue" /> that wraps the specified geometry value
        // </returns>
        internal static IDbSpatialValue AsSpatialValue(this DbGeometry geometryValue)
        {
            DebugCheck.NotNull(geometryValue);

            return new DbGeometryAdapter(geometryValue);
        }
    }
}
