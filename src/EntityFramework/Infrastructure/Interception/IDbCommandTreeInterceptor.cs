// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.Data.Entity.Core.Common.CommandTrees;

    /// <summary>
    /// An object that implements this interface can be registered with <see cref="DbInterception" /> to
    /// receive notifications when Entity Framework creates <see cref="DbCommandTree" /> command trees.
    /// </summary>
    /// <remarks>
    /// Interceptors can also be registered in the config file of the application.
    /// See http://go.microsoft.com/fwlink/?LinkId=260883 for more information about Entity Framework configuration.
    /// </remarks>
    public interface IDbCommandTreeInterceptor : IDbInterceptor
    {
        /// <summary>
        /// This method is called after a new <see cref="DbCommandTree" /> has been created.
        /// The tree that is used after interception can be changed by setting
        /// <see cref="DbCommandTreeInterceptionContext.Result" /> while intercepting.
        /// </summary>
        /// <remarks>
        /// Command trees are created for both queries and insert/update/delete commands. However, query
        /// command trees are cached by model which means that command tree creation only happens the
        /// first time a query is executed and this notification will only happen at that time
        /// </remarks>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        void TreeCreated(DbCommandTreeInterceptionContext interceptionContext);
    }
}
