// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    internal enum DatabaseExistenceState
    {
        Unknown,
        DoesNotExist,
        ExistsConsideredEmpty,
        Exists
    }
}
