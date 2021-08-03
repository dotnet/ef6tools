// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.EntitySql.AST
{
    // <summary>
    // From clause item kind.
    // </summary>
    internal enum FromClauseItemKind
    {
        AliasedFromClause,
        JoinFromClause,
        ApplyFromClause
    }
}
