﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    internal abstract class ModelBuilderEngineHostContext
    {
        internal abstract void LogMessage(string s);
        internal abstract void DispatchToModelGenerationExtensions();
    }
}
