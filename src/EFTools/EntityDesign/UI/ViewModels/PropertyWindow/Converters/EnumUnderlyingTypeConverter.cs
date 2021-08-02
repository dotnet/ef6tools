// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    using System.Linq;
    using Microsoft.Data.Entity.Design.Core.Controls;
    using Microsoft.Data.Entity.Design.Model;

    internal class EnumUnderlyingTypeConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            foreach (var primType in ModelHelper.UnderlyingEnumTypes.Select(t => t.Name))
            {
                AddMapping(primType, primType);
            }
        }
    }
}
