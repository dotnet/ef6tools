// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Query.InternalTrees
{
    using System.Diagnostics;

    // <summary>
    // ExceptOp (Minus)
    // </summary>
    internal sealed class ExceptOp : SetOp
    {
        #region constructors

        private ExceptOp()
            : base(OpType.Except)
        {
        }

        internal ExceptOp(VarVec outputs, VarMap left, VarMap right)
            : base(OpType.Except, outputs, left, right)
        {
        }

        #endregion

        #region public methods

        internal static readonly ExceptOp Pattern = new ExceptOp();

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
