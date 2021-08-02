// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees
{
    using System.Data.Entity.Core.Common.CommandTrees.Internal;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;

    /// <summary>Represents a single row delete operation expressed as a command tree. This class cannot be inherited.  </summary>
    public sealed class DbDeleteCommandTree : DbModificationCommandTree
    {
        private readonly DbExpression _predicate;

        internal DbDeleteCommandTree()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbDeleteCommandTree"/> class.
        /// </summary>
        /// <param name="metadata">The model this command will operate on.</param>
        /// <param name="dataSpace">The data space.</param>
        /// <param name="target">The target table for the data manipulation language (DML) operation.</param>
        /// <param name="predicate">A predicate used to determine which members of the target collection should be deleted.</param>
        public DbDeleteCommandTree(MetadataWorkspace metadata, DataSpace dataSpace, DbExpressionBinding target, DbExpression predicate)
            : base(metadata, dataSpace, target)
        {
            DebugCheck.NotNull(predicate);

            _predicate = predicate;
        }

        /// <summary>
        /// Gets an <see cref="T:System.Data.Entity.Core.Common.CommandTrees.DbExpression" /> that specifies the predicate used to determine which members of the target collection should be deleted.
        /// </summary>
        /// <remarks>
        /// The predicate can include only the following elements:
        /// <list>
        ///     <item>Equality expression</item>
        ///     <item>Constant expression</item>
        ///     <item>IsNull expression</item>
        ///     <item>Property expression</item>
        ///     <item>Reference expression to the target</item>
        ///     <item>And expression</item>
        ///     <item>Or expression</item>
        ///     <item>Not expression</item>
        /// </list>
        /// </remarks>        
        /// <returns>
        /// An <see cref="T:System.Data.Entity.Core.Common.CommandTrees.DbExpression" /> that specifies the predicate used to determine which members of the target collection should be deleted.
        /// </returns>
        public DbExpression Predicate
        {
            get { return _predicate; }
        }

        /// <summary>Gets the kind of this command tree.</summary>
        /// <returns>The kind of this command tree.</returns>
        public override DbCommandTreeKind CommandTreeKind
        {
            get { return DbCommandTreeKind.Delete; }
        }

        internal override bool HasReader
        {
            get
            {
                // a delete command never returns server-gen values, and
                // therefore never returns a reader
                return false;
            }
        }

        internal override void DumpStructure(ExpressionDumper dumper)
        {
            base.DumpStructure(dumper);

            if (Predicate != null)
            {
                dumper.Dump(Predicate, "Predicate");
            }
        }

        internal override string PrintTree(ExpressionPrinter printer)
        {
            return printer.Print(this);
        }
    }
}
