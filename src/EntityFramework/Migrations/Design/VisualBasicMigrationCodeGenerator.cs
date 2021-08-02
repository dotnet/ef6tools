// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Design
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure.Annotations;
    using System.Data.Entity.Migrations.Model;
    using System.Data.Entity.Migrations.Utilities;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.VisualBasic;

    /// <summary>
    /// Generates VB.Net code for a code-based migration.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public class VisualBasicMigrationCodeGenerator : MigrationCodeGenerator
    {
        private IEnumerable<Tuple<CreateTableOperation, AddForeignKeyOperation>> _newTableForeignKeys;
        private IEnumerable<Tuple<CreateTableOperation, CreateIndexOperation>> _newTableIndexes;

        /// <inheritdoc />
        public override ScaffoldedMigration Generate(
            string migrationId,
            IEnumerable<MigrationOperation> operations,
            string sourceModel,
            string targetModel,
            string @namespace,
            string className)
        {
            Check.NotEmpty(migrationId, "migrationId");
            Check.NotNull(operations, "operations");
            Check.NotEmpty(targetModel, "targetModel");
            Check.NotEmpty(className, "className");

            className = ScrubName(className);

            _newTableForeignKeys
                = (from ct in operations.OfType<CreateTableOperation>()
                   from cfk in operations.OfType<AddForeignKeyOperation>()
                   where ct.Name.EqualsIgnoreCase(cfk.DependentTable)
                   select Tuple.Create(ct, cfk)).ToList();

            _newTableIndexes
                = (from ct in operations.OfType<CreateTableOperation>()
                   from cfk in operations.OfType<CreateIndexOperation>()
                   where ct.Name.EqualsIgnoreCase(cfk.Table)
                   select Tuple.Create(ct, cfk)).ToList();

            var generatedMigration
                = new ScaffoldedMigration
                      {
                          MigrationId = migrationId,
                          Language = "vb",
                          UserCode = Generate(operations, @namespace, className),
                          DesignerCode = Generate(migrationId, sourceModel, targetModel, @namespace, className)
                      };

            if (!string.IsNullOrWhiteSpace(sourceModel))
            {
                generatedMigration.Resources.Add("Source", sourceModel);
            }

            generatedMigration.Resources.Add("Target", targetModel);

            return generatedMigration;
        }

        /// <summary>
        /// Generates the primary code file that the user can view and edit.
        /// </summary>
        /// <param name="operations"> Operations to be performed by the migration. </param>
        /// <param name="namespace"> Namespace that code should be generated in. </param>
        /// <param name="className"> Name of the class that should be generated. </param>
        /// <returns> The generated code. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "namespace")]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        protected virtual string Generate(
            IEnumerable<MigrationOperation> operations, string @namespace, string className)
        {
            Check.NotNull(operations, "operations");
            Check.NotEmpty(className, "className");

            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var writer = new IndentedTextWriter(stringWriter))
                {
                    WriteClassStart(
                        @namespace, className, writer, "Inherits DbMigration", designer: false,
                        namespaces: GetNamespaces(operations));

                    writer.WriteLine("Public Overrides Sub Up()");
                    writer.Indent++;

                    operations
                        .Except(_newTableForeignKeys.Select(t => t.Item2))
                        .Except(_newTableIndexes.Select(t => t.Item2))
                        .Each<dynamic>(o => Generate(o, writer));

                    writer.Indent--;
                    writer.WriteLine("End Sub");

                    writer.WriteLine();

                    writer.WriteLine("Public Overrides Sub Down()");
                    writer.Indent++;

                    operations
                        = operations
                            .Select(o => o.Inverse)
                            .Where(o => o != null)
                            .Reverse();

                    var hasUnsupportedOperations
                        = operations.Any(o => o is NotSupportedOperation);

                    operations
                        .Where(o => !(o is NotSupportedOperation))
                        .Each<dynamic>(o => Generate(o, writer));

                    if (hasUnsupportedOperations)
                    {
                        writer.Write("Throw New NotSupportedException(");
                        writer.Write(Generate(Strings.ScaffoldSprocInDownNotSupported));
                        writer.WriteLine(")");
                    }

                    writer.Indent--;
                    writer.WriteLine("End Sub");

                    WriteClassEnd(@namespace, writer);
                }

                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Generates the code behind file with migration metadata.
        /// </summary>
        /// <param name="migrationId"> Unique identifier of the migration. </param>
        /// <param name="sourceModel"> Source model to be stored in the migration metadata. </param>
        /// <param name="targetModel"> Target model to be stored in the migration metadata. </param>
        /// <param name="namespace"> Namespace that code should be generated in. </param>
        /// <param name="className"> Name of the class that should be generated. </param>
        /// <returns> The generated code. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "namespace")]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        protected virtual string Generate(
            string migrationId, string sourceModel, string targetModel, string @namespace, string className)
        {
            Check.NotEmpty(migrationId, "migrationId");
            Check.NotEmpty(targetModel, "targetModel");
            Check.NotEmpty(className, "className");

            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var writer = new IndentedTextWriter(stringWriter))
                {
                    writer.WriteLine("' <auto-generated />");

                    WriteClassStart(@namespace, className, writer, "Implements IMigrationMetadata", designer: true);

                    writer.Write("Private ReadOnly Resources As New ResourceManager(GetType(");
                    writer.Write(className);
                    writer.WriteLine("))");
                    writer.WriteLine();

                    WriteProperty("Id", Quote(migrationId), writer);
                    writer.WriteLine();
                    WriteProperty(
                        "Source",
                        sourceModel == null
                            ? null
                            : "Resources.GetString(\"Source\")",
                        writer);
                    writer.WriteLine();
                    WriteProperty("Target", "Resources.GetString(\"Target\")", writer);

                    WriteClassEnd(@namespace, writer);
                }

                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Generates a property to return the source or target model in the code behind file.
        /// </summary>
        /// <param name="name"> Name of the property. </param>
        /// <param name="value"> Value to be returned. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void WriteProperty(string name, string value, IndentedTextWriter writer)
        {
            Check.NotEmpty(name, "name");
            Check.NotNull(writer, "writer");

            writer.Write("Private ReadOnly Property IMigrationMetadata_");
            writer.Write(name);
            writer.Write("() As String Implements IMigrationMetadata.");
            writer.WriteLine(name);
            writer.Indent++;
            writer.WriteLine("Get");
            writer.Indent++;
            writer.Write("Return ");
            writer.WriteLine(value ?? "Nothing");
            writer.Indent--;
            writer.WriteLine("End Get");
            writer.Indent--;
            writer.WriteLine("End Property");
        }

        /// <summary>
        /// Generates class attributes.
        /// </summary>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        /// <param name="designer"> A value indicating if this class is being generated for a code-behind file. </param>
        protected virtual void WriteClassAttributes(IndentedTextWriter writer, bool designer)
        {
            if (designer)
            {
                writer.WriteLine(
                    "<GeneratedCode(\"EntityFramework.Migrations\", \"{0}\")>",
                    typeof(VisualBasicMigrationCodeGenerator).Assembly().GetInformationalVersion());
            }
        }

        /// <summary>
        /// Generates a namespace, using statements and class definition.
        /// </summary>
        /// <param name="namespace"> Namespace that code should be generated in. </param>
        /// <param name="className"> Name of the class that should be generated. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        /// <param name="base"> Base class for the generated class. </param>
        /// <param name="designer"> A value indicating if this class is being generated for a code-behind file. </param>
        /// <param name="namespaces"> Namespaces for which Imports directives will be added. If null, then the namespaces returned from GetDefaultNamespaces will be used. </param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "namespace")]
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "base")]
        protected virtual void WriteClassStart(
            string @namespace, string className, IndentedTextWriter writer, string @base, bool designer = false,
            IEnumerable<string> namespaces = null)
        {
            Check.NotNull(writer, "writer");
            Check.NotEmpty(className, "className");
            Check.NotEmpty(@base, "base");

            (namespaces ?? GetDefaultNamespaces(designer)).Each(n => writer.WriteLine("Imports " + n));

            if (!designer)
            {
                writer.WriteLine("Imports Microsoft.VisualBasic");
            }

            writer.WriteLine();

            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                writer.Write("Namespace ");
                writer.WriteLine(@namespace);
                writer.Indent++;
            }

            WriteClassAttributes(writer, designer);

            writer.Write("Public ");

            if (designer)
            {
                writer.Write("NotInheritable ");
            }

            writer.Write("Partial Class ");
            writer.Write(className);

            writer.WriteLine();
            writer.Indent++;
            writer.WriteLine(@base);
            writer.Indent--;

            writer.WriteLine();
            writer.Indent++;
        }

        /// <summary>
        /// Generates the closing code for a class that was started with WriteClassStart.
        /// </summary>
        /// <param name="namespace"> Namespace that code should be generated in. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "namespace")]
        protected virtual void WriteClassEnd(string @namespace, IndentedTextWriter writer)
        {
            Check.NotNull(writer, "writer");

            writer.Indent--;
            writer.WriteLine("End Class");

            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                writer.Indent--;
                writer.WriteLine("End Namespace");
            }
        }

        /// <summary>
        /// Generates code to perform an <see cref="AddColumnOperation" />.
        /// </summary>
        /// <param name="addColumnOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(AddColumnOperation addColumnOperation, IndentedTextWriter writer)
        {
            Check.NotNull(addColumnOperation, "addColumnOperation");
            Check.NotNull(writer, "writer");

            writer.Write("AddColumn(");
            writer.Write(Quote(addColumnOperation.Table));
            writer.Write(", ");
            writer.Write(Quote(addColumnOperation.Column.Name));
            writer.Write(", Function(c)");
            Generate(addColumnOperation.Column, writer);
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="DropColumnOperation" />.
        /// </summary>
        /// <param name="dropColumnOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(DropColumnOperation dropColumnOperation, IndentedTextWriter writer)
        {
            Check.NotNull(dropColumnOperation, "dropColumnOperation");
            Check.NotNull(writer, "writer");

            writer.Write("DropColumn(");
            writer.Write(Quote(dropColumnOperation.Table));
            writer.Write(", ");
            writer.Write(Quote(dropColumnOperation.Name));

            if (dropColumnOperation.RemovedAnnotations.Any())
            {
                writer.Indent++;

                writer.WriteLine(",");
                writer.Write("removedAnnotations := ");
                GenerateAnnotations(dropColumnOperation.RemovedAnnotations, writer);

                writer.Indent--;
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform an <see cref="AlterColumnOperation" />.
        /// </summary>
        /// <param name="alterColumnOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(AlterColumnOperation alterColumnOperation, IndentedTextWriter writer)
        {
            Check.NotNull(alterColumnOperation, "alterColumnOperation");
            Check.NotNull(writer, "writer");

            writer.Write("AlterColumn(");
            writer.Write(Quote(alterColumnOperation.Table));
            writer.Write(", ");
            writer.Write(Quote(alterColumnOperation.Column.Name));
            writer.Write(", Function(c)");
            Generate(alterColumnOperation.Column, writer);
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code for to re-create the given dictionary of annotations for use when passing
        /// these annotations as a parameter of a <see cref="DbMigration"/>. call.
        /// </summary>
        /// <param name="annotations">The annotations to generate.</param>
        /// <param name="writer">The writer to which generated code should be written.</param>
        protected internal virtual void GenerateAnnotations(IDictionary<string, object> annotations, IndentedTextWriter writer)
        {
            Check.NotNull(annotations, "annotations");
            Check.NotNull(writer, "writer");

            writer.WriteLine("New Dictionary(Of String, Object)() From _");
            writer.WriteLine("{");
            writer.Indent++;

            var names = annotations.Keys.OrderBy(k => k).ToArray();
            for (var i = 0; i < names.Length; i++)
            {
                writer.Write("{ ");
                writer.Write(Quote(names[i]) + ", ");
                GenerateAnnotation(names[i], annotations[names[i]], writer);
                writer.WriteLine(i < names.Length - 1 ? " }," : " }");
            }

            writer.Indent--;
            writer.Write("}");
        }

        /// <summary>
        /// Generates code for to re-create the given dictionary of annotations for use when passing
        /// these annotations as a parameter of a <see cref="DbMigration"/>. call.
        /// </summary>
        /// <param name="annotations">The annotations to generate.</param>
        /// <param name="writer">The writer to which generated code should be written.</param>
        protected internal virtual void GenerateAnnotations(IDictionary<string, AnnotationValues> annotations, IndentedTextWriter writer)
        {
            Check.NotNull(annotations, "annotations");
            Check.NotNull(writer, "writer");

            writer.WriteLine("New Dictionary(Of String, AnnotationValues)() From _");
            writer.WriteLine("{");
            writer.Indent++;

            if (annotations != null)
            {
                var names = annotations.Keys.OrderBy(k => k).ToArray();
                for (var i = 0; i < names.Length; i++)
                {
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine(Quote(names[i]) + ",");
                    writer.Write("New AnnotationValues(oldValue := ");
                    GenerateAnnotation(names[i], annotations[names[i]].OldValue, writer);
                    writer.Write(", newValue := ");
                    GenerateAnnotation(names[i], annotations[names[i]].NewValue, writer);
                    writer.WriteLine(")");
                    writer.Indent--;
                    writer.WriteLine(i < names.Length - 1 ? " }," : " }");
                }
            }

            writer.Indent--;
            writer.Write("}");
        }

        /// <summary>
        /// Generates code for the given annotation value, which may be null. The default behavior is to use an
        /// <see cref="AnnotationCodeGenerator"/> if one is registered, otherwise call ToString on the annotation value.
        /// </summary>
        /// <remarks>
        /// Note that a <see cref="AnnotationCodeGenerator"/> can be registered to generate code for custom annotations
        /// without the need to override the entire code generator.
        /// </remarks>
        /// <param name="name">The name of the annotation for which code is needed.</param>
        /// <param name="annotation">The annotation value to generate.</param>
        /// <param name="writer">The writer to which generated code should be written.</param>
        protected internal virtual void GenerateAnnotation(string name, object annotation, IndentedTextWriter writer)
        {
            Check.NotEmpty(name, "name");
            Check.NotNull(writer, "writer");

            if (annotation == null)
            {
                writer.Write("Nothing");
                return;
            }

            Func<AnnotationCodeGenerator> annotationGenerator;
            if (AnnotationGenerators.TryGetValue(name, out annotationGenerator)
                && annotationGenerator != null)
            {
                annotationGenerator().Generate(name, annotation, writer);
            }
            else
            {
                writer.Write(Quote(annotation.ToString()));
            }
        }

        /// <summary>Generates code to perform a <see cref="T:System.Data.Entity.Migrations.Model.CreateProcedureOperation" />.</summary>
        /// <param name="createProcedureOperation">The operation to generate code for.</param>
        /// <param name="writer">Text writer to add the generated code to.</param>
        protected virtual void Generate(CreateProcedureOperation createProcedureOperation, IndentedTextWriter writer)
        {
            Check.NotNull(createProcedureOperation, "createProcedureOperation");
            Check.NotNull(writer, "writer");

            Generate(createProcedureOperation, "CreateStoredProcedure", writer);
        }

        /// <summary>Generates code to perform a <see cref="T:System.Data.Entity.Migrations.Model.AlterProcedureOperation" />.</summary>
        /// <param name="alterProcedureOperation">The operation to generate code for.</param>
        /// <param name="writer">Text writer to add the generated code to.</param>
        protected virtual void Generate(AlterProcedureOperation alterProcedureOperation, IndentedTextWriter writer)
        {
            Check.NotNull(alterProcedureOperation, "alterProcedureOperation");
            Check.NotNull(writer, "writer");

            Generate(alterProcedureOperation, "AlterStoredProcedure", writer);
        }

        private void Generate(ProcedureOperation procedureOperation, string methodName, IndentedTextWriter writer)
        {
            DebugCheck.NotNull(procedureOperation);
            DebugCheck.NotEmpty(methodName);
            DebugCheck.NotNull(writer);

            writer.Write(methodName);
            writer.WriteLine("(");
            writer.Indent++;
            writer.Write(Quote(procedureOperation.Name));
            writer.WriteLine(",");

            if (procedureOperation.Parameters.Any())
            {
                writer.WriteLine("Function(p) New With");
                writer.Indent++;
                writer.WriteLine("{");
                writer.Indent++;

                procedureOperation.Parameters.Each(
                    (p, i) =>
                    {
                        var scrubbedName = ScrubName(p.Name);

                        writer.Write(".");
                        writer.Write(scrubbedName);
                        writer.Write(" =");
                        Generate(p, writer, !string.Equals(p.Name, scrubbedName, StringComparison.Ordinal));

                        if (i < procedureOperation.Parameters.Count - 1)
                        {
                            writer.Write(",");
                        }

                        writer.WriteLine();
                    });

                writer.Indent--;
                writer.WriteLine("},");
                writer.Indent--;
            }

            writer.Write("body :=");

            if (!string.IsNullOrWhiteSpace(procedureOperation.BodySql))
            {
                writer.WriteLine();
                writer.Indent++;

                var indentString
                    = "\" & vbCrLf & _"
                      + writer.NewLine
                      + writer.CurrentIndentation()
                      + "\"";

                writer.WriteLine(
                    Generate(
                        procedureOperation
                            .BodySql
                            .Replace(Environment.NewLine, indentString)));

                writer.Indent--;
            }
            else
            {
                writer.WriteLine(" \"\"");
            }

            writer.Indent--;
            writer.WriteLine(")");
            writer.WriteLine();
        }

        /// <summary>Generates code to perform a <see cref="T:System.Data.Entity.Migrations.Model.ParameterModel" />.</summary>
        /// <param name="parameterModel">The parameter model definition to generate code for.</param>
        /// <param name="writer">Text writer to add the generated code to.</param>
        /// <param name="emitName">true to include the column name in the definition; otherwise, false.</param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        protected virtual void Generate(ParameterModel parameterModel, IndentedTextWriter writer, bool emitName = false)
        {
            Check.NotNull(parameterModel, "parameterModel");
            Check.NotNull(writer, "writer");

            writer.Write(" p.");
            writer.Write(TranslateColumnType(parameterModel.Type));
            writer.Write("(");

            var args = new List<string>();

            if (emitName)
            {
                args.Add("name := " + Quote(parameterModel.Name));
            }

            if (parameterModel.MaxLength != null)
            {
                args.Add("maxLength := " + parameterModel.MaxLength);
            }

            if (parameterModel.Precision != null)
            {
                args.Add("precision := " + parameterModel.Precision);
            }

            if (parameterModel.Scale != null)
            {
                args.Add("scale := " + parameterModel.Scale);
            }

            if (parameterModel.IsFixedLength != null)
            {
                args.Add("fixedLength := " + parameterModel.IsFixedLength.ToString().ToLowerInvariant());
            }

            if (parameterModel.IsUnicode != null)
            {
                args.Add("unicode := " + parameterModel.IsUnicode.ToString().ToLowerInvariant());
            }

            if (parameterModel.DefaultValue != null)
            {
                args.Add("defaultValue := " + Generate((dynamic)parameterModel.DefaultValue));
            }

            if (!string.IsNullOrWhiteSpace(parameterModel.DefaultValueSql))
            {
                args.Add("defaultValueSql := " + Quote(parameterModel.DefaultValueSql));
            }

            if (!string.IsNullOrWhiteSpace(parameterModel.StoreType))
            {
                args.Add("storeType := " + Quote(parameterModel.StoreType));
            }

            if (parameterModel.IsOutParameter)
            {
                args.Add("outParameter := True");
            }

            writer.Write(args.Join());
            writer.Write(")");
        }

        /// <summary>Generates code to perform a <see cref="T:System.Data.Entity.Migrations.Model.DropProcedureOperation" />.</summary>
        /// <param name="dropProcedureOperation">The operation to generate code for.</param>
        /// <param name="writer">Text writer to add the generated code to.</param>
        protected virtual void Generate(DropProcedureOperation dropProcedureOperation, IndentedTextWriter writer)
        {
            Check.NotNull(dropProcedureOperation, "dropProcedureOperation");
            Check.NotNull(writer, "writer");

            writer.Write("DropStoredProcedure(");
            writer.Write(Quote(dropProcedureOperation.Name));
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="CreateTableOperation" />.
        /// </summary>
        /// <param name="createTableOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(CreateTableOperation createTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(createTableOperation, "createTableOperation");
            Check.NotNull(writer, "writer");

            writer.WriteLine("CreateTable(");
            writer.Indent++;
            writer.Write(Quote(createTableOperation.Name));
            writer.WriteLine(",");
            writer.WriteLine("Function(c) New With");
            writer.Indent++;
            writer.WriteLine("{");
            writer.Indent++;

            var columnCount = createTableOperation.Columns.Count();

            createTableOperation.Columns.Each(
                (c, i) =>
                {
                    var scrubbedName = ScrubName(c.Name);

                    writer.Write(".");
                    writer.Write(scrubbedName);
                    writer.Write(" =");
                    Generate(c, writer, !string.Equals(c.Name, scrubbedName, StringComparison.Ordinal));

                    if (i < columnCount - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                });

            writer.Indent--;
            writer.Write("}");
            writer.Indent--;

            if (createTableOperation.Annotations.Any())
            {
                writer.WriteLine(",");
                writer.Write("annotations := ");
                GenerateAnnotations(createTableOperation.Annotations, writer);
            }

            writer.Write(")");

            GenerateInline(createTableOperation.PrimaryKey, writer);

            _newTableForeignKeys
                .Where(t => t.Item1 == createTableOperation)
                .Each(t => GenerateInline(t.Item2, writer));

            _newTableIndexes
                .Where(t => t.Item1 == createTableOperation)
                .Each(t => GenerateInline(t.Item2, writer));

            writer.WriteLine();
            writer.Indent--;
            writer.WriteLine();
        }

        /// <summary>
        /// Generates code for an <see cref="AlterTableOperation"/>.
        /// </summary>
        /// <param name="alterTableOperation">The operation for which code should be generated.</param>
        /// <param name="writer">The writer to which generated code should be written.</param>
        protected internal virtual void Generate(AlterTableOperation alterTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(alterTableOperation, "alterTableOperation");
            Check.NotNull(writer, "writer");

            writer.WriteLine("AlterTableAnnotations(");
            writer.Indent++;
            writer.Write(Quote(alterTableOperation.Name));
            writer.WriteLine(",");
            writer.WriteLine("Function(c) New With");
            writer.Indent++;
            writer.WriteLine("{");
            writer.Indent++;

            var columnCount = alterTableOperation.Columns.Count();

            alterTableOperation.Columns.Each(
                (c, i) =>
                {
                    var scrubbedName = ScrubName(c.Name);

                    writer.Write(".");
                    writer.Write(scrubbedName);
                    writer.Write(" =");
                    Generate(c, writer, !string.Equals(c.Name, scrubbedName, StringComparison.Ordinal));

                    if (i < columnCount - 1)
                    {
                        writer.Write(",");
                    }

                    writer.WriteLine();
                });

            writer.Indent--;
            writer.Write("}");
            writer.Indent--;

            if (alterTableOperation.Annotations.Any())
            {
                writer.WriteLine(",");
                writer.Write("annotations := ");
                GenerateAnnotations(alterTableOperation.Annotations, writer);
            }

            writer.Write(")");

            writer.WriteLine();
            writer.Indent--;
            writer.WriteLine();
        }
        
        /// <summary>
        /// Generates code to perform an <see cref="AddPrimaryKeyOperation" /> as part of a <see cref="CreateTableOperation" />.
        /// </summary>
        /// <param name="addPrimaryKeyOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void GenerateInline(AddPrimaryKeyOperation addPrimaryKeyOperation, IndentedTextWriter writer)
        {
            Check.NotNull(writer, "writer");

            if (addPrimaryKeyOperation != null)
            {
                writer.WriteLine(" _");
                writer.Write(".PrimaryKey(");

                Generate(addPrimaryKeyOperation.Columns, writer);

                if (!addPrimaryKeyOperation.HasDefaultName)
                {
                    writer.Write(", name := ");
                    writer.Write(Quote(addPrimaryKeyOperation.Name));
                }

                if (!addPrimaryKeyOperation.IsClustered)
                {
                    writer.Write(", clustered := False");
                }

                writer.Write(")");
            }
        }

        /// <summary>
        /// Generates code to perform an <see cref="AddForeignKeyOperation" /> as part of a <see cref="CreateTableOperation" />.
        /// </summary>
        /// <param name="addForeignKeyOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void GenerateInline(AddForeignKeyOperation addForeignKeyOperation, IndentedTextWriter writer)
        {
            Check.NotNull(addForeignKeyOperation, "addForeignKeyOperation");
            Check.NotNull(writer, "writer");

            writer.WriteLine(" _");
            writer.Write(".ForeignKey(" + Quote(addForeignKeyOperation.PrincipalTable) + ", ");
            Generate(addForeignKeyOperation.DependentColumns, writer);

            if (addForeignKeyOperation.CascadeDelete)
            {
                writer.Write(", cascadeDelete := True");
            }

            writer.Write(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="CreateIndexOperation" /> as part of a <see cref="CreateTableOperation" />.
        /// </summary>
        /// <param name="createIndexOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void GenerateInline(CreateIndexOperation createIndexOperation, IndentedTextWriter writer)
        {
            Check.NotNull(createIndexOperation, "createIndexOperation");
            Check.NotNull(writer, "writer");

            writer.WriteLine(" _");
            writer.Write(".Index(");
            Generate(createIndexOperation.Columns, writer);
            WriteIndexParameters(createIndexOperation, writer);
            writer.Write(")");
        }

        /// <summary>
        /// Generates code to specify a set of column names using a lambda expression.
        /// </summary>
        /// <param name="columns"> The columns to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(IEnumerable<string> columns, IndentedTextWriter writer)
        {
            Check.NotNull(columns, "columns");
            Check.NotNull(writer, "writer");

            writer.Write("Function(t) ");

            if (columns.Count() == 1)
            {
                writer.Write("t." + ScrubName(columns.Single()));
            }
            else
            {
                writer.Write("New With { " + columns.Join(c => "t." + ScrubName(c)) + " }");
            }
        }

        /// <summary>
        /// Generates code to perform an <see cref="AddForeignKeyOperation" />.
        /// </summary>
        /// <param name="addForeignKeyOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(AddForeignKeyOperation addForeignKeyOperation, IndentedTextWriter writer)
        {
            Check.NotNull(addForeignKeyOperation, "addForeignKeyOperation");
            Check.NotNull(writer, "writer");

            writer.Write("AddForeignKey(");
            writer.Write(Quote(addForeignKeyOperation.DependentTable));
            writer.Write(", ");

            var compositeKey = addForeignKeyOperation.DependentColumns.Count() > 1;

            if (compositeKey)
            {
                writer.Write("New String() { ");
            }

            writer.Write(addForeignKeyOperation.DependentColumns.Join(Quote));

            if (compositeKey)
            {
                writer.Write(" }");
            }

            writer.Write(", ");
            writer.Write(Quote(addForeignKeyOperation.PrincipalTable));

            if (addForeignKeyOperation.PrincipalColumns.Any())
            {
                writer.Write(", ");

                if (compositeKey)
                {
                    writer.Write("New String() { ");
                }

                writer.Write(addForeignKeyOperation.PrincipalColumns.Join(Quote));

                if (compositeKey)
                {
                    writer.Write(" }");
                }
            }

            if (addForeignKeyOperation.CascadeDelete)
            {
                writer.Write(", cascadeDelete := True");
            }

            if (!addForeignKeyOperation.HasDefaultName)
            {
                writer.Write(", name := ");
                writer.Write(Quote(addForeignKeyOperation.Name));
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="DropForeignKeyOperation" />.
        /// </summary>
        /// <param name="dropForeignKeyOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(DropForeignKeyOperation dropForeignKeyOperation, IndentedTextWriter writer)
        {
            Check.NotNull(dropForeignKeyOperation, "dropForeignKeyOperation");
            Check.NotNull(writer, "writer");

            writer.Write("DropForeignKey(");
            writer.Write(Quote(dropForeignKeyOperation.DependentTable));
            writer.Write(", ");

            if (!dropForeignKeyOperation.HasDefaultName)
            {
                writer.Write(Quote(dropForeignKeyOperation.Name));
            }
            else
            {
                var compositeKey = dropForeignKeyOperation.DependentColumns.Count() > 1;

                if (compositeKey)
                {
                    writer.Write("New String() { ");
                }

                writer.Write(dropForeignKeyOperation.DependentColumns.Join(Quote));

                if (compositeKey)
                {
                    writer.Write(" }");
                }

                writer.Write(", ");
                writer.Write(Quote(dropForeignKeyOperation.PrincipalTable));
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform an <see cref="AddPrimaryKeyOperation" />.
        /// </summary>
        /// <param name="addPrimaryKeyOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(AddPrimaryKeyOperation addPrimaryKeyOperation, IndentedTextWriter writer)
        {
            Check.NotNull(addPrimaryKeyOperation, "addPrimaryKeyOperation");
            Check.NotNull(writer, "writer");

            writer.Write("AddPrimaryKey(");
            writer.Write(Quote(addPrimaryKeyOperation.Table));
            writer.Write(", ");

            var compositeIndex = addPrimaryKeyOperation.Columns.Count() > 1;

            if (compositeIndex)
            {
                writer.Write("New String() { ");
            }

            writer.Write(addPrimaryKeyOperation.Columns.Join(Quote));

            if (compositeIndex)
            {
                writer.Write(" }");
            }

            if (!addPrimaryKeyOperation.HasDefaultName)
            {
                writer.Write(", name := ");
                writer.Write(Quote(addPrimaryKeyOperation.Name));
            }

            if (!addPrimaryKeyOperation.IsClustered)
            {
                writer.Write(", clustered := False");
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="DropPrimaryKeyOperation" />.
        /// </summary>
        /// <param name="dropPrimaryKeyOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(DropPrimaryKeyOperation dropPrimaryKeyOperation, IndentedTextWriter writer)
        {
            Check.NotNull(dropPrimaryKeyOperation, "dropPrimaryKeyOperation");
            Check.NotNull(writer, "writer");

            writer.Write("DropPrimaryKey(");
            writer.Write(Quote(dropPrimaryKeyOperation.Table));

            if (!dropPrimaryKeyOperation.HasDefaultName)
            {
                writer.Write(", name := ");
                writer.Write(Quote(dropPrimaryKeyOperation.Name));
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="CreateIndexOperation" />.
        /// </summary>
        /// <param name="createIndexOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(CreateIndexOperation createIndexOperation, IndentedTextWriter writer)
        {
            Check.NotNull(createIndexOperation, "createIndexOperation");
            Check.NotNull(writer, "writer");

            writer.Write("CreateIndex(");
            writer.Write(Quote(createIndexOperation.Table));
            writer.Write(", ");

            var compositeIndex = createIndexOperation.Columns.Count() > 1;

            if (compositeIndex)
            {
                writer.Write("New String() { ");
            }

            writer.Write(createIndexOperation.Columns.Join(Quote));

            if (compositeIndex)
            {
                writer.Write(" }");
            }

            WriteIndexParameters(createIndexOperation, writer);

            writer.WriteLine(")");
        }

        private void WriteIndexParameters(CreateIndexOperation createIndexOperation, IndentedTextWriter writer)
        {
            if (createIndexOperation.IsUnique)
            {
                writer.Write(", unique := True");
            }

            if (createIndexOperation.IsClustered)
            {
                writer.Write(", clustered := True");
            }

            if (!createIndexOperation.HasDefaultName)
            {
                writer.Write(", name := ");
                writer.Write(Quote(createIndexOperation.Name));
            }
        }
        
        /// <summary>
        /// Generates code to perform a <see cref="DropIndexOperation" />.
        /// </summary>
        /// <param name="dropIndexOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(DropIndexOperation dropIndexOperation, IndentedTextWriter writer)
        {
            Check.NotNull(dropIndexOperation, "dropIndexOperation");
            Check.NotNull(writer, "writer");

            writer.Write("DropIndex(");
            writer.Write(Quote(dropIndexOperation.Table));
            writer.Write(", ");

            if (!dropIndexOperation.HasDefaultName)
            {
                writer.Write(Quote(dropIndexOperation.Name));
            }
            else
            {
                writer.Write("New String() { ");
                writer.Write(dropIndexOperation.Columns.Join(Quote));
                writer.Write(" }");
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to specify the definition for a <see cref="ColumnModel" />.
        /// </summary>
        /// <param name="column"> The column definition to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        /// <param name="emitName"> A value indicating whether to include the column name in the definition. </param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        protected virtual void Generate(ColumnModel column, IndentedTextWriter writer, bool emitName = false)
        {
            Check.NotNull(column, "column");
            Check.NotNull(writer, "writer");

            writer.Write(" c.");
            writer.Write(TranslateColumnType(column.Type));
            writer.Write("(");

            var args = new List<string>();

            if (emitName)
            {
                args.Add("name := " + Quote(column.Name));
            }

            if (column.IsNullable == false)
            {
                args.Add("nullable := False");
            }

            if (column.MaxLength != null)
            {
                args.Add("maxLength := " + column.MaxLength);
            }

            if (column.Precision != null)
            {
                args.Add("precision := " + column.Precision);
            }

            if (column.Scale != null)
            {
                args.Add("scale := " + column.Scale);
            }

            if (column.IsFixedLength != null)
            {
                args.Add("fixedLength := " + column.IsFixedLength.ToString().ToLowerInvariant());
            }

            if (column.IsUnicode != null)
            {
                args.Add("unicode := " + column.IsUnicode.ToString().ToLowerInvariant());
            }

            if (column.IsIdentity)
            {
                args.Add("identity := True");
            }

            if (column.DefaultValue != null)
            {
                args.Add("defaultValue := " + Generate((dynamic)column.DefaultValue));
            }

            if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
            {
                args.Add("defaultValueSql := " + Quote(column.DefaultValueSql));
            }

            if (column.IsTimestamp)
            {
                args.Add("timestamp := True");
            }

            if (!string.IsNullOrWhiteSpace(column.StoreType))
            {
                args.Add("storeType := " + Quote(column.StoreType));
            }

            writer.Write(args.Join());

            if (column.Annotations.Any())
            {
                writer.Indent++;

                writer.WriteLine(args.Any() ? "," : "");
                writer.Write("annotations := ");
                GenerateAnnotations(column.Annotations, writer);

                writer.Indent--;
            }
            
            writer.Write(")");
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="T:byte[]" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(byte[] defaultValue)
        {
            return "New Byte() {" + defaultValue.Join() + "}";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="DateTime" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(DateTime defaultValue)
        {
            return "New DateTime(" + defaultValue.Ticks + ", DateTimeKind."
                   + Enum.GetName(typeof(DateTimeKind), defaultValue.Kind) + ")";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="DateTimeOffset" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(DateTimeOffset defaultValue)
        {
            return "New DateTimeOffset(" + defaultValue.Ticks + ", new TimeSpan("
                   + defaultValue.Offset.Ticks + "))";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="decimal" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(decimal defaultValue)
        {
            return defaultValue.ToString(CultureInfo.InvariantCulture) + "D";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="Guid" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(Guid defaultValue)
        {
            return "New Guid(\"" + defaultValue + "\")";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="long" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(long defaultValue)
        {
            return defaultValue.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="float" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(float defaultValue)
        {
            return defaultValue.ToString(CultureInfo.InvariantCulture) + "F";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="string" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(string defaultValue)
        {
            return Quote(defaultValue);
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="TimeSpan" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(TimeSpan defaultValue)
        {
            return "New TimeSpan(" + defaultValue.Ticks + ")";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="DbGeography" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(DbGeography defaultValue)
        {
            return "DbGeography.FromText(\"" + defaultValue.AsText() + "\", " + defaultValue.CoordinateSystemId + ")";
        }

        /// <summary>
        /// Generates code to specify the default value for a <see cref="DbGeometry" /> column.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        protected virtual string Generate(DbGeometry defaultValue)
        {
            return "DbGeometry.FromText(\"" + defaultValue.AsText() + "\", " + defaultValue.CoordinateSystemId + ")";
        }

        /// <summary>
        /// Generates code to specify the default value for a column of unknown data type.
        /// </summary>
        /// <param name="defaultValue"> The value to be used as the default. </param>
        /// <returns> Code representing the default value. </returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        protected virtual string Generate(object defaultValue)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", defaultValue).ToLowerInvariant();
        }

        /// <summary>
        /// Generates code to perform a <see cref="DropTableOperation" />.
        /// </summary>
        /// <param name="dropTableOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(DropTableOperation dropTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(dropTableOperation, "dropTableOperation");
            Check.NotNull(writer, "writer");

            writer.Write("DropTable(");
            writer.Write(Quote(dropTableOperation.Name));

            if (dropTableOperation.RemovedAnnotations.Any())
            {
                writer.Indent++;

                writer.WriteLine(",");
                writer.Write("removedAnnotations := ");
                GenerateAnnotations(dropTableOperation.RemovedAnnotations, writer);
                writer.Indent--;
            }

            var columns = dropTableOperation.RemovedColumnAnnotations;
            if (columns.Any())
            {
                writer.Indent++;

                writer.WriteLine(",");
                writer.Write("removedColumnAnnotations := ");

                writer.WriteLine("New Dictionary(Of String, IDictionary(Of String, Object)) From _");
                writer.WriteLine("{");
                writer.Indent++;

                var columnNames = columns.Keys.OrderBy(k => k).ToArray();
                for (var i = 0; i < columnNames.Length; i++)
                {
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine(Quote(columnNames[i]) + ",");
                    GenerateAnnotations(columns[columnNames[i]], writer);
                    writer.WriteLine();
                    writer.Indent--;
                    writer.WriteLine(i < columnNames.Length - 1 ? " }," : " }");
                }

                writer.Indent--;
                writer.Write("}");
                writer.Indent--;
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="MoveTableOperation" />.
        /// </summary>
        /// <param name="moveTableOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(MoveTableOperation moveTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(moveTableOperation, "moveTableOperation");
            Check.NotNull(writer, "writer");

            writer.Write("MoveTable(name := ");
            writer.Write(Quote(moveTableOperation.Name));
            writer.Write(", newSchema := ");
            writer.Write(
                string.IsNullOrWhiteSpace(moveTableOperation.NewSchema)
                    ? "Nothing"
                    : Quote(moveTableOperation.NewSchema));
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="MoveProcedureOperation" />.
        /// </summary>
        /// <param name="moveProcedureOperation">The operation to generate code for.</param>
        /// <param name="writer">Text writer to add the generated code to.</param>
        protected virtual void Generate(MoveProcedureOperation moveProcedureOperation, IndentedTextWriter writer)
        {
            Check.NotNull(moveProcedureOperation, "moveProcedureOperation");
            Check.NotNull(writer, "writer");

            writer.Write("MoveStoredProcedure(name := ");
            writer.Write(Quote(moveProcedureOperation.Name));
            writer.Write(", newSchema := ");
            writer.Write(
                string.IsNullOrWhiteSpace(moveProcedureOperation.NewSchema)
                    ? "Nothing"
                    : Quote(moveProcedureOperation.NewSchema));
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="RenameTableOperation" />.
        /// </summary>
        /// <param name="renameTableOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(RenameTableOperation renameTableOperation, IndentedTextWriter writer)
        {
            Check.NotNull(renameTableOperation, "renameTableOperation");
            Check.NotNull(writer, "writer");

            writer.Write("RenameTable(name := ");
            writer.Write(Quote(renameTableOperation.Name));
            writer.Write(", newName := ");
            writer.Write(Quote(renameTableOperation.NewName));
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="RenameProcedureOperation" />.
        /// </summary>
        /// <param name="renameProcedureOperation">The operation to generate code for.</param>
        /// <param name="writer">Text writer to add the generated code to.</param>
        protected virtual void Generate(RenameProcedureOperation renameProcedureOperation, IndentedTextWriter writer)
        {
            Check.NotNull(renameProcedureOperation, "renameProcedureOperation");
            Check.NotNull(writer, "writer");

            writer.Write("RenameStoredProcedure(name := ");
            writer.Write(Quote(renameProcedureOperation.Name));
            writer.Write(", newName := ");
            writer.Write(Quote(renameProcedureOperation.NewName));
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="RenameColumnOperation" />.
        /// </summary>
        /// <param name="renameColumnOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(RenameColumnOperation renameColumnOperation, IndentedTextWriter writer)
        {
            Check.NotNull(renameColumnOperation, "renameColumnOperation");
            Check.NotNull(writer, "writer");

            writer.Write("RenameColumn(table := ");
            writer.Write(Quote(renameColumnOperation.Table));
            writer.Write(", name := ");
            writer.Write(Quote(renameColumnOperation.Name));
            writer.Write(", newName := ");
            writer.Write(Quote(renameColumnOperation.NewName));
            writer.WriteLine(")");
        }
        
        /// <summary>
        /// Generates code to perform a <see cref="RenameIndexOperation" />.
        /// </summary>
        /// <param name="renameIndexOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(RenameIndexOperation renameIndexOperation, IndentedTextWriter writer)
        {
            Check.NotNull(renameIndexOperation, "renameIndexOperation");
            Check.NotNull(writer, "writer");

            writer.Write("RenameIndex(table := ");
            writer.Write(Quote(renameIndexOperation.Table));
            writer.Write(", name := ");
            writer.Write(Quote(renameIndexOperation.Name));
            writer.Write(", newName := ");
            writer.Write(Quote(renameIndexOperation.NewName));
            writer.WriteLine(")");
        }

        /// <summary>
        /// Generates code to perform a <see cref="SqlOperation" />.
        /// </summary>
        /// <param name="sqlOperation"> The operation to generate code for. </param>
        /// <param name="writer"> Text writer to add the generated code to. </param>
        protected virtual void Generate(SqlOperation sqlOperation, IndentedTextWriter writer)
        {
            Check.NotNull(sqlOperation, "sqlOperation");
            Check.NotNull(writer, "writer");

            writer.Write("Sql(");
            writer.Write(Quote(sqlOperation.Sql));

            if (sqlOperation.SuppressTransaction)
            {
                writer.Write(", suppressTransaction := True");
            }

            writer.WriteLine(")");
        }

        /// <summary>
        /// Removes any invalid characters from the name of an database artifact.
        /// </summary>
        /// <param name="name"> The name to be scrubbed. </param>
        /// <returns> The scrubbed name. </returns>
        [SuppressMessage("Microsoft.Security", "CA2141:TransparentMethodsMustNotSatisfyLinkDemandsFxCopRule")]
        protected virtual string ScrubName(string name)
        {
            Check.NotEmpty(name, "name");

            var invalidChars
                = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");

            name = invalidChars.Replace(name, string.Empty);

            using (var codeProvider = new VBCodeProvider())
            {
                if ((!char.IsLetter(name[0]) && name[0] != '_')
                    || !codeProvider.IsValidIdentifier(name))
                {
                    name = "_" + name;
                }
            }

            return name;
        }

        /// <summary>
        /// Gets the type name to use for a column of the given data type.
        /// </summary>
        /// <param name="primitiveTypeKind"> The data type to translate. </param>
        /// <returns> The type name to use in the generated migration. </returns>
        protected virtual string TranslateColumnType(PrimitiveTypeKind primitiveTypeKind)
        {
            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.Int16:
                    return "Short";
                case PrimitiveTypeKind.Int32:
                    return "Int";
                case PrimitiveTypeKind.Int64:
                    return "Long";
                default:
                    return Enum.GetName(typeof(PrimitiveTypeKind), primitiveTypeKind);
            }
        }

        /// <summary>
        /// Quotes an identifier using appropriate escaping to allow it to be stored in a string.
        /// </summary>
        /// <param name="identifier"> The identifier to be quoted. </param>
        /// <returns> The quoted identifier. </returns>
        protected virtual string Quote(string identifier)
        {
            return "\"" + identifier + "\"";
        }
    }
}
