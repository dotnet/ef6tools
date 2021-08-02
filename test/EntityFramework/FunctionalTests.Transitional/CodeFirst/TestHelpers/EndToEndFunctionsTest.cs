// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace FunctionalTests
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.SqlClient;

    public abstract class EndToEndFunctionsTest : TestBase, IDisposable
    {
        private readonly DbConnection _connection;

        protected EndToEndFunctionsTest()
        {
            var databaseName = GetType().FullName;
            var connectionString = SimpleConnectionString(databaseName);

            _connection = new SqlConnection(connectionString);

            using (var context = CreateContext())
            {
                if (!context.Database.Exists())
                {
                    context.Database.Create();
                }
                else if (!context.Database.CompatibleWithModel(throwIfNoMetadata: true))
                {
                    context.Database.Delete();
                    context.Database.Create();
                }
            }
        }

        public virtual void Dispose()
        {
            _connection.Dispose();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Types().Configure(e => e.MapToStoredProcedures());
        }

        protected DbContext CreateContext()
        {
            var modelBuilder = new DbModelBuilder();

            OnModelCreating(modelBuilder);

            // TODO: Remove when Work Item 947 is fixed
            Database.SetInitializer<DbContext>(null);

            var model = modelBuilder.Build(_connection);
            var compiledModel = model.Compile();

            return new DbContext(_connection, compiledModel, false);
        }

        protected void Execute(string commandText)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = commandText;

                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                command.ExecuteNonQuery();
            }
        }
    }
}
