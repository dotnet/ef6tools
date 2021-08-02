// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.ConnectionFactoryConfig
{
    using System.Collections.Generic;
    using System.Data.Entity.Utilities;
    using System.Linq;

    // <summary>
    // Represents a specification for the default connection factory to be set into a config file.
    // </summary>
    internal class ConnectionFactorySpecification
    {
        public const string SqlConnectionFactoryName = "System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework";
        public const string SqlCeConnectionFactoryName = "System.Data.Entity.Infrastructure.SqlCeConnectionFactory, EntityFramework";
        public const string LocalDbConnectionFactoryName = "System.Data.Entity.Infrastructure.LocalDbConnectionFactory, EntityFramework";
        public const string SqlCompactProviderName = "System.Data.SqlServerCe.4.0";

        private readonly string _connectionFactoryName;
        private readonly IEnumerable<string> _constructorArguments;

        public ConnectionFactorySpecification(string connectionFactoryName, params string[] constructorArguments)
        {
            DebugCheck.NotEmpty(connectionFactoryName);

            _connectionFactoryName = connectionFactoryName;
            _constructorArguments = constructorArguments ?? Enumerable.Empty<string>();
        }

        public string ConnectionFactoryName
        {
            get { return _connectionFactoryName; }
        }

        public IEnumerable<string> ConstructorArguments
        {
            get { return _constructorArguments; }
        }
    }
}
