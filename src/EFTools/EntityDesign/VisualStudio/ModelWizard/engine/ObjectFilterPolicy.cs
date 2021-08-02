﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    internal enum ObjectFilterPolicy
    {
        Allow, // Allow new instances of this object
        Exclude, // Exclude new instances of this object
        Optimal, // Picks either Allow or Exclude as the policy depending on which has the smallest changeset
    }
}
