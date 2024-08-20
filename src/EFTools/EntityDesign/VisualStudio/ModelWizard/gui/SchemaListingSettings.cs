// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Gui
{
    using System;
    using Microsoft.Data.Entity.Design.VisualStudio.ModelWizard.Engine;
    using Microsoft.Data.Entity.Design.VisualStudio.Package;

    /// <summary>
    /// This object adapts the properties of the ModelBuilderWizardForm into a consistent form for getting the schema of a SQL Server
    /// </summary>
    internal sealed class SchemaListingSettings : ISchemaListingSettings
    {
        public Version TargetSchemaVersion { get; private set; }

        public string RuntimeProviderInvariantName { get; private set; }

        public string RuntimeConnectionString { get; private set; }

        internal static SchemaListingSettings FromWizard(ModelBuilderWizardForm wizard)
        {
            string runtimeConnectionString = ConnectionManager.TranslateConnectionStringFromDesignTime(
                wizard.ServiceProvider,
                wizard.ModelBuilderSettings.Project,
                wizard.ModelBuilderSettings.RuntimeProviderInvariantName,
                wizard.ModelBuilderSettings.DesignTimeConnectionString);

            SchemaListingSettings settings = new SchemaListingSettings()
            {
                TargetSchemaVersion = wizard.ModelBuilderSettings.TargetSchemaVersion,
                RuntimeProviderInvariantName = wizard.ModelBuilderSettings.RuntimeProviderInvariantName,
                RuntimeConnectionString = runtimeConnectionString,
            };
            return settings;
        }
    }
}
