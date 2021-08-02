// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio
{
    using System;

    internal class VsHelpersWrapper : IVsHelpers
    {
        public object GetDocData(IServiceProvider site, string documentPath)
        {
            return VSHelpers.GetDocData(site, documentPath);
        }
    }
}
