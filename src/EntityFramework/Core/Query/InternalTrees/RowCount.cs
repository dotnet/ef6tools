// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    // <summary>
    // Enum describing row counts
    // </summary>
    internal enum RowCount : byte
    {
        // <summary>
        // Zero rows
        // </summary>
        Zero = 0,

        // <summary>
        // One row
        // </summary>
        One = 1,

        // <summary>
        // Unbounded (unknown number of rows)
        // </summary>
        Unbounded = 2,
    }
}
