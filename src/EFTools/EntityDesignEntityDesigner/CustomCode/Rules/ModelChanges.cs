﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.Rules
{
    using Microsoft.Data.Entity.Design.Model.Commands;
    using Microsoft.Data.Tools.VSXmlDesignerBase.VisualStudio.Modeling;

    internal abstract class ViewModelChange : CommonViewModelChange
    {
        internal virtual bool IsDiagramChange
        {
            get { return false; }
        }

        internal abstract void Invoke(CommandProcessorContext cpc);
    }
}
