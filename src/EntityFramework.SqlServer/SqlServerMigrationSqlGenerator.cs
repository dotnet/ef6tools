// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Migrations.History;
    using System.Data.Entity.Migrations.Model;
    using System.Data.Entity.Migrations.Sql;
    using System.Data.Entity.Migrations.Utilities;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.SqlServer.Resources;
    using System.Data.Entity.SqlServer.SqlGen;
    using System.Data.Entity.SqlServer.Utilities;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provider to convert provider agnostic migration operations into SQL commands
    /// that can be run against a Microsoft SQL Server database.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public class SqlServerMigrationSqlGenerator : MigrationSqlGenerator
    {
        private const string BatchTerminator = "GO";

        internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK";
        internal const string DateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        private SqlGenerator _sqlGenerator;
        private List<MigrationStatement> _statements;
        private HashSet<string> _generatedSchemas;

        private string _providerManifestToken;
        private int _variableCounter;

        /// <summary>
        /// Determines if a provider specific exception corresponds to a database-level permission denied error.
        /// </summary>
        /// <param name="exception">The database exception.</param>
        /// <returns> true if the supplied exception corresponds to a database-level permission denied error; otherwise false. </returns>
        public override bool IsPermissionDeniedError(Exception exception)
        {
            var sqlException = exception as SqlException;

            return sqlException != null && sqlException.Number == 229;
        }

        /// <summary>
        /// Converts a set of migration operations into Microsoft SQL Server specific SQL.
        /// </summary>
        /// <param name="migrationOperations"> The operations to be converted. </param>
        /// <param name="providerManifestToken"> Token representing the version of SQL Server being targeted (i.e. "2005", "2008"). </param>
        /// <returns> A list of SQL statements to be executed to perform the migration operations. </returns>
        public override IEnumerable<MigrationStatement> Generate(
            IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
        {
            Check.NotNull(migrationOperations, "migrationOperations");
            Check.NotNull(providerManifestToken, "providerManifestToken");

            _statements = new List<MigrationStatement>();
            _generatedSchemas = new HashSet<string>();

            InitializeProviderServices(providerManifestToken);
            GenerateStatements(migrationOperations);

            return _statements;
        }

        private void GenerateStatements(IEnumerable<MigrationOperation> migrationOperations)
        {
            Check.NotNull(migrationOperations, "migrationOperations");

            DetectHistoryRebuild(migrationOperations).Each<dynamic>(o => Generate(o));
        }

        /// <summary>
        /// Generates the SQL body for a stored procedure.
        /// </summary>
        /// <param name="commandTrees">The command trees representing the commands for an insert, update or delete operation.</param>
        /// <param name="rowsAffectedParameter">The rows affected parameter name.</param>
        /// <param name="providerManifestToken">The provider manifest token.</param>
        /// <returns>The SQL body for the stored procedure.</returns>
        public override string GenerateProcedureBody(
            ICollection<DbModificationCommandTree> commandTrees,
            string rowsAffectedParameter,
            string providerManifestToken)
        {
            Check.NotNull(commandTrees, "commandTrees");
            Check.NotEmpty(providerManifestToken, "providerManifestToken");

            if (!commandTrees.Any())
            {
                return "RETURN";
            }

            InitializeProviderServices(providerManifestToken);

            return GenerateFunctionSql(commandTrees, rowsAffectedParameter);
        }

        private void InitializeProviderServices(string providerManifestToken)
        {
            Check.NotEmpty(providerManifestToken, "providerManifestToken");

            _providerManifestToken = providerManifestToken;

            using (var connection = CreateConnection())
            {
                ProviderManifest
                    = DbProviderServices
                        .GetProviderServices(connection)
                        .GetProviderManifest(providerManifestToken);

                _sqlGenerator = new SqlGenerator(SqlVersionUtils.GetSqlVersion(providerManifestToken));
            }
        }

        private string GenerateFunctionSql(ICollection<DbModificationCommandTree> commandTrees, string rowsAffectedParameter)
        {
            DebugCheck.NotNull(commandTrees);
            Debug.Assert(commandTrees.Any());

            var functionSqlGenerator = new DmlFunctionSqlGenerator(_sqlGenerator);

            switch (commandTrees.First().CommandTreeKind)
            {
                case DbCommandTreeKind.Insert:
                    return functionSqlGenerator.GenerateInsert(commandTrees.Cast<DbInsertCommandTree>().ToList());
                case DbCommandTreeKind.Update:
                    return functionSqlGenerator.GenerateUpdate(commandTrees.Cast<DbUpdateCommandTree>().ToList(), rowsAffectedParameter);
                case DbCommandTreeKind.Delete:
                    return functionSqlGenerator.GenerateDelete(commandTrees.Cast<DbDeleteCommandTree>().ToList(), rowsAffectedParameter);
            }

            return null;
        }

        /// <summary>
        /// Generates the specified update database operation which represents applying a series of migrations.
        /// The generated script is idempotent, meaning it contains conditional logic to check if individual migrations 
        /// have already been applied and only apply the pending ones.
        /// </summary>
        /// <param name="updateDatabaseOperation">The update database operation.</param>
        protected virtual void Generate(UpdateDatabaseOperation updateDatabaseOperation)
        {
            Check.NotNull(updateDatabaseOperation, "updateDatabaseOperation");

            if (!updateDatabaseOperation.Migrations.Any())
            {
                return;
            }

            using (var writer = Writer())
            {
                writer.WriteLine("DECLARE @CurrentMigration [nvarchar](max)");
                writer.WriteLine();

                foreach (var historyQueryTree in updateDatabaseOperation.HistoryQueryTrees)
                {
                    HashSet<string> _;
                    var historyQuery
                        = _sqlGenerator.GenerateSql(historyQueryTree, out _);

                    writer.Write("IF object_id('");
                    writer.Write(Escape(_sqlGenerator.Targets.Single()));
                    writer.WriteLine("') IS NOT NULL");
                    writer.Indent++;
                    writer.WriteLine("SELECT @CurrentMigration =");
                    writer.Indent++;
                    writer.Write("(");
                    writer.Write(Indent(historyQuery, writer.CurrentIndentation()));
                    writer.WriteLine(")");
                    writer.Indent -= 2;
                    writer.WriteLine();
                }

                writer.WriteLine("IF @CurrentMigration IS NULL");
                writer.Indent++;
                writer.WriteLine("SET @CurrentMigration = '0'");

                Statement(writer);
            }

            var existingStatements = _statements;

            foreach (var migration in updateDatabaseOperation.Migrations)
            {
                using (var writer = Writer())
                {
                    _statements = new List<MigrationStatement>();

                    GenerateStatements(migration.Operations);

                    if (_statements.Count > 0)
                    {
                        writer.Write("IF @CurrentMigration < '");
                        writer.Write(Escape(migration.MigrationId));
                        writer.WriteLine("'");
                        writer.Write("BEGIN");

                        using (var blockWriter = Writer())
                        {
                            blockWriter.WriteLine();
                            blockWriter.Indent++;

                            foreach (var migrationStatement in _statements)
                            {
                                if (string.IsNullOrWhiteSpace(migrationStatement.BatchTerminator))
                                {
                                    migrationStatement.Sql.EachLine(blockWriter.WriteLine);
                                }
                                else
                                {
                                    blockWriter.WriteLine("EXECUTE('");
                                    blockWriter.Indent++;
                                    migrationStatement.Sql.EachLine(l => blockWriter.WriteLine(Escape(l)));
                                    blockWriter.Indent--;
                                    blockWriter.WriteLine("')");
                                }
                            }

                            writer.WriteLine(blockWriter.InnerWriter.ToString().TrimEnd());
                        }

                        writer.WriteLine("END");

                        existingStatements.Add(
                            new MigrationStatement
                                {
                                    Sql
                                        = writer.InnerWriter.ToString()
                                });
                    }
                }
            }

            _statements = existingStatements;
        }

        /// <summary>
        /// Generates SQL for a <see cref="MigrationOperation" />.
        /// Allows derived providers to handle additional operation types.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="migrationOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(MigrationOperation migrationOperation)
        {
            Check.NotNull(migrationOperation, "migrationOperation");

            throw Error.SqlServerMigrationSqlGenerator_UnknownOperation(GetType().Name, migrationOperation.GetType().FullName);
        }

        /// <summary>
        /// Creates an empty connection for the current provider.
        /// Allows derived providers to use connection other than <see cref="SqlConnection" />.
        /// </summary>
        /// <returns> An empty connection for the current provider. </returns>
        protected virtual DbConnection CreateConnection()
        {
            return DbConfiguration.DependencyResolver.GetService<DbProviderFactory>("System.Data.SqlClient").CreateConnection();
        }

        /// <summary>
        /// Generates the specified create procedure operation.
        /// </summary>
        /// <param name="createProcedureOperation">The create procedure operation.</param>
        protected virtual void Generate(CreateProcedureOperation createProcedureOperation)
        {
            Check.NotNull(createProcedureOperation, "createProcedureOperation");

            Generate(createProcedureOperation, "CREATE");
        }

        /// <summary>
        /// Generates the specified alter procedure operation.
        /// </summary>
        /// <param name="alterProcedureOperation">The alter procedure operation.</param>
        protected virtual void Generate(AlterProcedureOperation alterProcedureOperation)
        {
            Check.NotNull(alterProcedureOperation, "alterProcedureOperation");

            Generate(alterProcedureOperation, "ALTER");
        }

        private void Generate(ProcedureOperation procedureOperation, string modifier)
        {
            DebugCheck.NotNull(procedureOperation);
            DebugCheck.NotEmpty(modifier);

            using (var writer = Writer())
            {
                writer.Write(modifier);
                writer.WriteLine(" PROCEDURE " + Name(procedureOperation.Name));
                writer.Indent++;

                procedureOperation.Parameters.Each(
                    (p, i) =>
                    {
                        Generate(p, writer);
                        writer.WriteLine(
                            i < procedureOperation.Parameters.Count - 1
                                ? ","
                                : string.Empty);
                    });

                writer.Indent--;
                writer.WriteLine("AS");
                writer.WriteLine("BEGIN");
                writer.Indent++;

                writer.WriteLine(
                    !string.IsNullOrWhiteSpace(procedureOperation.BodySql)
                        ? Indent(procedureOperation.BodySql, writer.CurrentIndentation())
                        : "RETURN");

                writer.Indent--;
                writer.Write("END");

                Statement(writer, batchTerminator: BatchTerminator);
            }
        }

        private void Generate(ParameterModel parameterModel, IndentedTextWriter writer)
        {
            DebugCheck.NotNull(parameterModel);
            DebugCheck.NotNull(writer);

            writer.Write("@");
            writer.Write(parameterModel.Name);
            writer.Write(" ");
            writer.Write(BuildPropertyType(parameterModel));

            if (parameterModel.IsOutParameter)
            {
                writer.Write(" OUT");
            }

            if (parameterModel.DefaultValue != null)
            {
                writer.Write(" = ");
                writer.Write(Generate((dynamic)parameterModel.DefaultValue));
            }
            else if (!string.IsNullOrWhiteSpace(parameterModel.DefaultValueSql))
            {
                writer.Write(" = ");
                writer.Write(parameterModel.DefaultValueSql);
            }
        }

        /// <summary>
        /// Generates the specified drop procedure operation.
        /// </summary>
        /// <param name="dropProcedureOperation">The drop procedure operation.</param>
        protected virtual void Generate(DropProcedureOperation dropProcedureOperation)
        {
            Check.NotNull(dropProcedureOperation, "dropProcedureOperation");

            using (var writer = Writer())
            {
                writer.Write("DROP PROCEDURE ");
                writer.Write(Name(dropProcedureOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="CreateTableOperation" />. This method differs from
        /// <see cref="WriteCreateTable(System.Data.Entity.Migrations.Model.CreateTableOperation)" /> in that it will
        /// create the target database schema if it does not already exist.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(CreateTableOperation createTableOperation)
        {
            Check.NotNull(createTableOperation, "createTableOperation");

            var databaseName = DatabaseName.Parse(createTableOperation.Name);

            if (!string.IsNullOrWhiteSpace(databaseName.Schema))
            {
                if (!databaseName.Schema.EqualsIgnoreCase("dbo")
                    && !_generatedSchemas.Contains(databaseName.Schema))
                {
                    GenerateCreateSchema(databaseName.Schema);

                    _generatedSchemas.Add(databaseName.Schema);
                }
            }

            WriteCreateTable(createTableOperation);
        }

        /// <summary>
        /// Generates SQL for a <see cref="CreateTableOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createTableOperation"> The operation to produce SQL for. </param>
        protected virtual void WriteCreateTable(CreateTableOperation createTableOperation)
        {
            Check.NotNull(createTableOperation, "createTableOperation");

            using (var writer = Writer())
            {
                WriteCreateTable(createTableOperation, writer);

                Statement(writer);
            }
        }

        /// <summary>
        /// Writes CREATE TABLE SQL to the target writer.
        /// </summary>
        /// <param name="createTableOperation"> The operation to produce SQL for. </param>
        /// <param name="writer"> The target writer. </param>
        protected virtual void WriteCreateTable(CreateTableOperation createTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(createTableOperation, "createTableOperation");
            Check.NotNull(writer, "writer");

            writer.WriteLine("CREATE TABLE " + Name(createTableOperation.Name) + " (");
            writer.Indent++;

            createTableOperation.Columns.Each(
                (c, i) =>
                {
                    Generate(c, writer);

                    if (i < createTableOperation.Columns.Count - 1)
                    {
                        writer.WriteLine(",");
                    }
                });

            if (createTableOperation.PrimaryKey != null)
            {
                writer.WriteLine(",");
                writer.Write("CONSTRAINT ");
                writer.Write(Quote(createTableOperation.PrimaryKey.Name));
                writer.Write(" PRIMARY KEY ");

                if (!createTableOperation.PrimaryKey.IsClustered)
                {
                    writer.Write("NONCLUSTERED ");
                }

                writer.Write("(");
                writer.Write(createTableOperation.PrimaryKey.Columns.Join(Quote));
                writer.WriteLine(")");
            }
            else
            {
                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write(")");
        }

        /// <summary>
        /// Override this method to generate SQL when the definition of a table or its attributes are changed.
        /// The default implementation of this method does nothing.
        /// </summary>
        /// <param name="alterTableOperation"> The operation describing changes to the table. </param>
        protected internal virtual void Generate(AlterTableOperation alterTableOperation)
        {
            Check.NotNull(alterTableOperation, "alterTableOperation");

            // Nothing to do since there is no inherent semantics associated with annotations
        }

        /// <summary>
        /// Generates SQL to mark a table as a system table.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createTableOperation"> The table to mark as a system table. </param>
        /// <param name="writer"> The <see cref='IndentedTextWriter' /> to write the generated SQL to. </param>
        protected virtual void GenerateMakeSystemTable(CreateTableOperation createTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(createTableOperation, "createTableOperation");
            Check.NotNull(writer, "writer");

            writer.WriteLine("BEGIN TRY");

            writer.Indent++;
            writer.WriteLine("EXECUTE sp_MS_marksystemobject '" + Escape(createTableOperation.Name) + "'");
            writer.Indent--;

            writer.WriteLine("END TRY");
            writer.WriteLine("BEGIN CATCH");
            writer.Write("END CATCH");
        }

        /// <summary>
        /// Generates SQL to create a database schema.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="schema"> The name of the schema to create. </param>
        protected virtual void GenerateCreateSchema(string schema)
        {
            Check.NotEmpty(schema, "schema");

            using (var writer = Writer())
            {
                writer.Write("IF schema_id('");
                writer.Write(Escape(schema));
                writer.WriteLine("') IS NULL");
                writer.Indent++;
                writer.Write("EXECUTE('CREATE SCHEMA ");
                writer.Write(Escape(Quote(schema)));
                writer.Write("')");

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="AddForeignKeyOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="addForeignKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AddForeignKeyOperation addForeignKeyOperation)
        {
            Check.NotNull(addForeignKeyOperation, "addForeignKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addForeignKeyOperation.DependentTable));
                writer.Write(" ADD CONSTRAINT ");
                writer.Write(Quote(addForeignKeyOperation.Name));
                writer.Write(" FOREIGN KEY (");
                writer.Write(addForeignKeyOperation.DependentColumns.Select(Quote).Join());
                writer.Write(") REFERENCES ");
                writer.Write(Name(addForeignKeyOperation.PrincipalTable));
                writer.Write(" (");
                writer.Write(addForeignKeyOperation.PrincipalColumns.Select(Quote).Join());
                writer.Write(")");

                if (addForeignKeyOperation.CascadeDelete)
                {
                    writer.Write(" ON DELETE CASCADE");
                }

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="DropForeignKeyOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropForeignKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropForeignKeyOperation dropForeignKeyOperation)
        {
            Check.NotNull(dropForeignKeyOperation, "dropForeignKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("IF object_id(N'");

                var schema = DatabaseName.Parse(dropForeignKeyOperation.DependentTable).Schema;
                if (schema != null)
                {
                    writer.Write(Escape(Quote(schema)));
                    writer.Write(".");
                }

                writer.Write(Escape(Quote(dropForeignKeyOperation.Name)));
                writer.WriteLine("', N'F') IS NOT NULL");

                writer.Indent++;
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropForeignKeyOperation.DependentTable));
                writer.Write(" DROP CONSTRAINT ");
                writer.Write(Quote(dropForeignKeyOperation.Name));
                writer.Indent--;

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="CreateIndexOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createIndexOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(CreateIndexOperation createIndexOperation)
        {
            Check.NotNull(createIndexOperation, "createIndexOperation");

            using (var writer = Writer())
            {
                writer.Write("CREATE ");

                if (createIndexOperation.IsUnique)
                {
                    writer.Write("UNIQUE ");
                }

                if (createIndexOperation.IsClustered)
                {
                    writer.Write("CLUSTERED ");
                }

                writer.Write("INDEX ");
                writer.Write(Quote(createIndexOperation.Name));
                writer.Write(" ON ");
                writer.Write(Name(createIndexOperation.Table));
                writer.Write("(");
                writer.Write(createIndexOperation.Columns.Join(Quote));
                writer.Write(")");

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="DropIndexOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropIndexOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropIndexOperation dropIndexOperation)
        {
            Check.NotNull(dropIndexOperation, "dropIndexOperation");

            using (var writer = Writer())
            {
                writer.Write("IF EXISTS (SELECT name FROM sys.indexes WHERE name = N'");
                writer.Write(Escape(dropIndexOperation.Name));
                writer.Write("' AND object_id = object_id(N'");
                writer.Write(Escape(Name(dropIndexOperation.Table)));
                writer.WriteLine("', N'U'))");
                writer.Indent++;
                writer.Write("DROP INDEX ");
                writer.Write(Quote(dropIndexOperation.Name));
                writer.Write(" ON ");
                writer.Write(Name(dropIndexOperation.Table));
                writer.Indent--;

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="AddPrimaryKeyOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="addPrimaryKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AddPrimaryKeyOperation addPrimaryKeyOperation)
        {
            Check.NotNull(addPrimaryKeyOperation, "addPrimaryKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addPrimaryKeyOperation.Table));
                writer.Write(" ADD CONSTRAINT ");
                writer.Write(Quote(addPrimaryKeyOperation.Name));
                writer.Write(" PRIMARY KEY ");

                if (!addPrimaryKeyOperation.IsClustered)
                {
                    writer.Write("NONCLUSTERED ");
                }

                writer.Write("(");
                writer.Write(addPrimaryKeyOperation.Columns.Select(Quote).Join());
                writer.Write(")");

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="DropPrimaryKeyOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropPrimaryKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropPrimaryKeyOperation dropPrimaryKeyOperation)
        {
            Check.NotNull(dropPrimaryKeyOperation, "dropPrimaryKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropPrimaryKeyOperation.Table));
                writer.Write(" DROP CONSTRAINT ");
                writer.Write(Quote(dropPrimaryKeyOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="AddColumnOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="addColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AddColumnOperation addColumnOperation)
        {
            Check.NotNull(addColumnOperation, "addColumnOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addColumnOperation.Table));
                writer.Write(" ADD ");

                var column = addColumnOperation.Column;

                Generate(column, writer);

                if ((column.IsNullable != null)
                    && !column.IsNullable.Value
                    && (column.DefaultValue == null)
                    && (string.IsNullOrWhiteSpace(column.DefaultValueSql))
                    && !column.IsIdentity
                    && !column.IsTimestamp
                    && !column.StoreType.EqualsIgnoreCase("rowversion")
                    && !column.StoreType.EqualsIgnoreCase("timestamp"))
                {
                    writer.Write(" DEFAULT ");

                    if (column.Type == PrimitiveTypeKind.DateTime)
                    {
                        writer.Write(Generate(DateTime.Parse("1900-01-01 00:00:00", CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        writer.Write(Generate((dynamic)column.ClrDefaultValue));
                    }
                }

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="DropColumnOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropColumnOperation dropColumnOperation)
        {
            Check.NotNull(dropColumnOperation, "dropColumnOperation");

            using (var writer = Writer())
            {
                DropDefaultConstraint(dropColumnOperation.Table, dropColumnOperation.Name, writer);

                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropColumnOperation.Table));
                writer.Write(" DROP COLUMN ");
                writer.Write(Quote(dropColumnOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="AlterColumnOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="alterColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AlterColumnOperation alterColumnOperation)
        {
            Check.NotNull(alterColumnOperation, "alterColumnOperation");

            var column = alterColumnOperation.Column;

            using (var writer = Writer())
            {
                DropDefaultConstraint(alterColumnOperation.Table, column.Name, writer);

                writer.Write("ALTER TABLE ");
                writer.Write(Name(alterColumnOperation.Table));
                writer.Write(" ALTER COLUMN ");
                writer.Write(Quote(column.Name));
                writer.Write(" ");
                writer.Write(BuildColumnType(column));

                if ((column.IsNullable != null)
                    && !column.IsNullable.Value)
                {
                    writer.Write(" NOT");
                }

                writer.Write(" NULL");

                if ((column.DefaultValue != null)
                    || !string.IsNullOrWhiteSpace(column.DefaultValueSql))
                {
                    writer.WriteLine();
                    writer.Write("ALTER TABLE ");
                    writer.Write(Name(alterColumnOperation.Table));
                    writer.Write(" ADD CONSTRAINT ");
                    writer.Write(Quote("DF_" + alterColumnOperation.Table + "_" + column.Name));
                    writer.Write(" DEFAULT ");
                    writer.Write(
                        (column.DefaultValue != null)
                            ? Generate((dynamic)column.DefaultValue)
                            : column.DefaultValueSql
                        );
                    writer.Write(" FOR ");
                    writer.Write(Quote(column.Name));
                }

                Statement(writer);
            }
        }

        /// <summary>
        /// Call this method to generate SQL that will attempt to drop the default constraint created
        /// when a column is created. This method is usually called by code that overrides the creation or
        /// altering of columns.
        /// </summary>
        /// <param name="table">The table to which the constraint applies.</param>
        /// <param name="column">The column to which the constraint applies.</param>
        /// <param name="writer">The writer to which generated SQL should be written.</param>
        protected internal virtual void DropDefaultConstraint(string table, string column, IndentedTextWriter writer)
        {
            Check.NotEmpty(table, "table");
            Check.NotEmpty(column, "column");
            Check.NotNull(writer, "writer");

            var variable = "@var" + _variableCounter++;

            writer.Write("DECLARE ");
            writer.Write(variable);
            writer.WriteLine(" nvarchar(128)");
            writer.Write("SELECT ");
            writer.Write(variable);
            writer.WriteLine(" = name");
            writer.WriteLine("FROM sys.default_constraints");
            writer.Write("WHERE parent_object_id = object_id(N'");
            writer.Write(table);
            writer.WriteLine("')");
            writer.Write("AND col_name(parent_object_id, parent_column_id) = '");
            writer.Write(column);
            writer.WriteLine("';");
            writer.Write("IF ");
            writer.Write(variable);
            writer.WriteLine(" IS NOT NULL");
            writer.Indent++;
            writer.Write("EXECUTE('ALTER TABLE ");
            writer.Write(Escape(Name(table)));
            writer.Write(" DROP CONSTRAINT [' + ");
            writer.Write(variable);
            writer.WriteLine(" + ']')");
            writer.Indent--;
        }

        /// <summary>
        /// Generates SQL for a <see cref="DropTableOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropTableOperation dropTableOperation)
        {
            Check.NotNull(dropTableOperation, "dropTableOperation");

            using (var writer = Writer())
            {
                writer.Write("DROP TABLE ");
                writer.Write(Name(dropTableOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="SqlOperation" />.
        /// Generated SQL should be added using the Statement or StatementBatch methods.
        /// </summary>
        /// <param name="sqlOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(SqlOperation sqlOperation)
        {
            Check.NotNull(sqlOperation, "sqlOperation");

            StatementBatch(sqlOperation.Sql, sqlOperation.SuppressTransaction);
        }

        /// <summary>
        /// Generates SQL for a <see cref="RenameColumnOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="renameColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(RenameColumnOperation renameColumnOperation)
        {
            Check.NotNull(renameColumnOperation, "renameColumnOperation");

            using (var writer = Writer())
            {
                writer.Write("EXECUTE sp_rename @objname = N'");
                writer.Write(Escape(renameColumnOperation.Table));
                writer.Write(".");
                writer.Write(Escape(renameColumnOperation.Name));
                writer.Write("', @newname = N'");
                writer.Write(Escape(renameColumnOperation.NewName));
                writer.Write("', @objtype = N'COLUMN'");

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="RenameIndexOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="renameIndexOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(RenameIndexOperation renameIndexOperation)
        {
            Check.NotNull(renameIndexOperation, "renameIndexOperation");

            using (var writer = Writer())
            {
                writer.Write("EXECUTE sp_rename @objname = N'");
                writer.Write(Escape(renameIndexOperation.Table));
                writer.Write(".");
                writer.Write(Escape(renameIndexOperation.Name));
                writer.Write("', @newname = N'");
                writer.Write(Escape(renameIndexOperation.NewName));
                writer.Write("', @objtype = N'INDEX'");

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="RenameTableOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="renameTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(RenameTableOperation renameTableOperation)
        {
            Check.NotNull(renameTableOperation, "renameTableOperation");

            using (var writer = Writer())
            {
                WriteRenameTable(renameTableOperation, writer);

                // rename the PK constraint
                var oldPkName = PrimaryKeyOperation.BuildDefaultName(renameTableOperation.Name);
                var newPkName = PrimaryKeyOperation.BuildDefaultName(((RenameTableOperation)renameTableOperation.Inverse).Name);

                writer.WriteLine();
                writer.Write("IF object_id('");
                writer.Write(Escape(Quote(oldPkName)));
                writer.WriteLine("') IS NOT NULL BEGIN");
                writer.Indent++;
                writer.Write("EXECUTE sp_rename @objname = N'");
                writer.Write(Escape(Quote(oldPkName)));
                writer.Write("', @newname = N'");
                writer.Write(Escape(newPkName));
                writer.WriteLine("', @objtype = N'OBJECT'"); 
                writer.Indent--;
                writer.Write("END");

                Statement(writer);
            }
        }

        private static void WriteRenameTable(RenameTableOperation renameTableOperation, IndentedTextWriter writer)
        {
            writer.Write("EXECUTE sp_rename @objname = N'");
            writer.Write(Escape(renameTableOperation.Name));
            writer.Write("', @newname = N'");
            writer.Write(Escape(renameTableOperation.NewName));
            writer.Write("', @objtype = N'OBJECT'");
        }

        /// <summary>
        /// Generates the specified rename procedure operation.
        /// </summary>
        /// <param name="renameProcedureOperation">The rename procedure operation.</param>
        protected virtual void Generate(RenameProcedureOperation renameProcedureOperation)
        {
            Check.NotNull(renameProcedureOperation, "renameProcedureOperation");

            using (var writer = Writer())
            {
                writer.Write("EXECUTE sp_rename @objname = N'");
                writer.Write(Escape(renameProcedureOperation.Name));
                writer.Write("', @newname = N'");
                writer.Write(Escape(renameProcedureOperation.NewName));
                writer.Write("', @objtype = N'OBJECT'");

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates the specified move procedure operation.
        /// </summary>
        /// <param name="moveProcedureOperation">The move procedure operation.</param>
        protected virtual void Generate(MoveProcedureOperation moveProcedureOperation)
        {
            Check.NotNull(moveProcedureOperation, "moveProcedureOperation");

            var newSchema = moveProcedureOperation.NewSchema ?? "dbo";

            if (!newSchema.EqualsIgnoreCase("dbo")
                && !_generatedSchemas.Contains(newSchema))
            {
                GenerateCreateSchema(newSchema);

                _generatedSchemas.Add(newSchema);
            }

            using (var writer = Writer())
            {
                writer.Write("ALTER SCHEMA ");
                writer.Write(Quote(newSchema));
                writer.Write(" TRANSFER ");
                writer.Write(Name(moveProcedureOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="MoveTableOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="moveTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(MoveTableOperation moveTableOperation)
        {
            Check.NotNull(moveTableOperation, "moveTableOperation");

            var newSchema = moveTableOperation.NewSchema ?? "dbo";

            if (!newSchema.EqualsIgnoreCase("dbo")
                && !_generatedSchemas.Contains(newSchema))
            {
                GenerateCreateSchema(newSchema);

                _generatedSchemas.Add(newSchema);
            }

            if (!moveTableOperation.IsSystem)
            {
                using (var writer = Writer())
                {
                    writer.Write("ALTER SCHEMA ");
                    writer.Write(Quote(newSchema));
                    writer.Write(" TRANSFER ");
                    writer.Write(Name(moveTableOperation.Name));

                    Statement(writer);
                }
            }
            else
            {
                Debug.Assert(moveTableOperation.CreateTableOperation != null);
                Debug.Assert(!string.IsNullOrWhiteSpace(moveTableOperation.ContextKey));

                using (var writer = Writer())
                {
                    writer.Write("IF object_id('");
                    writer.Write(moveTableOperation.CreateTableOperation.Name);
                    writer.WriteLine("') IS NULL BEGIN");
                    writer.Indent++;
                    WriteCreateTable(moveTableOperation.CreateTableOperation, writer);
                    writer.WriteLine();
                    writer.Indent--;
                    writer.WriteLine("END");

                    writer.Write("INSERT INTO ");
                    writer.WriteLine(Name(moveTableOperation.CreateTableOperation.Name));
                    writer.Write("SELECT * FROM ");
                    writer.WriteLine(Name(moveTableOperation.Name));
                    writer.Write("WHERE [ContextKey] = ");
                    writer.WriteLine(Generate(moveTableOperation.ContextKey));

                    writer.Write("DELETE ");
                    writer.WriteLine(Name(moveTableOperation.Name));
                    writer.Write("WHERE [ContextKey] = ");
                    writer.WriteLine(Generate(moveTableOperation.ContextKey));

                    writer.Write("IF NOT EXISTS(SELECT * FROM ");
                    writer.Write(Name(moveTableOperation.Name));
                    writer.WriteLine(")");
                    writer.Indent++;
                    writer.Write("DROP TABLE ");
                    writer.Write(Name(moveTableOperation.Name));
                    writer.Indent--;

                    Statement(writer);
                }
            }
        }

        /// <summary>
        /// Generates SQL for the given column model. This method is called by other methods that
        /// process columns and can be overridden to change the SQL generated.
        /// </summary>
        /// <param name="column">The column for which SQL is being generated.</param>
        /// <param name="writer">The writer to which generated SQL should be written.</param>
        protected internal virtual void Generate(ColumnModel column, IndentedTextWriter writer)
        {
            Check.NotNull(column, "column");
            Check.NotNull(writer, "writer");

            writer.Write(Quote(column.Name));
            writer.Write(" ");
            writer.Write(BuildColumnType(column));

            if ((column.IsNullable != null)
                && !column.IsNullable.Value)
            {
                writer.Write(" NOT NULL");
            }

            if (column.DefaultValue != null)
            {
                writer.Write(" DEFAULT ");
                writer.Write(Generate((dynamic)column.DefaultValue));
            }
            else if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
            {
                writer.Write(" DEFAULT ");
                writer.Write(column.DefaultValueSql);
            }
            else if (column.IsIdentity)
            {
                if ((column.Type == PrimitiveTypeKind.Guid)
                    && (column.DefaultValue == null))
                {
                    writer.Write(" DEFAULT " + GuidColumnDefault);
                }
                else
                {
                    writer.Write(" IDENTITY");
                }
            }
        }

        /// <summary>
        /// Returns the column default value to use for store-generated GUID columns when
        /// no default value is explicitly specified in the migration.
        /// Returns newsequentialid() for on-premises SQL Server 2005 and later.
        /// Returns newid() for SQL Azure.
        /// </summary>
        /// <value>Either newsequentialid() or newid() as described above.</value>
        protected virtual string GuidColumnDefault
        {
            get
            {
                return (_providerManifestToken != SqlProviderManifest.TokenAzure11
                        && _providerManifestToken != "2000")
                           ? "newsequentialid()"
                           : "newid()";
            }
        }

        /// <summary>
        /// Generates SQL for a <see cref="HistoryOperation" />.
        /// Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="historyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(HistoryOperation historyOperation)
        {
            Check.NotNull(historyOperation, "historyOperation");

            using (var writer = Writer())
            {
                historyOperation.CommandTrees.Each(
                    commandTree =>
                    {
                        List<SqlParameter> _;

                        switch (commandTree.CommandTreeKind)
                        {
                            case DbCommandTreeKind.Insert:

                                writer.Write(
                                    DmlSqlGenerator
                                        .GenerateInsertSql(
                                            (DbInsertCommandTree)commandTree,
                                            _sqlGenerator,
                                            out _,
                                            generateReturningSql: false,
                                            upperCaseKeywords: true,
                                            createParameters: false));
                                break;

                            case DbCommandTreeKind.Delete:
                                writer.Write(
                                    DmlSqlGenerator
                                        .GenerateDeleteSql(
                                            (DbDeleteCommandTree)commandTree,
                                            _sqlGenerator,
                                            out _,
                                            upperCaseKeywords: true,
                                            createParameters: false));
                                break;
                        }
                    });

                Statement(writer);
            }
        }

        /// <summary>
        /// Generates SQL to specify a constant byte[] default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(byte[] defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");

            return "0x" + defaultValue.ToHexString();
        }

        /// <summary>
        /// Generates SQL to specify a constant bool default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(bool defaultValue)
        {
            return defaultValue ? "1" : "0";
        }

        /// <summary>
        /// Generates SQL to specify a constant DateTime default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DateTime defaultValue)
        {
            return "'" + defaultValue.ToString(DateTimeFormat, CultureInfo.InvariantCulture) + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant DateTimeOffset default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DateTimeOffset defaultValue)
        {
            return "'" + defaultValue.ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture) + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant Guid default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(Guid defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant string default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(string defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");

            return "'" + defaultValue + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant TimeSpan default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(TimeSpan defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant geogrpahy default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DbGeography defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant geometry default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DbGeometry defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        /// Generates SQL to specify a constant default value being set on a column.
        /// This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(object defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");
            Debug.Assert(defaultValue.GetType().IsValueType());

            return string.Format(CultureInfo.InvariantCulture, "{0}", defaultValue);
        }

        /// <summary>
        /// Generates SQL to specify the data type of a column.
        /// This method just generates the actual type, not the SQL to create the column.
        /// </summary>
        /// <param name="columnModel"> The definition of the column. </param>
        /// <returns> SQL representing the data type. </returns>
        protected virtual string BuildColumnType(ColumnModel columnModel)
        {
            Check.NotNull(columnModel, "columnModel");

            if (columnModel.IsTimestamp)
            {
                return "rowversion";
            }

            return BuildPropertyType(columnModel);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private string BuildPropertyType(PropertyModel propertyModel)
        {
            DebugCheck.NotNull(propertyModel);

            var originalStoreTypeName = propertyModel.StoreType;
            var typeUsage = ProviderManifest.GetStoreType(propertyModel.TypeUsage);

            if (string.IsNullOrWhiteSpace(originalStoreTypeName))
            {
                originalStoreTypeName = typeUsage.EdmType.Name;
            }
            else
            {
                var storeTypeUsage = BuildStoreTypeUsage(originalStoreTypeName, propertyModel);

                typeUsage = storeTypeUsage ?? typeUsage;
            }

            var storeTypeName = originalStoreTypeName;

            const string MaxSuffix = "(max)";

            if (storeTypeName.EndsWith(MaxSuffix, StringComparison.Ordinal))
            {
                storeTypeName = Quote(storeTypeName.Substring(0, storeTypeName.Length - MaxSuffix.Length)) + MaxSuffix;
            }
            else
            {
                storeTypeName = Quote(storeTypeName);
            }

            switch (originalStoreTypeName)
            {
                case "decimal":
                case "numeric":
                    storeTypeName += "(" + (propertyModel.Precision ?? typeUsage.GetPrecision())
                                     + ", " + (propertyModel.Scale ?? typeUsage.GetScale()) + ")";
                    break;
                case "datetime2":
                case "datetimeoffset":
                case "time":
                    storeTypeName += "(" + (propertyModel.Precision ?? typeUsage.GetPrecision()) + ")";
                    break;
                case "binary":
                case "varbinary":
                case "nvarchar":
                case "varchar":
                case "char":
                case "nchar":
                    storeTypeName += "(" + (propertyModel.MaxLength ?? typeUsage.GetMaxLength()) + ")";
                    break;
            }

            return storeTypeName;
        }

        /// <summary>
        /// Generates a quoted name. The supplied name may or may not contain the schema.
        /// </summary>
        /// <param name="name"> The name to be quoted. </param>
        /// <returns> The quoted name. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        protected virtual string Name(string name)
        {
            Check.NotEmpty(name, "name");

            var databaseName = DatabaseName.Parse(name);

            return new[] { databaseName.Schema, databaseName.Name }.Join(Quote, ".");
        }

        /// <summary>
        /// Quotes an identifier for SQL Server.
        /// </summary>
        /// <param name="identifier"> The identifier to be quoted. </param>
        /// <returns> The quoted identifier. </returns>
        protected virtual string Quote(string identifier)
        {
            Check.NotEmpty(identifier, "identifier");

            return SqlGenerator.QuoteIdentifier(identifier);
        }

        private static string Escape(string s)
        {
            DebugCheck.NotNull(s);

            return s.Replace("'", "''");
        }

        private static string Indent(string s, string indentation)
        {
            DebugCheck.NotEmpty(s);
            DebugCheck.NotNull(s);

            return new Regex(@"\r?\n *").Replace(s, Environment.NewLine + indentation);
        }

        /// <summary>
        /// Adds a new Statement to be executed against the database.
        /// </summary>
        /// <param name="sql"> The statement to be executed. </param>
        /// <param name="suppressTransaction"> Gets or sets a value indicating whether this statement should be performed outside of the transaction scope that is used to make the migration process transactional. If set to true, this operation will not be rolled back if the migration process fails. </param>
        /// <param name="batchTerminator">The batch terminator for the database provider.</param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void Statement(string sql, bool suppressTransaction = false, string batchTerminator = null)
        {
            Check.NotEmpty(sql, "sql");

            _statements.Add(
                new MigrationStatement
                    {
                        Sql = sql,
                        SuppressTransaction = suppressTransaction,
                        BatchTerminator = batchTerminator
                    });
        }

        /// <summary>
        /// Gets a new <see cref="IndentedTextWriter" /> that can be used to build SQL.
        /// This is just a helper method to create a writer. Writing to the writer will
        /// not cause SQL to be registered for execution. You must pass the generated
        /// SQL to the Statement method.
        /// </summary>
        /// <returns> An empty text writer to use for SQL generation. </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected static IndentedTextWriter Writer()
        {
            return new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds a new Statement to be executed against the database.
        /// </summary>
        /// <param name="writer"> The writer containing the SQL to be executed. </param>
        /// <param name="batchTerminator">The batch terminator for the database provider.</param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void Statement(IndentedTextWriter writer, string batchTerminator = null)
        {
            Check.NotNull(writer, "writer");

            Statement(writer.InnerWriter.ToString(), batchTerminator: batchTerminator);
        }

        /// <summary>
        /// Breaks sql string into one or more statements, handling T-SQL utility statements as necessary.
        /// </summary>
        /// <param name="sqlBatch"> The SQL to split into one ore more statements to be executed. </param>
        /// <param name="suppressTransaction"> Gets or sets a value indicating whether this statement should be performed outside of the transaction scope that is used to make the migration process transactional. If set to true, this operation will not be rolled back if the migration process fails. </param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void StatementBatch(string sqlBatch, bool suppressTransaction = false)
        {
            Check.NotNull(sqlBatch, "sqlBatch");

            // Handle backslash utility statement (see http://technet.microsoft.com/en-us/library/dd207007.aspx)
            sqlBatch = Regex.Replace(sqlBatch, @"\\(\r\n|\r|\n)", "");

            // Handle batch splitting utility statement (see http://technet.microsoft.com/en-us/library/ms188037.aspx)
            var batches = Regex.Split(sqlBatch,
                String.Format(CultureInfo.InvariantCulture, @"^\s*({0}[ \t]+[0-9]+|{0})(?:\s+|$)", BatchTerminator), 
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            
            for (int i = 0; i < batches.Length; ++i)
            {
                // Skip batches that merely contain the batch terminator
                if (batches[i].StartsWith(BatchTerminator, StringComparison.OrdinalIgnoreCase) || 
                    (i == batches.Length - 1 && string.IsNullOrWhiteSpace(batches[i])))
                {
                    continue;
                }

                // Include batch terminator if the next element is a batch terminator
                if (batches.Length > i + 1 &&
                    batches[i + 1].StartsWith(BatchTerminator, StringComparison.OrdinalIgnoreCase))
                {
                    int repeatCount = 1;

                    // Handle count parameter on the batch splitting utility statement
                    if (! batches[i + 1].EqualsIgnoreCase(BatchTerminator))
                    {
                        repeatCount = int.Parse(Regex.Match(batches[i + 1], @"([0-9]+)").Value, CultureInfo.InvariantCulture);
                    }

                    for (int j = 0; j < repeatCount; ++j)
                        Statement(batches[i], suppressTransaction, BatchTerminator);
                }
                else
                {
                    Statement(batches[i], suppressTransaction);
                }
            }
        }

        private static IEnumerable<MigrationOperation> DetectHistoryRebuild(
            IEnumerable<MigrationOperation> operations)
        {
            DebugCheck.NotNull(operations);

            var enumerator = operations.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var sequence = HistoryRebuildOperationSequence.Detect(enumerator);

                yield return sequence ?? enumerator.Current;
            }
        }

        private void Generate(HistoryRebuildOperationSequence sequence)
        {
            DebugCheck.NotNull(sequence);

            var createTableOperationSource = sequence.DropPrimaryKeyOperation.CreateTableOperation;
            var createTableOperationTarget = ResolveNameConflicts(createTableOperationSource);
            var renameTableOperation = new RenameTableOperation(
                createTableOperationTarget.Name, HistoryContext.DefaultTableName);

            using (var writer = Writer())
            {
                WriteCreateTable(createTableOperationTarget, writer);
                writer.WriteLine();

                // Copy the data from the original table into the new table.
                writer.Write("INSERT INTO ");
                writer.WriteLine(Name(createTableOperationTarget.Name));
                writer.Write("SELECT ");

                var first = true;
                foreach (var column in createTableOperationSource.Columns)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        writer.Write(", ");
                    }

                    writer.Write(
                        (column.Name == sequence.AddColumnOperation.Column.Name)
                            ? Generate((string)sequence.AddColumnOperation.Column.DefaultValue)
                            : (column.Type == PrimitiveTypeKind.String)
                                  ? "LEFT(" + Name(column.Name) + ", " + column.MaxLength + ")"
                                  : Name(column.Name));
                }

                writer.Write(" FROM ");
                writer.WriteLine(Name(createTableOperationSource.Name));

                writer.Write("DROP TABLE ");
                writer.WriteLine(Name(createTableOperationSource.Name));

                WriteRenameTable(renameTableOperation, writer);

                Statement(writer);
            }
        }

        // <summary>
        // Creates a shallow copy of the source CreateTableOperation and the associated
        // AddPrimaryKeyOperation but renames the table and the primary key in order
        // to avoid name conflicts with existing objects.
        // </summary>
        private static CreateTableOperation ResolveNameConflicts(CreateTableOperation source)
        {
            DebugCheck.NotNull(source);

            const string suffix = "2";

            var target = new CreateTableOperation(source.Name + suffix)
                             {
                                 PrimaryKey = new AddPrimaryKeyOperation() 
                                 {
                                     IsClustered = source.PrimaryKey.IsClustered
                                 }
                             };

            Debug.Assert(target.PrimaryKey.Name == source.PrimaryKey.Name + suffix);

            source.Columns.Each(c => target.Columns.Add(c));
            source.PrimaryKey.Columns.Each(c => target.PrimaryKey.Columns.Add(c));

            return target;
        }

        private class HistoryRebuildOperationSequence : MigrationOperation
        {
            public readonly AddColumnOperation AddColumnOperation;
            public readonly DropPrimaryKeyOperation DropPrimaryKeyOperation;

            private HistoryRebuildOperationSequence(
                AddColumnOperation addColumnOperation,
                DropPrimaryKeyOperation dropPrimaryKeyOperation)
                : base(null)
            {
                AddColumnOperation = addColumnOperation;
                DropPrimaryKeyOperation = dropPrimaryKeyOperation;
            }

            public override bool IsDestructiveChange
            {
                get { return false; }
            }

            public static HistoryRebuildOperationSequence Detect(IEnumerator<MigrationOperation> enumerator)
            {
                const string HistoryTableName = "dbo." + HistoryContext.DefaultTableName;

                var addColumnOperation = enumerator.Current as AddColumnOperation;
                if (addColumnOperation == null
                    || addColumnOperation.Table != HistoryTableName
                    || addColumnOperation.Column.Name != "ContextKey")
                {
                    return null;
                }

                Debug.Assert(addColumnOperation.Column.DefaultValue is string);

                enumerator.MoveNext();
                var dropPrimaryKeyOperation = (DropPrimaryKeyOperation)enumerator.Current;
                Debug.Assert(dropPrimaryKeyOperation.Table == HistoryTableName);
                DebugCheck.NotNull(dropPrimaryKeyOperation.CreateTableOperation);

                enumerator.MoveNext();
                var alterColumnOperation = (AlterColumnOperation)enumerator.Current;
                Debug.Assert(alterColumnOperation.Table == HistoryTableName);

                enumerator.MoveNext();
                var addPrimaryKeyOperation = (AddPrimaryKeyOperation)enumerator.Current;
                Debug.Assert(addPrimaryKeyOperation.Table == HistoryTableName);

                return new HistoryRebuildOperationSequence(
                    addColumnOperation, dropPrimaryKeyOperation);
            }
        }
    }
}
