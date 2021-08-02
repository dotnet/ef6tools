﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Configuration
{
#if !NET40
    using MySql.Data.MySqlClient;
#endif
    using System.Configuration;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.SqlServer;
    using System.Runtime.Remoting.Messaging;

    public class ProviderAgnosticConfiguration : DbConfiguration
    {
        private static readonly string _providerInvariantName = ConfigurationManager.AppSettings["ProviderInvariantName"];
        private static readonly string _baseConnectionString = ConfigurationManager.AppSettings["BaseConnectionString"];

        public ProviderAgnosticConfiguration()
        {
#if !NET40
            SetHistoryContext(
                "MySql.Data.MySqlClient",
                (connection, defaultSchema) => new MySqlHistoryContext(connection, defaultSchema));
#endif

            SetExecutionStrategy("System.Data.SqlClient", () => SuspendExecutionStrategy
              ? (IDbExecutionStrategy)new DefaultExecutionStrategy()
              : new SqlAzureExecutionStrategy());

            switch (_providerInvariantName)
            {
                case "System.Data.SqlClient":
                    SetDefaultConnectionFactory(new SqlConnectionFactory(_baseConnectionString));
                    break;

#if !NET40
                case "MySql.Data.MySqlClient" :
                    SetDefaultConnectionFactory(new MySqlConnectionFactory());
                    break;
#endif

                default:
                    throw new InvalidOperationException("Unknown ProviderInvariantName specified in App.config: " + _providerInvariantName);
            }

            AddDependencyResolver(MutableResolver.Instance);
        }

        public static bool SuspendExecutionStrategy
        {
            get
            {
                return (bool?)CallContext.LogicalGetData("SuspendExecutionStrategy") ?? false;
            }
            set
            {
                CallContext.LogicalSetData("SuspendExecutionStrategy", value);
            }
        }
    }
}
