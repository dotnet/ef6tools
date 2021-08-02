// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.FunctionImports
{
    using Microsoft.Data.Entity.Design.Base.Context;
    using Microsoft.Data.Entity.Design.Model;

    internal class MappingFunctionImportMappingRoot : MappingEFElement
    {
        public MappingFunctionImportMappingRoot(EditingContext context, EFElement modelItem, MappingEFElement parent)
            : base(context, modelItem, parent)
        {
        }

        internal MappingFunctionImport MappingFunctionImport
        {
            get { return GetParentOfType(typeof(MappingFunctionImport)) as MappingFunctionImport; }
        }
    }
}
