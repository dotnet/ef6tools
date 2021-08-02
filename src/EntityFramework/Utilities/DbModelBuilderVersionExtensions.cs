// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Utilities
{
    using System.Data.Entity.Core.Metadata.Edm;

    internal static class DbModelBuilderVersionExtensions
    {
        public static double GetEdmVersion(this DbModelBuilderVersion modelBuilderVersion)
        {
            switch (modelBuilderVersion)
            {
                case DbModelBuilderVersion.V4_1:
                case DbModelBuilderVersion.V5_0_Net4:
                    return XmlConstants.EdmVersionForV2;
                case DbModelBuilderVersion.V5_0:
                case DbModelBuilderVersion.V6_0:
                case DbModelBuilderVersion.Latest:
                    return XmlConstants.EdmVersionForV3;
                default:
                    throw new ArgumentOutOfRangeException("modelBuilderVersion");
            }
        }

        public static bool IsEF6OrHigher(this DbModelBuilderVersion modelBuilderVersion)
        {
            return modelBuilderVersion >= DbModelBuilderVersion.V6_0 
                || modelBuilderVersion == DbModelBuilderVersion.Latest;
        }
    }
}
