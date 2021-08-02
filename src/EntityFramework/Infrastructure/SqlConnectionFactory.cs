// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Instances of this class are used to create DbConnection objects for
    /// SQL Server based on a given database name or connection string. By default, the connection is
    /// made to '.\SQLEXPRESS'.  This can be changed by changing the base connection
    /// string when constructing a factory instance.
    /// </summary>
    /// <remarks>
    /// An instance of this class can be set on the <see cref="Database" /> class to
    /// cause all DbContexts created with no connection information or just a database
    /// name or connection string to use SQL Server by default.
    /// This class is immutable since multiple threads may access instances simultaneously
    /// when creating connections.
    /// </remarks>
    public sealed class SqlConnectionFactory : IDbConnectionFactory
    {
        #region Constructors and fields

        // All fields should remain readonly since this is intended to be an immutable class.
        private readonly string _baseConnectionString;

        private Func<string, DbProviderFactory> _providerFactoryCreator;

        /// <summary>
        /// Creates a new connection factory with a default BaseConnectionString property of
        /// 'Data Source=.\SQLEXPRESS; Integrated Security=True; MultipleActiveResultSets=True;'.
        /// </summary>
        public SqlConnectionFactory()
        {
            _baseConnectionString = @"Data Source=.\SQLEXPRESS; Integrated Security=True; MultipleActiveResultSets=True;";
        }

        /// <summary>
        /// Creates a new connection factory with the given BaseConnectionString property.
        /// </summary>
        /// <param name="baseConnectionString"> The connection string to use for options to the database other than the 'Initial Catalog'. The 'Initial Catalog' will be prepended to this string based on the database name when CreateConnection is called. </param>
        public SqlConnectionFactory(string baseConnectionString)
        {
            Check.NotNull(baseConnectionString, "baseConnectionString");

            _baseConnectionString = baseConnectionString;
        }

        #endregion

        #region Properties

        // <summary>
        // Remove hard dependency on DbProviderFactories.
        // </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Func<string, DbProviderFactory> ProviderFactory
        {
            get { return _providerFactoryCreator ?? (DbConfiguration.DependencyResolver.GetService<DbProviderFactory>); }
            set
            {
                DebugCheck.NotNull(value);
                _providerFactoryCreator = value;
            }
        }

        /// <summary>
        /// The connection string to use for options to the database other than the 'Initial Catalog'.
        /// The 'Initial Catalog' will  be prepended to this string based on the database name when
        /// CreateConnection is called.
        /// The default is 'Data Source=.\SQLEXPRESS; Integrated Security=True;'.
        /// </summary>
        public string BaseConnectionString
        {
            get { return _baseConnectionString; }
        }

        #endregion

        #region CreateConnection

        /// <summary>
        /// Creates a connection for SQL Server based on the given database name or connection string.
        /// If the given string contains an '=' character then it is treated as a full connection string,
        /// otherwise it is treated as a database name only.
        /// </summary>
        /// <param name="nameOrConnectionString"> The database name or connection string. </param>
        /// <returns> An initialized DbConnection. </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public DbConnection CreateConnection(string nameOrConnectionString)
        {
            Check.NotEmpty(nameOrConnectionString, "nameOrConnectionString");

            // If the "name or connection string" contains an '=' character then it is treated as a connection string.
            var connectionString = nameOrConnectionString;
            if (!DbHelpers.TreatAsConnectionString(nameOrConnectionString))
            {
                if (nameOrConnectionString.EndsWith(".mdf", ignoreCase: true, culture: null))
                {
                    throw Error.SqlConnectionFactory_MdfNotSupported(nameOrConnectionString);
                }

                connectionString =
                    new SqlConnectionStringBuilder(BaseConnectionString)
                        {
                            InitialCatalog = nameOrConnectionString
                        }.ConnectionString;
            }

            DbConnection connection = null;
            try
            {
                connection = ProviderFactory("System.Data.SqlClient").CreateConnection();

                DbInterception.Dispatch.Connection.SetConnectionString(
                    connection,
                    new DbConnectionPropertyInterceptionContext<string>().WithValue(connectionString));
            }
            catch
            {
                // Fallback to hard-coded type if provider didn't work
                connection = new SqlConnection();

                DbInterception.Dispatch.Connection.SetConnectionString(
                    connection,
                    new DbConnectionPropertyInterceptionContext<string>().WithValue(connectionString));
            }

            return connection;
        }

        #endregion
    }
}
