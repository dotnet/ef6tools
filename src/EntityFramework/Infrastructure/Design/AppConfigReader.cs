﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Design
{
    using System.Configuration;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Internal.ConfigFile;
    using System.Data.Entity.Utilities;
    using System.Linq;

    /// <summary>
    /// Provides utility methods for reading from an App.config or Web.config file.
    /// </summary>
    public class AppConfigReader
    {
        private readonly Configuration _configuration;

        /// <summary>
        /// Initializes a new instance of <see cref="AppConfigReader" />.
        /// </summary>
        /// <param name="configuration">The configuration to read from.</param>
        public AppConfigReader(Configuration configuration)
        {
            Check.NotNull(configuration, "configuration");

            _configuration = configuration;
        }

        /// <summary>
        /// Gets the specified provider services from the configuration.
        /// </summary>
        /// <param name="invariantName">The invariant name of the provider services.</param>
        /// <returns>The provider services type name, or null if not found.</returns>
        public string GetProviderServices(string invariantName)
        {
            var providers = ((EntityFrameworkSection)_configuration.GetSection(AppConfig.EFSectionName))
                .Providers.Cast<ProviderElement>();

            return (from p in providers
                    where p.InvariantName == invariantName
                    select p.ProviderTypeName)
                    .FirstOrDefault();
        }
    }
}
