// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Data.Entity.Utilities;

    internal class ConventionsTypeActivator
    {
        public virtual IConvention Activate(Type conventionType)
        {
            DebugCheck.NotNull(conventionType);

            return (IConvention)Activator
                .CreateInstance(conventionType, nonPublic: true);
        }
    }
}
