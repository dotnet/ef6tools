﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if !NET40
namespace System.Data.Entity.Configuration
{
    using MySql.Data.MySqlClient;
    using System.Configuration;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure;

    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private static readonly string _baseConnectionString = ConfigurationManager.AppSettings["BaseConnectionString"];

        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            var connectionBuilder = new MySqlConnectionStringBuilder(_baseConnectionString);
            connectionBuilder.Database = nameOrConnectionString;

            return new MySqlConnection(connectionBuilder.ToString());
        }
    }
}
#endif