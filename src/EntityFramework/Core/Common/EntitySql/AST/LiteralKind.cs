// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.EntitySql.AST
{
    // <summary>
    // Defines literal value kind, including the eSQL untyped NULL.
    // </summary>
    internal enum LiteralKind
    {
        Number,
        String,
        UnicodeString,
        Boolean,
        Binary,
        DateTime,
        Time,
        DateTimeOffset,
        Guid,
        Null
    }
}
