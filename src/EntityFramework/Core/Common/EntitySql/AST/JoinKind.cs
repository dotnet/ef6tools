// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.EntitySql.AST
{
    // <summary>
    // Represents join kind (cross,inner,leftouter,rightouter).
    // </summary>
    internal enum JoinKind
    {
        Cross,
        Inner,
        LeftOuter,
        FullOuter,
        RightOuter
    }
}
