// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    // <summary>
    // Represents the various kind of member mapping
    // </summary>
    internal enum MemberMappingKind
    {
        ScalarPropertyMapping = 0,

        NavigationPropertyMapping = 1,

        AssociationEndMapping = 2,

        ComplexPropertyMapping = 3,
    }
}
