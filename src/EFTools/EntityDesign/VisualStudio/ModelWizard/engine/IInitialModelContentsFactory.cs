// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    using System;

    internal interface IInitialModelContentsFactory
    {
        string GetInitialModelContents(Version targetSchemaVersion);
    }
}
