// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    // <summary>
    // The types of member entries supported.
    // </summary>
    internal enum MemberEntryType
    {
        ReferenceNavigationProperty,
        CollectionNavigationProperty,
        ScalarProperty,
        ComplexProperty,
    }
}
