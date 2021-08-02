﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    using System;

    internal class UpdateModelFromDatabaseModelBuilderEngine : EdmxModelBuilderEngine
    {
        internal UpdateModelFromDatabaseModelBuilderEngine()
            : base(new InitialModelContentsFactory())
        {
        }

        protected override void UpdateDesignerInfo(EdmxHelper edmxHelper, ModelBuilderSettings settings)
        {
            // in the update model case, there is no need to fix up the designer info
            // since it would only be applied to the temporary artifact and then ignored
        }
    }
}
