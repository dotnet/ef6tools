﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    using Microsoft.Data.Entity.Design.Core.Controls;
    using Microsoft.Data.Entity.Design.Model;

    internal class ConcurrencyModeConverter : FixedListConverter<string>
    {
        protected override void PopulateMapping()
        {
            AddMapping(ModelConstants.ConcurrencyModeNone, ModelConstants.ConcurrencyModeNone);
            AddMapping(ModelConstants.ConcurrencyModeFixed, ModelConstants.ConcurrencyModeFixed);
        }
    }
}
