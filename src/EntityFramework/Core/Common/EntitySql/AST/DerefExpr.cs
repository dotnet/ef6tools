// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.EntitySql.AST
{
    // <summary>
    // Represents DEREF(epxr) expression.
    // </summary>
    internal sealed class DerefExpr : Node
    {
        private readonly Node _argExpr;

        // <summary>
        // Initializes DEREF expression node.
        // </summary>
        internal DerefExpr(Node derefArgExpr)
        {
            _argExpr = derefArgExpr;
        }

        // <summary>
        // Ieturns ref argument expression.
        // </summary>
        internal Node ArgExpr
        {
            get { return _argExpr; }
        }
    }
}
