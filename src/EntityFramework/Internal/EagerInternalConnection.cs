// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Data.Common;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Utilities;

    // <summary>
    // A EagerInternalConnection object wraps an already existing DbConnection object.
    // </summary>
    internal class EagerInternalConnection : InternalConnection
    {
        #region Fields and constructors

        private readonly bool _connectionOwned;

        // <summary>
        // Creates a new EagerInternalConnection that wraps an existing DbConnection.
        // </summary>
        // <param name="existingConnection"> An existing connection. </param>
        // <param name="connectionOwned">
        // If set to <c>true</c> then the underlying connection should be disposed when this object is disposed.
        // </param>
        public EagerInternalConnection(DbContext context, DbConnection existingConnection, bool connectionOwned)
            : base(new DbInterceptionContext().WithDbContext(context))
        {
            DebugCheck.NotNull(existingConnection);

            UnderlyingConnection = existingConnection;
            _connectionOwned = connectionOwned;

            OnConnectionInitialized();
        }

        #endregion

        #region Connection management

        // <summary>
        // Returns the origin of the underlying connection string.
        // </summary>
        public override DbConnectionStringOrigin ConnectionStringOrigin
        {
            get { return DbConnectionStringOrigin.UserCode; }
        }

        #endregion

        #region Dispose

        // <summary>
        // Dispose the existing connection is the original caller has specified that it should be disposed
        // by the framework.
        // </summary>
        public override void Dispose()
        {
            if (_connectionOwned)
            {
                if (UnderlyingConnection is EntityConnection)
                {
                    UnderlyingConnection.Dispose();
                }
                else
                {
                    DbInterception.Dispatch.Connection.Dispose(UnderlyingConnection, InterceptionContext);
                }
            }
        }

        #endregion
    }
}
