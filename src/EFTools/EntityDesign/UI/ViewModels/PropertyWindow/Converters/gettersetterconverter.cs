// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    using Microsoft.Data.Entity.Design.Model;

    internal class GetterSetterConverter : AccessConverter
    {
        protected override void PopulateMapping()
        {
            base.PopulateMapping();
            AddMapping(ModelConstants.CodeGenerationAccessProtected, ModelConstants.CodeGenerationAccessProtected);
            AddMapping(ModelConstants.CodeGenerationAccessPrivate, ModelConstants.CodeGenerationAccessPrivate);
        }
    }
}
