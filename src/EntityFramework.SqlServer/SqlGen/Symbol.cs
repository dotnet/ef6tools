// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.SqlGen
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Globalization;

    // <summary>
    // <see cref="SymbolTable" />
    // This class represents an extent/nested select statement,
    // or a column.
    // The important fields are Name, Type and NewName.
    // NewName starts off the same as Name, and is then modified as necessary.
    // The rest are used by special symbols.
    // e.g. NeedsRenaming is used by columns to indicate that a new name must
    // be picked for the column in the second phase of translation.
    // IsUnnest is used by symbols for a collection expression used as a from clause.
    // This allows <see cref="SqlGenerator.AddFromSymbol(SqlSelectStatement, string, Symbol, bool)" /> to add the column list
    // after the alias.
    // </summary>
    internal class Symbol : ISqlFragment
    {
        // <summary>
        // Used to track the columns originating from this Symbol when it is used
        // in as a from extent in a SqlSelectStatement with a Join or as a From Extent
        // in a Join Symbol.
        // </summary>
        private Dictionary<string, Symbol> columns;

        internal Dictionary<string, Symbol> Columns
        {
            get
            {
                if (null == columns)
                {
                    columns = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);
                }
                return columns;
            }
        }

        // <summary>
        // Used to track the output columns of a SqlSelectStatement it represents
        // </summary>
        private Dictionary<string, Symbol> outputColumns;

        internal Dictionary<string, Symbol> OutputColumns
        {
            get
            {
                if (null == outputColumns)
                {
                    outputColumns = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);
                }
                return outputColumns;
            }
        }

        internal bool NeedsRenaming { get; set; }

        internal bool OutputColumnsRenamed { get; set; }

        private readonly string name;

        public string Name
        {
            get { return name; }
        }

        public string NewName { get; set; }

        internal TypeUsage Type { get; set; }

        public Symbol(string name, TypeUsage type)
        {
            this.name = name;
            NewName = name;
            Type = type;
        }

        // <summary>
        // Use this constructor if the symbol represents a SqlStatement for which the output columns need to be tracked.
        // </summary>
        public Symbol(string name, TypeUsage type, Dictionary<string, Symbol> outputColumns, bool outputColumnsRenamed)
        {
            this.name = name;
            NewName = name;
            Type = type;
            this.outputColumns = outputColumns;
            OutputColumnsRenamed = outputColumnsRenamed;
        }

        #region ISqlFragment Members

        // <summary>
        // Write this symbol out as a string for sql.  This is just
        // the new name of the symbol (which could be the same as the old name).
        // We rename columns here if necessary.
        // </summary>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (NeedsRenaming)
            {
                int i;

                if (sqlGenerator.AllColumnNames.TryGetValue(NewName, out i))
                {
                    string newNameCandidate;
                    do
                    {
                        ++i;
                        newNameCandidate = NewName + i.ToString(CultureInfo.InvariantCulture);
                    }
                    while (sqlGenerator.AllColumnNames.ContainsKey(newNameCandidate));

                    sqlGenerator.AllColumnNames[NewName] = i;

                    NewName = newNameCandidate;
                }

                // Add this column name to list of known names so that there are no subsequent
                // collisions
                sqlGenerator.AllColumnNames[NewName] = 0;

                // Prevent it from being renamed repeatedly.
                NeedsRenaming = false;
            }
            writer.Write(SqlGenerator.QuoteIdentifier(NewName));
        }

        #endregion
    }
}
