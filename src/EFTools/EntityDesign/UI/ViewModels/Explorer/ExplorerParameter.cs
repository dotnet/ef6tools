// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    using Microsoft.Data.Entity.Design.Base.Context;
    using Microsoft.Data.Entity.Design.Model.Entity;

    internal class ExplorerParameter : EntityDesignExplorerEFElement
    {
        public ExplorerParameter(EditingContext context, Parameter parameter, ExplorerEFElement parent)
            : base(context, parameter, parent)
        {
            // do nothing
        }

        protected override void LoadChildrenFromModel()
        {
            // do nothing
        }

        protected override void LoadWpfChildrenCollection()
        {
            // do nothing
        }

        internal override string ExplorerImageResourceKeyName
        {
            get { return "SprocParamPngIcon"; }
        }
    }
}
