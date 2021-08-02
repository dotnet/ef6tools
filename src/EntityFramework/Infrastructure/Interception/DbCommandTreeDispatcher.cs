// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Utilities;

    internal class DbCommandTreeDispatcher
    {
        private readonly InternalDispatcher<IDbCommandTreeInterceptor> _internalDispatcher
            = new InternalDispatcher<IDbCommandTreeInterceptor>();

        public InternalDispatcher<IDbCommandTreeInterceptor> InternalDispatcher
        {
            get { return _internalDispatcher; }
        }

        public virtual DbCommandTree Created(DbCommandTree commandTree, DbInterceptionContext interceptionContext)
        {
            DebugCheck.NotNull(commandTree);
            DebugCheck.NotNull(interceptionContext);

            return _internalDispatcher.Dispatch(
                commandTree,
                new DbCommandTreeInterceptionContext(interceptionContext),
                (i, c) => i.TreeCreated(c));
        }
    }
}
