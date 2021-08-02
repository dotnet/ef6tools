// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees
{
    using System.Data.Entity.Core.Common.CommandTrees.Internal;
    using System.Data.Entity.Core.Common.Utils;

    /// <summary>
    /// Specifies a single clause in an insert or update modification operation, see
    /// <see cref="DbInsertCommandTree.SetClauses" /> and <see cref="DbUpdateCommandTree.SetClauses" />
    /// </summary>
    /// <remarks>
    /// An abstract base class allows the possibility of patterns other than
    /// Property = Value in future versions, e.g.,
    /// <code>update SomeTable
    ///     set ComplexTypeColumn.SomeProperty()
    ///     where Id = 2</code>
    /// </remarks>
    public abstract class DbModificationClause
    {
        internal DbModificationClause()
        {
        }

        // Effects: describes the contents of this clause using the given dumper
        internal abstract void DumpStructure(ExpressionDumper dumper);

        // Effects: produces a tree node describing this clause, recursively producing nodes
        // for child expressions using the given expression visitor
        internal abstract TreeNode Print(DbExpressionVisitor<TreeNode> visitor);
    }
}
