// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    using System.Data.Entity.Core.Metadata.Edm;

    internal sealed class FunctionImportReturnTypeStructuralTypeColumn
    {
        internal readonly StructuralType Type;
        internal readonly bool IsTypeOf;
        internal readonly string ColumnName;
        internal readonly LineInfo LineInfo;

        internal FunctionImportReturnTypeStructuralTypeColumn(string columnName, StructuralType type, bool isTypeOf, LineInfo lineInfo)
        {
            ColumnName = columnName;
            IsTypeOf = isTypeOf;
            Type = type;
            LineInfo = lineInfo;
        }
    }
}
