// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine
{
    using System;

    internal interface ISchemaListingSettings
    {
        /// <summary>
        /// EF Schema Version
        /// </summary>
        Version TargetSchemaVersion { get; }

        /// <summary>
        /// Runtime Provider Invariant Name
        /// </summary>
        string RuntimeProviderInvariantName { get; }

        /// <summary>
        /// Runtime Connection string value
        /// </summary>
        string RuntimeConnectionString { get; }
    }
}
