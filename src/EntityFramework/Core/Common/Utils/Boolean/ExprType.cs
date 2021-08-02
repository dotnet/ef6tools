// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.Utils.Boolean
{
    // <summary>
    // Enumeration of Boolean expression node types.
    // </summary>
    internal enum ExprType
    {
        And,
        Not,
        Or,
        Term,
        True,
        False,
    }
}
