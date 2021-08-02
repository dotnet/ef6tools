// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;

    // <summary>
    // Represents a comparision operation (LT, GT etc.)
    // </summary>
    internal sealed class ComparisonOp : ScalarOp
    {
        #region constructors

        internal ComparisonOp(OpType opType, TypeUsage type)
            : base(opType, type)
        {
        }

        private ComparisonOp(OpType opType)
            : base(opType)
        {
        }

        #endregion

        #region public methods

        // <summary>
        // Patterns for use in transformation rules
        // </summary>
        internal static readonly ComparisonOp PatternEq = new ComparisonOp(OpType.EQ);

        // <summary>
        // 2 children - left, right
        // </summary>
        internal override int Arity
        {
            get { return 2; }
        }

        internal bool UseDatabaseNullSemantics { get; set; }

        // <summary>
        // Visitor pattern method
        // </summary>
        // <param name="v"> The BasicOpVisitor that is visiting this Op </param>
        // <param name="n"> The Node that references this Op </param>
        [DebuggerNonUserCode]
        internal override void Accept(BasicOpVisitor v, Node n)
        {
            v.Visit(this, n);
        }

        // <summary>
        // Visitor pattern method for visitors with a return value
        // </summary>
        // <param name="v"> The visitor </param>
        // <param name="n"> The node in question </param>
        // <returns> An instance of TResultType </returns>
        [DebuggerNonUserCode]
        internal override TResultType Accept<TResultType>(BasicOpVisitorOfT<TResultType> v, Node n)
        {
            return v.Visit(this, n);
        }

        #endregion
    }
}
