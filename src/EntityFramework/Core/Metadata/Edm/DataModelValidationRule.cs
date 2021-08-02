// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    internal abstract class DataModelValidationRule
    {
        internal abstract Type ValidatedType { get; }
        internal abstract void Evaluate(EdmModelValidationContext context, MetadataItem item);
    }
}
