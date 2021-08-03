// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    using System.Data.Entity.Core.Metadata.Edm;

    // <summary>
    // A computed expression. Defined by a VarDefOp
    // </summary>
    internal sealed class ComputedVar : Var
    {
        internal ComputedVar(int id, TypeUsage type)
            : base(id, VarType.Computed, type)
        {
        }
    }
}
