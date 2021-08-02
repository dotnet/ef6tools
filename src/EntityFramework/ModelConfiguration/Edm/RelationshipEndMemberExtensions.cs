// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm
{
    using System.Data.Entity.Core.Metadata.Edm;

    internal static class RelationshipEndMemberExtensions
    {
        public static bool IsMany(this RelationshipEndMember associationEnd)
        {
            return associationEnd.RelationshipMultiplicity.IsMany();
        }

        public static bool IsOptional(this RelationshipEndMember associationEnd)
        {
            return associationEnd.RelationshipMultiplicity.IsOptional();
        }

        public static bool IsRequired(this RelationshipEndMember associationEnd)
        {
            return associationEnd.RelationshipMultiplicity.IsRequired();
        }
    }
}
