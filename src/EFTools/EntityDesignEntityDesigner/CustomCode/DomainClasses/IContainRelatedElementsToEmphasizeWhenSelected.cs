// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.ViewModel
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Modeling;

    internal interface IContainRelatedElementsToEmphasizeWhenSelected
    {
        IEnumerable<ModelElement> RelatedElementsToEmphasizeOnSelected { get; }
    }
}
