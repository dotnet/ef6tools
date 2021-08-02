// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.History
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Migrations.Edm;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Data.Entity.Migrations.Model;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Transactions;
    using System.Xml.Linq;

    internal class HistoryRepository : RepositoryBase
    {
        private static readonly string _productVersion = typeof(HistoryRepository).Assembly().GetInformationalVersion();

        public static readonly PropertyInfo MigrationIdProperty = typeof(HistoryRow).GetDeclaredProperty("MigrationId");
        public static readonly PropertyInfo ContextKeyProperty = typeof(HistoryRow).GetDeclaredProperty("ContextKey");

        private readonly string _contextKey;
        private readonly int? _commandTimeout;
        private readonly IEnumerable<string> _schemas;
        private readonly Func<DbConnection, string, HistoryContext> _historyContextFactory;
        private readonly DbContext _contextForInterception;
        private readonly int _contextKeyMaxLength;
        private readonly int _migrationIdMaxLength;
        private readonly DatabaseExistenceState _initialExistence;
        private readonly Func<Exception, bool> _permissionDeniedDetector;
        private readonly DbTransaction _existingTransaction;

        private string _currentSchema;
        private bool? _exists;
        private bool _contextKeyColumnExists;

        public HistoryRepository(
            InternalContext usersContext,
            string connectionString,
            DbProviderFactory providerFactory,
            string contextKey,
            int? commandTimeout,
            Func<DbConnection, string, HistoryContext> historyContextFactory,
            IEnumerable<string> schemas = null,
            DbContext contextForInterception = null,
            DatabaseExistenceState initialExistence = DatabaseExistenceState.Unknown,
            Func<Exception, bool> permissionDeniedDetector = null)
            : base(usersContext, connectionString, providerFactory)
        {
            DebugCheck.NotEmpty(contextKey);
            DebugCheck.NotNull(historyContextFactory);

            _initialExistence = initialExistence;
            _permissionDeniedDetector = permissionDeniedDetector;
            _commandTimeout = commandTimeout;
            _existingTransaction = usersContext.TryGetCurrentStoreTransaction();

            _schemas
                = new[] { EdmModelExtensions.DefaultSchema }
                    .Concat(schemas ?? Enumerable.Empty<string>())
                    .Distinct();

            _contextForInterception = contextForInterception;
            _historyContextFactory = historyContextFactory;
            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    var historyRowEntity
                        = ((IObjectContextAdapter)context).ObjectContext
                            .MetadataWorkspace
                            .GetItems<EntityType>(DataSpace.CSpace)
                            .Single(et => et.GetClrType() == typeof(HistoryRow));

                    var maxLength
                        = historyRowEntity
                            .Properties
                            .Single(p => p.GetClrPropertyInfo().IsSameAs(MigrationIdProperty))
                            .MaxLength;

                    _migrationIdMaxLength
                        = maxLength.HasValue
                            ? maxLength.Value
                            : HistoryContext.MigrationIdMaxLength;

                    maxLength
                        = historyRowEntity
                            .Properties
                            .Single(p => p.GetClrPropertyInfo().IsSameAs(ContextKeyProperty))
                            .MaxLength;

                    _contextKeyMaxLength
                        = maxLength.HasValue
                            ? maxLength.Value
                            : HistoryContext.ContextKeyMaxLength;
                }
            }
            finally
            {
                DisposeConnection(connection);
            }

            _contextKey = contextKey.RestrictTo(_contextKeyMaxLength);
        }

        public int ContextKeyMaxLength
        {
            get { return _contextKeyMaxLength; }
        }

        public int MigrationIdMaxLength
        {
            get { return _migrationIdMaxLength; }
        }
        
        public string CurrentSchema
        {
            get { return _currentSchema; }
            set
            {
                DebugCheck.NotEmpty(value);

                _currentSchema = value;
            }
        }

        public virtual XDocument GetLastModel(out string migrationId, out string productVersion, string contextKey = null)
        {
            migrationId = null;
            productVersion = null;

            if (!Exists(contextKey))
            {
                return null;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        var baseQuery
                            = CreateHistoryQuery(context, contextKey)
                                .OrderByDescending(h => h.MigrationId);

                        var lastModel
                            = baseQuery
                                .Select(
                                    s => new
                                        {
                                            s.MigrationId,
                                            s.Model,
                                            s.ProductVersion
                                        })
                                .FirstOrDefault();

                        if (lastModel == null)
                        {
                            return null;
                        }

                        migrationId = lastModel.MigrationId;
                        productVersion = lastModel.ProductVersion;

                        return new ModelCompressor().Decompress(lastModel.Model);
                    }
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual XDocument GetModel(string migrationId, out string productVersion)
        {
            DebugCheck.NotEmpty(migrationId);

            productVersion = null;

            if (!Exists())
            {
                return null;
            }

            migrationId = migrationId.RestrictTo(_migrationIdMaxLength);

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    var baseQuery
                        = CreateHistoryQuery(context)
                            .Where(h => h.MigrationId == migrationId);

                    var model
                        = baseQuery
                            .Select(
                                h => new
                                    {
                                        h.Model,
                                        h.ProductVersion
                                    })
                            .SingleOrDefault();

                    if (model == null)
                    {
                        return null;
                    }

                    productVersion = model.ProductVersion;

                    return new ModelCompressor().Decompress(model.Model);
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual IEnumerable<string> GetPendingMigrations(IEnumerable<string> localMigrations)
        {
            DebugCheck.NotNull(localMigrations);

            if (!Exists())
            {
                return localMigrations;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    List<string> databaseMigrations;
                    using (new TransactionScope(TransactionScopeOption.Suppress))
                    {
                        databaseMigrations = CreateHistoryQuery(context)
                            .Select(h => h.MigrationId)
                            .ToList();
                    }

                    localMigrations
                        = localMigrations
                            .Select(m => m.RestrictTo(_migrationIdMaxLength))
                            .ToArray();

                    var pendingMigrations = localMigrations.Except(databaseMigrations);
                    var firstDatabaseMigration = databaseMigrations.FirstOrDefault();
                    var firstLocalMigration = localMigrations.FirstOrDefault();

                    // If the first database migration and the first local migration don't match,
                    // but both are named InitialCreate then treat it as already applied. This can
                    // happen when trying to migrate a database that was created using initializers
                    if (firstDatabaseMigration != firstLocalMigration
                        && firstDatabaseMigration != null
                        && firstDatabaseMigration.MigrationName() == Strings.InitialCreate
                        && firstLocalMigration != null
                        && firstLocalMigration.MigrationName() == Strings.InitialCreate)
                    {
                        Debug.Assert(pendingMigrations.First() == firstLocalMigration);

                        pendingMigrations = pendingMigrations.Skip(1);
                    }

                    return pendingMigrations.ToList();
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual IEnumerable<string> GetMigrationsSince(string migrationId)
        {
            DebugCheck.NotEmpty(migrationId);

            var exists = Exists();

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    var query = CreateHistoryQuery(context);

                    migrationId = migrationId.RestrictTo(_migrationIdMaxLength);

                    if (migrationId != DbMigrator.InitialDatabase)
                    {
                        if (!exists
                            || !query.Any(h => h.MigrationId == migrationId))
                        {
                            throw Error.MigrationNotFound(migrationId);
                        }

                        query = query.Where(h => string.Compare(h.MigrationId, migrationId, StringComparison.Ordinal) > 0);
                    }
                    else if (!exists)
                    {
                        return Enumerable.Empty<string>();
                    }

                    return query
                        .OrderByDescending(h => h.MigrationId)
                        .Select(h => h.MigrationId)
                        .ToList();
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual string GetMigrationId(string migrationName)
        {
            DebugCheck.NotEmpty(migrationName);

            if (!Exists())
            {
                return null;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    var migrationIds
                        = CreateHistoryQuery(context)
                            .Select(h => h.MigrationId)
                            .Where(m => m.Substring(16) == migrationName)
                            .ToList();

                    if (!migrationIds.Any())
                    {
                        return null;
                    }

                    if (migrationIds.Count() == 1)
                    {
                        return migrationIds.Single();
                    }

                    throw Error.AmbiguousMigrationName(migrationName);
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        private IQueryable<HistoryRow> CreateHistoryQuery(HistoryContext context, string contextKey = null)
        {
            IQueryable<HistoryRow> q = context.History;

            contextKey
                = !string.IsNullOrWhiteSpace(contextKey)
                    ? contextKey.RestrictTo(_contextKeyMaxLength)
                    : _contextKey;

            if (_contextKeyColumnExists)
            {
                q = q.Where(h => h.ContextKey == contextKey);
            }

            return q;
        }

        public virtual bool IsShared()
        {
            if (!Exists()
                || !_contextKeyColumnExists)
            {
                return false;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    return context.History.Any(hr => hr.ContextKey != _contextKey);
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual bool HasMigrations()
        {
            if (!Exists())
            {
                return false;
            }

            if (!_contextKeyColumnExists)
            {
                return true;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    return context.History.Count(hr => hr.ContextKey == _contextKey) > 0;
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual bool Exists(string contextKey = null)
        {
            if (_exists == null)
            {
                _exists = QueryExists(contextKey ?? _contextKey);
            }

            return _exists.Value;
        }

        private bool QueryExists(string contextKey)
        {
            DebugCheck.NotNull(contextKey);

            if (_initialExistence == DatabaseExistenceState.DoesNotExist)
            {
                return false;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                if (_initialExistence == DatabaseExistenceState.Unknown)
                {
                    using (var context = CreateContext(connection))
                    {
                        if (!context.Database.Exists())
                        {
                            return false;
                        }
                    }
                }

                foreach (var schema in _schemas.Reverse())
                {
                    using (var context = CreateContext(connection, schema))
                    {
                        _currentSchema = schema;
                        _contextKeyColumnExists = true;

                        // Do the context-key specific query first, since if it succeeds we can avoid
                        // doing the more general query.
                        try
                        {
                            using (new TransactionScope(TransactionScopeOption.Suppress))
                            {
                                contextKey = contextKey.RestrictTo(_contextKeyMaxLength);

                                if (context.History.Count(hr => hr.ContextKey == contextKey) > 0)
                                {
                                    return true;
                                }
                            }
                        }
                        catch (EntityException entityException)
                        {
                            if (_permissionDeniedDetector != null 
                                && _permissionDeniedDetector(entityException.InnerException))
                            {
                                throw;
                            }

                            _contextKeyColumnExists = false;
                        }

                        // If the context-key specific query failed, then try the general query to see
                        // if there is a history table in this schema at all
                        if (!_contextKeyColumnExists)
                        {
                            try
                            {
                                using (new TransactionScope(TransactionScopeOption.Suppress))
                                {
                                    context.History.Count();
                                }
                            }
                            catch (EntityException entityException)
                            {
                                if (_permissionDeniedDetector != null 
                                    && _permissionDeniedDetector(entityException.InnerException))
                                {
                                    throw;
                                }

                                _currentSchema = null;
                            }
                        }
                    }
                }
            }
            finally
            {
                DisposeConnection(connection);
            }

            return !string.IsNullOrWhiteSpace(_currentSchema);
        }

        public virtual void ResetExists()
        {
            _exists = null;
        }

        public virtual IEnumerable<MigrationOperation> GetUpgradeOperations()
        {
            if (!Exists())
            {
                yield break;
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                var tableName = "dbo." + HistoryContext.DefaultTableName;

                DbProviderManifest providerManifest;
                if (connection.GetProviderInfo(out providerManifest).IsSqlCe())
                {
                    tableName = HistoryContext.DefaultTableName;
                }

                using (var context = new LegacyHistoryContext(connection))
                {
                    var createdOnExists = false;

                    try
                    {
                        InjectInterceptionContext(context);

                        using (new TransactionScope(TransactionScopeOption.Suppress))
                        {
                            context.History
                                .Select(h => h.CreatedOn)
                                .FirstOrDefault();
                        }

                        createdOnExists = true;
                    }
                    catch (EntityException)
                    {
                    }

                    if (createdOnExists)
                    {
                        yield return new DropColumnOperation(tableName, "CreatedOn");
                    }
                }

                using (var context = CreateContext(connection))
                {
                    if (!_contextKeyColumnExists)
                    {
                        if (_historyContextFactory != HistoryContext.DefaultFactory)
                        {
                            throw Error.UnableToUpgradeHistoryWhenCustomFactory();
                        }

                        yield return new AddColumnOperation(
                            tableName,
                            new ColumnModel(PrimitiveTypeKind.String)
                                {
                                    MaxLength = _contextKeyMaxLength,
                                    Name = "ContextKey",
                                    IsNullable = false,
                                    DefaultValue = _contextKey
                                });

                        var emptyModel = new DbModelBuilder().Build(connection).GetModel();
                        var createTableOperation = (CreateTableOperation)
                            new EdmModelDiffer().Diff(emptyModel, context.GetModel()).Single();

                        var dropPrimaryKeyOperation
                            = new DropPrimaryKeyOperation
                                {
                                    Table = tableName,
                                    CreateTableOperation = createTableOperation
                                };

                        dropPrimaryKeyOperation.Columns.Add("MigrationId");

                        yield return dropPrimaryKeyOperation;

                        yield return new AlterColumnOperation(
                            tableName,
                            new ColumnModel(PrimitiveTypeKind.String)
                                {
                                    MaxLength = _migrationIdMaxLength,
                                    Name = "MigrationId",
                                    IsNullable = false
                                },
                            isDestructiveChange: false);

                        var addPrimaryKeyOperation
                            = new AddPrimaryKeyOperation
                                {
                                    Table = tableName
                                };

                        addPrimaryKeyOperation.Columns.Add("MigrationId");
                        addPrimaryKeyOperation.Columns.Add("ContextKey");

                        yield return addPrimaryKeyOperation;
                    }
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual MigrationOperation CreateInsertOperation(string migrationId, VersionedModel versionedModel)
        {
            DebugCheck.NotEmpty(migrationId);
            DebugCheck.NotNull(versionedModel);

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    context.History.Add(
                        new HistoryRow
                            {
                                MigrationId = migrationId.RestrictTo(_migrationIdMaxLength),
                                ContextKey = _contextKey,
                                Model = new ModelCompressor().Compress(versionedModel.Model),
                                ProductVersion = versionedModel.Version ?? _productVersion
                            });

                    using (var commandTracer = new CommandTracer(context))
                    {
                        context.SaveChanges();

                        return new HistoryOperation(
                            commandTracer.CommandTrees.OfType<DbModificationCommandTree>().ToList());
                    }
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual MigrationOperation CreateDeleteOperation(string migrationId)
        {
            DebugCheck.NotEmpty(migrationId);

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    var historyRow
                        = new HistoryRow
                            {
                                MigrationId = migrationId.RestrictTo(_migrationIdMaxLength),
                                ContextKey = _contextKey
                            };

                    context.History.Attach(historyRow);
                    context.History.Remove(historyRow);

                    using (var commandTracer = new CommandTracer(context))
                    {
                        context.SaveChanges();

                        return new HistoryOperation(
                            commandTracer.CommandTrees.OfType<DbModificationCommandTree>().ToList());
                    }
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public virtual IEnumerable<DbQueryCommandTree> CreateDiscoveryQueryTrees()
        {
            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                foreach (var schema in _schemas)
                {
                    using (var context = CreateContext(connection, schema))
                    {
                        var query
                            = context.History
                                .Where(h => h.ContextKey == _contextKey)
                                .Select(s => s.MigrationId)
                                .OrderByDescending(s => s);

                        var dbQuery = query as DbQuery<string>;

                        if (dbQuery != null)
                        {
                            dbQuery.InternalQuery.ObjectQuery.EnablePlanCaching = false;
                        }

                        using (var commandTracer = new CommandTracer(context))
                        {
                            query.First();

                            var queryTree
                                = commandTracer
                                    .CommandTrees
                                    .OfType<DbQueryCommandTree>()
                                    .Single(t => t.DataSpace == DataSpace.SSpace);

                            yield return
                                new DbQueryCommandTree(
                                    queryTree.MetadataWorkspace,
                                    queryTree.DataSpace,
                                    queryTree.Query.Accept(
                                        new ParameterInliner(
                                            commandTracer.DbCommands.Single().Parameters)));
                        }
                    }
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        private class ParameterInliner : DefaultExpressionVisitor
        {
            private readonly DbParameterCollection _parameters;

            public ParameterInliner(DbParameterCollection parameters)
            {
                DebugCheck.NotNull(parameters);

                _parameters = parameters;
            }

            public override DbExpression Visit(DbParameterReferenceExpression expression)
            {
                // Inline parameters
                return DbExpressionBuilder.Constant(_parameters[expression.ParameterName].Value);
            }

            // Removes null parameter checks

            public override DbExpression Visit(DbOrExpression expression)
            {
                return expression.Left.Accept(this);
            }

            public override DbExpression Visit(DbAndExpression expression)
            {
                if (expression.Right is DbNotExpression)
                {
                    return expression.Left.Accept(this);
                }

                return base.Visit(expression);
            }
        }

        public virtual void BootstrapUsingEFProviderDdl(VersionedModel versionedModel)
        {
            DebugCheck.NotNull(versionedModel);

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();

                using (var context = CreateContext(connection))
                {
                    context.Database.ExecuteSqlCommand(
                        ((IObjectContextAdapter)context).ObjectContext.CreateDatabaseScript());

                    context.History.Add(
                        new HistoryRow
                            {
                                MigrationId = MigrationAssembly
                                    .CreateMigrationId(Strings.InitialCreate)
                                    .RestrictTo(_migrationIdMaxLength),
                                ContextKey = _contextKey,
                                Model = new ModelCompressor().Compress(versionedModel.Model),
                                ProductVersion = versionedModel.Version ?? _productVersion
                            });

                    context.SaveChanges();
                }
            }
            finally
            {
                DisposeConnection(connection);
            }
        }

        public HistoryContext CreateContext(DbConnection connection, string schema = null)
        {
            DebugCheck.NotNull(connection);

            var context = _historyContextFactory(connection, schema ?? CurrentSchema);

            context.Database.CommandTimeout = _commandTimeout;

            if (_existingTransaction != null)
            {
                Debug.Assert(_existingTransaction.Connection == connection);

                if (_existingTransaction.Connection == connection)
                {
                    context.Database.UseTransaction(_existingTransaction);
                }
            }

            InjectInterceptionContext(context);

            return context;
        }

        private void InjectInterceptionContext(DbContext context)
        {
            if (_contextForInterception != null)
            {
                var objectContext = context.InternalContext.ObjectContext;

                objectContext.InterceptionContext
                    = objectContext.InterceptionContext.WithDbContext(_contextForInterception);
            }
        }
    }
}
