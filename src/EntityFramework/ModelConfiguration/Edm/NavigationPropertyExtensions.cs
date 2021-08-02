// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;

    internal static class NavigationPropertyExtensions
    {
        public static object GetConfiguration(this NavigationProperty navigationProperty)
        {
            DebugCheck.NotNull(navigationProperty);

            return navigationProperty.Annotations.GetConfiguration();
        }

        public static void SetConfiguration(this NavigationProperty navigationProperty, object configuration)
        {
            DebugCheck.NotNull(navigationProperty);

            navigationProperty.GetMetadataProperties().SetConfiguration(configuration);
        }

        public static AssociationEndMember GetFromEnd(this NavigationProperty navProp)
        {
            DebugCheck.NotNull(navProp.Association);

            return navProp.Association.SourceEnd == navProp.ResultEnd
                       ? navProp.Association.TargetEnd
                       : navProp.Association.SourceEnd;
        }
    }
}
