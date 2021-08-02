// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServerCompact.SqlGen
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.SqlServerCompact.Utilities;

    // <summary>
    // This class is like StringBuilder.  While traversing the tree for the first time,
    // we do not know all the strings that need to be appended e.g. things that need to be
    // renamed, nested select statements etc.  So, we use a builder that can collect
    // all kinds of sql fragments.
    // </summary>
    internal sealed class SqlBuilder : ISqlFragment
    {
        private List<object> _sqlFragments;

        private List<object> sqlFragments
        {
            get
            {
                if (null == _sqlFragments)
                {
                    _sqlFragments = new List<object>();
                }
                return _sqlFragments;
            }
        }

        // <summary>
        // Add an object to the list - we do not verify that it is a proper sql fragment
        // since this is an internal method.
        // </summary>
        public void Append(object s)
        {
            DebugCheck.NotNull(s);
            sqlFragments.Add(s);
        }

        // <summary>
        // This is to pretty print the SQL.  The writer <see cref="SqlWriter" />
        // needs to know about new lines so that it can add the right amount of
        // indentation at the beginning of lines.
        // </summary>
        public void AppendLine()
        {
            sqlFragments.Add("\r\n");
        }

        // <summary>
        // Whether the builder is empty.  This is used by the <see cref="SqlGenerator.Visit(DbProjectExpression)" />
        // to determine whether a sql statement can be reused.
        // </summary>
        public bool IsEmpty
        {
            get { return ((null == _sqlFragments) || (0 == _sqlFragments.Count)); }
        }

        #region ISqlFragment Members

        // <summary>
        // We delegate the writing of the fragment to the appropriate type.
        // </summary>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (null != _sqlFragments)
            {
                foreach (var o in _sqlFragments)
                {
                    var str = (o as String);
                    if (null != str)
                    {
                        writer.Write(str);
                    }
                    else
                    {
                        var sqlFragment = (o as ISqlFragment);
                        if (null != sqlFragment)
                        {
                            sqlFragment.WriteSql(writer, sqlGenerator);
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
            }
        }

        #endregion
    }
}
