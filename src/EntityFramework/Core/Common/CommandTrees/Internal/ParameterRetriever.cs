// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Common.CommandTrees.Internal
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Utilities;
    using System.Linq;

    internal sealed class ParameterRetriever : BasicCommandTreeVisitor
    {
        private readonly Dictionary<string, DbParameterReferenceExpression> paramMappings =
            new Dictionary<string, DbParameterReferenceExpression>();

        private ParameterRetriever()
        {
        }

        internal static ReadOnlyCollection<DbParameterReferenceExpression> GetParameters(DbCommandTree tree)
        {
            DebugCheck.NotNull(tree);

            var retriever = new ParameterRetriever();
            retriever.VisitCommandTree(tree);
            return new ReadOnlyCollection<DbParameterReferenceExpression>(retriever.paramMappings.Values.ToList());
        }

        public override void Visit(DbParameterReferenceExpression expression)
        {
            Check.NotNull(expression, "expression");

            paramMappings[expression.ParameterName] = expression;
        }
    }
}
