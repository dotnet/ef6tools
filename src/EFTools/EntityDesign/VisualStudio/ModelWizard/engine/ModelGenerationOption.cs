// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    internal enum ModelGenerationOption
    {
        GenerateFromDatabase = 0,
        EmptyModel = 1,
        GenerateDatabaseScript = 3,
        EmptyModelCodeFirst = 4,
        CodeFirstFromDatabase = 5
    }
}