// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Mapping
{
    /// <summary>
    /// Base class for mapping a property of a function import return type.
    /// </summary>
    public abstract class FunctionImportReturnTypePropertyMapping : MappingItem
    {
        internal readonly LineInfo LineInfo;

        internal FunctionImportReturnTypePropertyMapping(LineInfo lineInfo)
        {
            LineInfo = lineInfo;
        }

        internal abstract string CMember { get; }
        internal abstract string SColumn { get; }
    }
}
