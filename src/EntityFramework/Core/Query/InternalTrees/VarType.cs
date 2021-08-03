// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    // <summary>
    // Types of variable
    // </summary>
    internal enum VarType
    {
        // <summary>
        // a parameter
        // </summary>
        Parameter,

        // <summary>
        // Column of a table
        // </summary>
        Column,

        // <summary>
        // A Computed var
        // </summary>
        Computed,

        // <summary>
        // Var for SetOps (Union, Intersect, Except)
        // </summary>
        SetOp,

        // <summary>
        // NotValid
        // </summary>
        NotValid
    }
}
