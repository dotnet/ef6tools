// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Core.EntityClient
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.EntityClient.Internal;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using IsolationLevel = System.Data.IsolationLevel;

    /// <summary>
    /// Class representing a connection for the conceptual layer. An entity connection may only
    /// be initialized once (by opening the connection). It is subsequently not possible to change
    /// the connection string, attach a new store connection, or change the store connection string.
    /// </summary>
    public class EntityConnection : DbConnection
    {
        private const string EntityClientProviderName = "System.Data.EntityClient";
        private const string ProviderInvariantName = "provider";
        private const string ProviderConnectionString = "provider connection string";
        private const string ReaderPrefix = "reader://";

        private readonly object _connectionStringLock = new object();
        private static readonly DbConnectionOptions _emptyConnectionOptions = new DbConnectionOptions(String.Empty, new string[0]);

        // The connection options object having the connection settings needed by this connection
        private DbConnectionOptions _userConnectionOptions;
        private DbConnectionOptions _effectiveConnectionOptions;

        // The internal connection state of the entity client, which reflects the underlying
        // store connection's state.
        private ConnectionState _entityClientConnectionState = ConnectionState.Closed;

        private DbProviderFactory _providerFactory;
        private DbConnection _storeConnection;
        private readonly bool _entityConnectionOwnsStoreConnection = true;
        private MetadataWorkspace _metadataWorkspace;
        // DbTransaction started using BeginDbTransaction() method
        private EntityTransaction _currentTransaction;
        // Transaction the user enlisted in using EnlistTransaction() method
        private Transaction _enlistedTransaction;
        private bool _initialized;

        private ConnectionState? _fakeConnectionState;
        private readonly List<ObjectContext> _associatedContexts = new List<ObjectContext>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> class.
        /// </summary>
        [ResourceExposure(ResourceScope.None)] //We are not exposing any resource
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
            Justification = "Object is in fact passed to property of the class and gets Disposed properly in the Dispose() method.")]
        //For EntityConnection constructor. But since the connection string we pass in is an Empty String,
        //we consume the resource and do not expose it any further.        
        public EntityConnection()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> class, based on the connection string.
        /// </summary>
        /// <param name="connectionString">The provider-specific connection string.</param>
        /// <exception cref="T:System.ArgumentException">An invalid connection string keyword has been provided, or a required connection string keyword has not been provided.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        [ResourceExposure(ResourceScope.Machine)] //Exposes the file names as part of ConnectionString which are a Machine resource
        [ResourceConsumption(ResourceScope.Machine)]
        //For ChangeConnectionString method call. But the paths are not created in this method.        
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
            Justification = "Object is in fact passed to property of the class and gets Disposed properly in the Dispose() method.")]
        public EntityConnection(string connectionString)
        {
            ChangeConnectionString(connectionString);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> class with a specified
        /// <see  cref="T:System.Data.Entity.Core.Metadata.Edm.MetadataWorkspace" /> and 
        /// <see cref="T:System.Data.Common.DbConnection" />.
        /// </summary>
        /// <param name="workspace">
        /// A <see cref="T:System.Data.Entity.Core.Metadata.Edm.MetadataWorkspace" /> to be associated with this
        /// <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />.
        /// </param>
        /// <param name="connection">
        /// The underlying data source connection for this <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> object.
        /// </param>
        /// <exception cref="T:System.ArgumentNullException">The  workspace  or  connection  parameter is null.</exception>
        /// <exception cref="T:System.ArgumentException">The conceptual model is missing from the workspace.-or-The mapping file is missing from the workspace.-or-The storage model is missing from the workspace.-or-The  connection  is not in a closed state.</exception>
        /// <exception cref="T:System.Data.Entity.Core.ProviderIncompatibleException">The  connection  is not from an ADO.NET Entity Framework-compatible provider.</exception>
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
            Justification = "Object is in fact passed to property of the class and gets Disposed properly in the Dispose() method.")]
        public EntityConnection(MetadataWorkspace workspace, DbConnection connection)
            : this(Check.NotNull(workspace, "workspace"), Check.NotNull(connection, "connection"), false, false)
        {
        }

        /// <summary>
        /// Constructs the EntityConnection from Metadata loaded in memory
        /// </summary>
        /// <param name="workspace"> Workspace containing metadata information. </param>
        /// <param name="connection"> Store connection. </param>
        /// <param name="entityConnectionOwnsStoreConnection"> If set to true the store connection is disposed when the entity connection is disposed, otherwise the caller must dispose the store connection. </param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
            Justification = "Object is in fact passed to property of the class and gets Disposed properly in the Dispose() method.")]
        public EntityConnection(MetadataWorkspace workspace, DbConnection connection, bool entityConnectionOwnsStoreConnection)
            : this(Check.NotNull(workspace, "workspace"), Check.NotNull(connection, "connection"),
                false, entityConnectionOwnsStoreConnection)
        {
        }

        // <summary>
        // This constructor allows to skip the initialization code for testing purposes.
        // </summary>
        internal EntityConnection(
            MetadataWorkspace workspace,
            DbConnection connection,
            bool skipInitialization,
            bool entityConnectionOwnsStoreConnection)
        {
            if (!skipInitialization)
            {
                if (!workspace.IsItemCollectionAlreadyRegistered(DataSpace.CSpace))
                {
                    throw new ArgumentException(Strings.EntityClient_ItemCollectionsNotRegisteredInWorkspace("EdmItemCollection"));
                }
                if (!workspace.IsItemCollectionAlreadyRegistered(DataSpace.SSpace))
                {
                    throw new ArgumentException(Strings.EntityClient_ItemCollectionsNotRegisteredInWorkspace("StoreItemCollection"));
                }
                if (!workspace.IsItemCollectionAlreadyRegistered(DataSpace.CSSpace))
                {
                    throw new ArgumentException(
                        Strings.EntityClient_ItemCollectionsNotRegisteredInWorkspace("StorageMappingItemCollection"));
                }

                // Verify that a factory can be retrieved
                var providerFactory = connection.GetProviderFactory();
                if (providerFactory == null)
                {
                    throw new ProviderIncompatibleException(Strings.EntityClient_DbConnectionHasNoProvider(connection));
                }

                var collection = (StoreItemCollection)workspace.GetItemCollection(DataSpace.SSpace);

                _providerFactory = collection.ProviderFactory;
                Debug.Assert(_providerFactory == providerFactory);
                _initialized = true;
            }

            _metadataWorkspace = workspace;
            _storeConnection = connection;
            _entityConnectionOwnsStoreConnection = entityConnectionOwnsStoreConnection;

            if (_storeConnection != null)
            {
                _entityClientConnectionState = DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext);
            }

            SubscribeToStoreConnectionStateChangeEvents();
        }

        private void SubscribeToStoreConnectionStateChangeEvents()
        {
            if (_storeConnection != null)
            {
                _storeConnection.StateChange += StoreConnectionStateChangeHandler;
            }
        }

        private void UnsubscribeFromStoreConnectionStateChangeEvents()
        {
            if (_storeConnection != null)
            {
                _storeConnection.StateChange -= StoreConnectionStateChangeHandler;
            }
        }

        // <summary>Handles the event when the database connection state changes.</summary>
        // <param name="sender">The source of the event.</param>
        // <param name="stateChange">The data for the event.</param>
        internal virtual void StoreConnectionStateChangeHandler(Object sender, StateChangeEventArgs stateChange)
        {
            var newStoreConnectionState = stateChange.CurrentState;
            if (_entityClientConnectionState != newStoreConnectionState)
            {
                var origEntityConnectionState = _entityClientConnectionState;
                _entityClientConnectionState = stateChange.CurrentState;
                OnStateChange(new StateChangeEventArgs(origEntityConnectionState, newStoreConnectionState));
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> connection string.
        /// </summary>
        /// <returns>The connection string required to establish the initial connection to a data source. The default value is an empty string. On a closed connection, the currently set value is returned. If no value has been set, an empty string is returned.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// An attempt was made to set the <see cref="P:System.Data.Entity.Core.EntityClient.EntityConnection.ConnectionString" /> property after the
        /// <see
        ///     cref="EntityConnection" />
        /// ’s <see cref="T:System.Data.Entity.Core.Metadata.Edm.MetadataWorkspace" /> was initialized. The
        /// <see
        ///     cref="MetadataWorkspace" />
        /// is initialized either when the <see cref="EntityConnection" /> instance is constructed through the overload that takes a
        /// <see
        ///     cref="MetadataWorkspace" />
        /// as a parameter, or when the
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// instance has been opened.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">An invalid connection string keyword has been provided or a required connection string keyword has not been provided.</exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override string ConnectionString
        {
            get
            {
                // EntityConnection created using MetadataWorkspace
                // _userConnectionOptions is not null when empty Constructor is used
                // Therefore it is sufficient to identify whether EC(MW, DbConnection) is used
                if (_userConnectionOptions == null)
                {
                    Debug.Assert(_storeConnection != null);

                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={3}{4};{1}={5};{2}=\"{6}\";",
                        EntityConnectionStringBuilder.MetadataParameterName,
                        ProviderInvariantName,
                        ProviderConnectionString,
                        ReaderPrefix,
                        _metadataWorkspace.MetadataWorkspaceId,
                        _storeConnection.GetProviderInvariantName(),
                        DbInterception.Dispatch.Connection.GetConnectionString(_storeConnection, InterceptionContext));
                }

                var userConnectionString = _userConnectionOptions.UsersConnectionString;

                // In here, we ask the store connection for the connection string only if the user didn't specify a name
                // connection (meaning effective connection options == user connection options).  If the user specified a
                // named connection, then we return just that.  Otherwise, if the connection string is different from what
                // we have in the connection options, which is possible if the store connection changed the connection
                // string to hide the password, then we use the builder to reconstruct the string. The parameters will be
                // shuffled, which is unavoidable but it's ok because the connection string cannot be the same as what the
                // user originally passed in anyway.  However, if the store connection string is still the same, then we
                // simply return what the user originally passed in.
                if (ReferenceEquals(_userConnectionOptions, _effectiveConnectionOptions)
                    && _storeConnection != null)
                {
                    string storeConnectionString = null;
                    try
                    {
                        storeConnectionString = DbInterception.Dispatch.Connection.GetConnectionString(
                            _storeConnection, InterceptionContext);
                    }
                    catch (Exception e)
                    {
                        if (e.IsCatchableExceptionType())
                        {
                            throw new EntityException(Strings.EntityClient_ProviderSpecificError(@"ConnectionString"), e);
                        }

                        throw;
                    }

                    // SQLBU 514721, 515024 - Defer connection string parsing to ConnectionStringBuilder
                    // if the 'userStoreConnectionString' and 'storeConnectionString' are unequal, except
                    // when they are both null or empty (we treat null and empty as equivalent here).
                    //
                    var userStoreConnectionString =
                        _userConnectionOptions[EntityConnectionStringBuilder.ProviderConnectionStringParameterName];
                    if ((storeConnectionString != userStoreConnectionString)
                        && !(string.IsNullOrEmpty(storeConnectionString) && string.IsNullOrEmpty(userStoreConnectionString)))
                    {
                        // Feeds the connection string into the connection string builder, then plug in the provider connection string into
                        // the builder, and then extract the string from the builder
                        var connectionStringBuilder = new EntityConnectionStringBuilder(userConnectionString);
                        connectionStringBuilder.ProviderConnectionString = storeConnectionString;
                        return connectionStringBuilder.ConnectionString;
                    }
                }

                return userConnectionString;
            }
            [ResourceExposure(ResourceScope.Machine)] // Exposes the file names as part of ConnectionString which are a Machine resource
            [ResourceConsumption(ResourceScope.Machine)]
            // For ChangeConnectionString method call. But the paths are not created in this method.
            set
            {
                if (_initialized)
                {
                    throw new InvalidOperationException(Strings.EntityClient_SettingsCannotBeChangedOnOpenConnection);
                }
                ChangeConnectionString(value);
            }
        }

        internal IEnumerable<ObjectContext> AssociatedContexts
        {
            get { return _associatedContexts; }
        }

        internal virtual void AssociateContext(ObjectContext context)
        {
            DebugCheck.NotNull(context);

            if (_associatedContexts.Count != 0)
            {
                foreach (var alreadyAssociated in _associatedContexts.ToArray())
                {
                    if (ReferenceEquals(context, alreadyAssociated)
                        || alreadyAssociated.IsDisposed)
                    {
                        _associatedContexts.Remove(alreadyAssociated);
                    }
                }
            }

            _associatedContexts.Add(context);
        }

        internal DbInterceptionContext InterceptionContext
        {
            get { return DbInterceptionContext.Combine(AssociatedContexts.Select(c => c.InterceptionContext)); }
        }

        /// <summary>Gets the number of seconds to wait when attempting to establish a connection before ending the attempt and generating an error.</summary>
        /// <returns>The time (in seconds) to wait for a connection to open. The default value is the underlying data provider's default time-out. </returns>
        /// <exception cref="T:System.ArgumentException">The value set is less than 0. </exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override int ConnectionTimeout
        {
            get
            {
                if (_storeConnection == null)
                {
                    return 0;
                }

                try
                {
                    return DbInterception.Dispatch.Connection.GetConnectionTimeout(_storeConnection, InterceptionContext);
                }
                catch (Exception e)
                {
                    if (e.IsCatchableExceptionType())
                    {
                        throw new EntityException(Strings.EntityClient_ProviderSpecificError(@"ConnectionTimeout"), e);
                    }

                    throw;
                }
            }
        }

        /// <summary>Gets the name of the current database, or the database that will be used after a connection is opened.</summary>
        /// <returns>The value of the Database property of the underlying data provider.</returns>
        /// <exception cref="T:System.InvalidOperationException">The underlying data provider is not known. </exception>
        public override string Database
        {
            get { return String.Empty; }
        }

        /// <summary>
        /// Gets the state of the EntityConnection, which is set up to track the state of the underlying
        /// database connection that is wrapped by this EntityConnection.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override ConnectionState State
        {
            get { return _fakeConnectionState ?? _entityClientConnectionState; }
        }

        /// <summary>Gets the name or network address of the data source to connect to.</summary>
        /// <returns>The name of the data source. The default value is an empty string.</returns>
        /// <exception cref="T:System.InvalidOperationException">The underlying data provider is not known. </exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override string DataSource
        {
            get
            {
                if (_storeConnection == null)
                {
                    return String.Empty;
                }

                try
                {
                    return DbInterception.Dispatch.Connection.GetDataSource(_storeConnection, InterceptionContext);
                }
                catch (Exception e)
                {
                    if (e.IsCatchableExceptionType())
                    {
                        throw new EntityException(Strings.EntityClient_ProviderSpecificError(@"DataSource"), e);
                    }

                    throw;
                }
            }
        }

        /// <summary>Gets a string that contains the version of the data source to which the client is connected.</summary>
        /// <returns>The version of the data source that is contained in the provider connection string.</returns>
        /// <exception cref="T:System.InvalidOperationException">The connection is closed. </exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override string ServerVersion
        {
            get
            {
                if (_storeConnection == null)
                {
                    throw Error.EntityClient_ConnectionStringNeededBeforeOperation();
                }

                if (State != ConnectionState.Open)
                {
                    throw Error.EntityClient_ConnectionNotOpen();
                }

                try
                {
                    return DbInterception.Dispatch.Connection.GetServerVersion(_storeConnection, InterceptionContext);
                }
                catch (Exception e)
                {
                    if (e.IsCatchableExceptionType())
                    {
                        throw new EntityException(Strings.EntityClient_ProviderSpecificError(@"ServerVersion"), e);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the provider factory associated with EntityConnection
        /// </summary>
        protected override DbProviderFactory DbProviderFactory
        {
            get { return EntityProviderFactory.Instance; }
        }

        // <summary>
        // Gets the DbProviderFactory for the underlying provider
        // </summary>
        internal virtual DbProviderFactory StoreProviderFactory
        {
            get { return _providerFactory; }
        }

        /// <summary>
        /// Provides access to the underlying data source connection that is used by the
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// object.
        /// </summary>
        /// <returns>
        /// The <see cref="T:System.Data.Common.DbConnection" /> for the data source connection.
        /// </returns>
        public virtual DbConnection StoreConnection
        {
            get { return _storeConnection; }
        }

        /// <summary>
        /// Returns the <see cref="T:System.Data.Entity.Core.Metadata.Edm.MetadataWorkspace" /> associated with this
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// .
        /// </summary>
        /// <returns>
        /// The <see cref="T:System.Data.Entity.Core.Metadata.Edm.MetadataWorkspace" /> associated with this
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// .
        /// </returns>
        /// <exception cref="T:System.Data.Entity.Core.MetadataException">The inline connection string contains an invalid Metadata keyword value.</exception>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual MetadataWorkspace GetMetadataWorkspace()
        {
            if (_metadataWorkspace != null)
            {
                return _metadataWorkspace;
            }

            _metadataWorkspace = MetadataCache.Instance.GetMetadataWorkspace(_effectiveConnectionOptions);
            _initialized = true;
            return _metadataWorkspace;
        }

        /// <summary>
        /// Gets the current transaction that this connection is enlisted in. May be null.
        /// </summary>
        public virtual EntityTransaction CurrentTransaction
        {
            get
            {
                // Null out the current transaction if the state is closed or zombied
                if ((null != _currentTransaction)
                    && ((null
                         == DbInterception.Dispatch.Transaction.GetConnection(_currentTransaction.StoreTransaction, InterceptionContext))
                        || (State == ConnectionState.Closed)))
                {
                    ClearCurrentTransaction();
                }

                return _currentTransaction;
            }
        }

        // <summary>
        // Whether the user has enlisted in transaction using EnlistTransaction method
        // </summary>
        internal virtual bool EnlistedInUserTransaction
        {
            get
            {
                try
                {
                    return _enlistedTransaction != null && _enlistedTransaction.TransactionInformation.Status == TransactionStatus.Active;
                }
                catch (ObjectDisposedException)
                {
                    _enlistedTransaction = null;
                    return false;
                }
            }
        }

        /// <summary>Establishes a connection to the data source by calling the underlying data provider's Open method.</summary>
        /// <exception cref="T:System.InvalidOperationException">An error occurs when you open the connection, or the name of the underlying data provider is not known.</exception>
        /// <exception cref="T:System.Data.Entity.Core.MetadataException">The inline connection string contains an invalid Metadata keyword value.</exception>
        public override void Open()
        {
            _fakeConnectionState = null;

            if (!DbInterception.Dispatch.CancelableEntityConnection.Opening(this, InterceptionContext))
            {
                _fakeConnectionState = ConnectionState.Open;

                return;
            }

            if (_storeConnection == null)
            {
                throw Error.EntityClient_ConnectionStringNeededBeforeOperation();
            }

            if (State == ConnectionState.Broken)
            {
                throw Error.EntityClient_CannotOpenBrokenConnection();
            }

            if (DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) != ConnectionState.Open)
            {
                var metadataWorkspace = GetMetadataWorkspace();
                try
                {
                    DbProviderServices.GetExecutionStrategy(_storeConnection, metadataWorkspace).Execute(
                        () => DbInterception.Dispatch.Connection.Open(_storeConnection, InterceptionContext));
                }
                catch (Exception e)
                {
                    if (e.IsCatchableExceptionType())
                    {
                        var exceptionMessage = Strings.EntityClient_ProviderSpecificError("Open");
                        throw new EntityException(exceptionMessage, e);
                    }

                    throw;
                }

                // With every successful open of the store connection, always null out the current db transaction and enlistedTransaction
                ClearTransactions();
            }

            // the following guards against the case when the user closes the underlying store connection
            // in the state change event handler, as a consequence of which we are in the 'Broken' state
            if (_storeConnection == null
                || DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) != ConnectionState.Open)
            {
                throw Error.EntityClient_ConnectionNotOpen();
            }
        }

#if !NET40

        /// <summary>
        /// Asynchronously establishes a connection to the data store by calling the Open method on the underlying data provider
        /// </summary>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        public override async Task OpenAsync(CancellationToken cancellationToken)
        {
            if (_storeConnection == null)
            {
                throw Error.EntityClient_ConnectionStringNeededBeforeOperation();
            }

            if (State == ConnectionState.Broken)
            {
                throw Error.EntityClient_CannotOpenBrokenConnection();
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) != ConnectionState.Open)
            {
                var metadataWorkspace = GetMetadataWorkspace();
                try
                {
                    var executionStrategy = DbProviderServices.GetExecutionStrategy(_storeConnection, metadataWorkspace);
                    await executionStrategy.ExecuteAsync(
                        () => DbInterception.Dispatch.Connection.OpenAsync(_storeConnection, InterceptionContext, cancellationToken),
                        cancellationToken)
                        .WithCurrentCulture();
                }
                catch (Exception e)
                {
                    if (e.IsCatchableExceptionType())
                    {
                        var exceptionMessage = Strings.EntityClient_ProviderSpecificError("Open");
                        throw new EntityException(exceptionMessage, e);
                    }

                    throw;
                }

                // With every successful open of the store connection, always null out the current db transaction and enlistedTransaction
                ClearTransactions();
            }

            // the following guards against the case when the user closes the underlying store connection
            // in the state change event handler, as a consequence of which we are in the 'Broken' state
            if (_storeConnection == null
                || DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) != ConnectionState.Open)
            {
                throw Error.EntityClient_ConnectionNotOpen();
            }
        }

#endif

        /// <summary>
        /// Creates a new instance of an <see cref="T:System.Data.Entity.Core.EntityClient.EntityCommand" />, with the
        /// <see
        ///     cref="P:System.Data.Entity.Core.EntityClient.EntityCommand.Connection" />
        /// set to this
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// .
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Data.Entity.Core.EntityClient.EntityCommand" /> object.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The name of the underlying data provider is not known.</exception>
        public new virtual EntityCommand CreateCommand()
        {
            return new EntityCommand(null, this);
        }

        /// <summary>
        /// Create a new command object that uses this connection object
        /// </summary>
        /// <returns>The command object.</returns>
        protected override DbCommand CreateDbCommand()
        {
            return CreateCommand();
        }

        /// <summary>Closes the connection to the database.</summary>
        /// <exception cref="T:System.InvalidOperationException">An error occurred when closing the connection.</exception>
        public override void Close()
        {
            _fakeConnectionState = null;

            // It's a no-op if there isn't an underlying connection
            if (_storeConnection == null)
            {
                return;
            }

            StoreCloseHelper(); // note: we will update our own state since we are subscribed to event on underlying store connection
        }

        /// <summary>Not supported.</summary>
        /// <param name="databaseName">Not supported. </param>
        /// <exception cref="T:System.NotSupportedException">When the method is called. </exception>
        public override void ChangeDatabase(string databaseName)
        {
            throw new NotSupportedException();
        }

        /// <summary>Begins a transaction by using the underlying provider. </summary>
        /// <returns>
        /// A new <see cref="T:System.Data.Entity.Core.EntityClient.EntityTransaction" />. The returned
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityTransaction" />
        /// instance can later be associated with the
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityCommand" />
        /// to execute the command under that transaction.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// The underlying provider is not known.-or-The call to
        /// <see
        ///     cref="M:System.Data.Entity.Core.EntityClient.EntityConnection.BeginTransaction" />
        /// was made on an
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// that already has a current transaction.-or-The state of the
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// is not
        /// <see
        ///     cref="F:System.Data.ConnectionState.Open" />
        /// .
        /// </exception>
        public new virtual EntityTransaction BeginTransaction()
        {
            return base.BeginTransaction() as EntityTransaction;
        }

        /// <summary>Begins a transaction with the specified isolation level by using the underlying provider. </summary>
        /// <returns>
        /// A new <see cref="T:System.Data.Entity.Core.EntityClient.EntityTransaction" />. The returned
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityTransaction" />
        /// instance can later be associated with the
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityCommand" />
        /// to execute the command under that transaction.
        /// </returns>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <exception cref="T:System.InvalidOperationException">
        /// The underlying provider is not known.-or-The call to
        /// <see
        ///     cref="M:System.Data.Entity.Core.EntityClient.EntityConnection.BeginTransaction" />
        /// was made on an
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// that already has a current transaction.-or-The state of the
        /// <see
        ///     cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" />
        /// is not
        /// <see
        ///     cref="F:System.Data.ConnectionState.Open" />
        /// .
        /// </exception>
        public new virtual EntityTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return base.BeginTransaction(isolationLevel) as EntityTransaction;
        }

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        /// <param name="isolationLevel"> The isolation level of the transaction </param>
        /// <returns> An object representing the new transaction </returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            if (_fakeConnectionState != null)
            {
                return new EntityTransaction();
            }

            if (CurrentTransaction != null)
            {
                throw new InvalidOperationException(Strings.EntityClient_TransactionAlreadyStarted);
            }

            if (_storeConnection == null)
            {
                throw Error.EntityClient_ConnectionStringNeededBeforeOperation();
            }

            if (State != ConnectionState.Open)
            {
                throw Error.EntityClient_ConnectionNotOpen();
            }

            var interceptionContext = new BeginTransactionInterceptionContext(InterceptionContext);
            if (isolationLevel != IsolationLevel.Unspecified)
            {
                interceptionContext = interceptionContext.WithIsolationLevel(isolationLevel);
            }

            DbTransaction storeTransaction = null;
            try
            {
                var executionStrategy = DbProviderServices.GetExecutionStrategy(_storeConnection, GetMetadataWorkspace());
                storeTransaction = executionStrategy.Execute(
                    () =>
                    {
                        if (DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) == ConnectionState.Broken)
                        {
                            DbInterception.Dispatch.Connection.Close(_storeConnection, interceptionContext);
                        }

                        if (DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) == ConnectionState.Closed)
                        {
                            DbInterception.Dispatch.Connection.Open(_storeConnection, interceptionContext);
                        }

                        return DbInterception.Dispatch.Connection.BeginTransaction(
                            _storeConnection,
                            interceptionContext);
                    });
            }
            catch (Exception e)
            {
                if (e.IsCatchableExceptionType())
                {
                    throw new EntityException(Strings.EntityClient_ErrorInBeginningTransaction, e);
                }
                throw;
            }

            // The provider is problematic if it succeeded in beginning a transaction but returned a null
            // for the transaction object
            if (storeTransaction == null)
            {
                throw new ProviderIncompatibleException(
                    Strings.EntityClient_ReturnedNullOnProviderMethod("BeginTransaction", _storeConnection.GetType().Name));
            }

            _currentTransaction = new EntityTransaction(this, storeTransaction);
            return _currentTransaction;
        }

        // <summary>
        // Enables the user to pass in a database transaction created outside of the Entity Framework
        // if you want the framework to execute commands within that external transaction.
        // Or pass in null to clear the Framework's knowledge of the current transaction.
        // </summary>
        // <returns>the EntityTransaction wrapping the DbTransaction or null if cleared</returns>
        // <exception cref="InvalidOperationException">Thrown if the transaction is already completed</exception>
        // <exception cref="InvalidOperationException">
        // Thrown if the connection associated with the <see cref="Database" /> object is already enlisted in a
        // <see
        //     cref="System.Transactions.TransactionScope" />
        // transaction
        // </exception>
        // <exception cref="InvalidOperationException">
        // Thrown if the connection associated with the <see cref="Database" /> object is already participating in a transaction
        // </exception>
        // <exception cref="InvalidOperationException">Thrown if the connection associated with the transaction does not match the Entity Framework's connection</exception>
        internal virtual EntityTransaction UseStoreTransaction(DbTransaction storeTransaction)
        {
            if (storeTransaction == null)
            {
                ClearCurrentTransaction();
            }
            else
            {
                if (CurrentTransaction != null)
                {
                    throw new InvalidOperationException(Strings.DbContext_TransactionAlreadyStarted);
                }

                if (EnlistedInUserTransaction)
                {
                    throw new InvalidOperationException(Strings.DbContext_TransactionAlreadyEnlistedInUserTransaction);
                }

                var transactionConnection = DbInterception.Dispatch.Transaction.GetConnection(
                    storeTransaction, InterceptionContext);
                if (transactionConnection == null)
                {
                    throw new InvalidOperationException(Strings.DbContext_InvalidTransactionNoConnection);
                }

                if (transactionConnection != StoreConnection)
                {
                    throw new InvalidOperationException(Strings.DbContext_InvalidTransactionForConnection);
                }

                _currentTransaction = new EntityTransaction(this, storeTransaction);
            }

            return _currentTransaction;
        }

        /// <summary>
        /// Enlists this <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> in the specified transaction.
        /// </summary>
        /// <param name="transaction">The transaction object to enlist into.</param>
        /// <exception cref="T:System.InvalidOperationException">
        /// The state of the <see cref="T:System.Data.Entity.Core.EntityClient.EntityConnection" /> is not
        /// <see
        ///     cref="F:System.Data.ConnectionState.Open" />
        /// .
        /// </exception>
        public override void EnlistTransaction(Transaction transaction)
        {
            if (_storeConnection == null)
            {
                throw Error.EntityClient_ConnectionStringNeededBeforeOperation();
            }

            if (State != ConnectionState.Open)
            {
                throw Error.EntityClient_ConnectionNotOpen();
            }

            try
            {
                var interceptionContext = new EnlistTransactionInterceptionContext(InterceptionContext);
                interceptionContext = interceptionContext.WithTransaction(transaction);

                DbInterception.Dispatch.Connection.EnlistTransaction(_storeConnection, interceptionContext);

                // null means "Unenlist transaction". It is fine if no transaction is in progress (no op). Otherwise
                // _storeConnection.EnlistTransaction should throw and we would not get here.
                Debug.Assert(
                    transaction != null || !EnlistedInUserTransaction,
                    "DbConnection should not allow unenlist from a transaction that has not completed.");

                // It is OK to enlist in null transaction or multiple times in the same transaction. 
                // In the latter case we don't need to be called multiple times when the transaction completes
                // so subscribe only when enlisting for the first time. Note that _storeConnection.EnlistTransaction
                // will throw in invalid cases (like enlisting the connection in a transaction when another
                // transaction has not completed) so when we get here we are sure that either no transactions are
                // active or the transaction the caller tries enlisting to 
                // is the active transaction.
                if (transaction != null
                    && !EnlistedInUserTransaction)
                {
                    transaction.TransactionCompleted += EnlistedTransactionCompleted;
                }

                _enlistedTransaction = transaction;
            }
            catch (Exception e)
            {
                if (e.IsCatchableExceptionType())
                {
                    throw new EntityException(Strings.EntityClient_ProviderSpecificError(@"EnlistTransaction"), e);
                }
                throw;
            }
        }

        /// <summary>
        /// Cleans up this connection object
        /// </summary>
        /// <param name="disposing"> true to release both managed and unmanaged resources; false to release only unmanaged resources </param>
        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_currentTransaction")]
        [ResourceExposure(ResourceScope.None)] //We are not exposing any resource
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        //For ChangeConnectionString method call. But since the connection string we pass in is an Empty String,
        //we consume the resource and do not expose it any further.
        protected override void Dispose(bool disposing)
        {
            // It is possible for the EntityConnection to be finalized even if the object was not actually
            // created due to a "won't fix" bug in the x86 JITer--see Dev10 bug 892884.
            // Even without this bug, a stack overflow trying to allocate space to run the constructor can
            // result in effectively the same situation.  This means we can end up finalizing objects that
            // have not even been fully initialized.  In order for this to work we have to be very careful
            // what we do in Dispose and we need to stick rigidly to the "only dispose unmanaged resources
            // if disposing is false" rule.  We don't actually have any unmanaged resources--these are
            // handled by the base class or other managed classes that we have references to.  These classes
            // will dispose of their unmanaged resources on finalize, so we shouldn't try to do it here.
            if (disposing)
            {
                ClearTransactions();

                if (_storeConnection != null)
                {
                    if (_entityConnectionOwnsStoreConnection)
                    {
                        StoreCloseHelper(); // closes store connection
                    }

                    UnsubscribeFromStoreConnectionStateChangeEvents();

                    if (_entityConnectionOwnsStoreConnection)
                    {
                        DbInterception.Dispatch.Connection.Dispose(_storeConnection, InterceptionContext);
                    }

                    _storeConnection = null;
                }

                // ensure our own state is closed even if _storeConnection was null
                _entityClientConnectionState = ConnectionState.Closed;

                // Change the connection string to just an empty string, ChangeConnectionString should always succeed here,
                // it's unnecessary to pass in the connection string parameter name in the second argument, which we don't
                // have anyway
                ChangeConnectionString(String.Empty);
            }
            base.Dispose(disposing);
        }

        // <summary>
        // Clears the current DbTransaction for this connection
        // </summary>
        internal virtual void ClearCurrentTransaction()
        {
            _currentTransaction = null;
        }

        // <summary>
        // Reinitialize this connection object to use the new connection string
        // </summary>
        // <param name="newConnectionString"> The new connection string </param>
        [ResourceExposure(ResourceScope.Machine)] //Exposes the file names which are a Machine resource as part of the connection string
        private void ChangeConnectionString(string newConnectionString)
        {
            var userConnectionOptions = _emptyConnectionOptions;
            if (!String.IsNullOrEmpty(newConnectionString))
            {
                userConnectionOptions = new DbConnectionOptions(newConnectionString, EntityConnectionStringBuilder.ValidKeywords);
            }

            DbProviderFactory factory = null;
            DbConnection storeConnection = null;
            var effectiveConnectionOptions = userConnectionOptions;

            if (!userConnectionOptions.IsEmpty)
            {
                // Check if we have the named connection, if yes, then use the connection string from the configuration manager settings
                var namedConnection = userConnectionOptions[EntityConnectionStringBuilder.NameParameterName];
                if (!string.IsNullOrEmpty(namedConnection))
                {
                    // There cannot be other parameters when the named connection is specified
                    if (1 < userConnectionOptions.Parsetable.Count)
                    {
                        throw new ArgumentException(Strings.EntityClient_ExtraParametersWithNamedConnection);
                    }

                    // Find the named connection from the configuration, then extract the settings
                    var setting = ConfigurationManager.ConnectionStrings[namedConnection];
                    if (setting == null
                        || setting.ProviderName != EntityClientProviderName)
                    {
                        throw new ArgumentException(Strings.EntityClient_InvalidNamedConnection);
                    }

                    effectiveConnectionOptions = new DbConnectionOptions(
                        setting.ConnectionString, EntityConnectionStringBuilder.ValidKeywords);

                    // Check for a nested Name keyword
                    var nestedNamedConnection = effectiveConnectionOptions[EntityConnectionStringBuilder.NameParameterName];
                    if (!string.IsNullOrEmpty(nestedNamedConnection))
                    {
                        throw new ArgumentException(Strings.EntityClient_NestedNamedConnection(namedConnection));
                    }
                }

                //Validate the connection string has the required Keywords( Provider and Metadata)
                //We trim the values for both the Keywords, so a string value with only spaces will throw an exception
                //reporting back to the user that the Keyword was missing.
                ValidateValueForTheKeyword(effectiveConnectionOptions, EntityConnectionStringBuilder.MetadataParameterName);

                var providerName = ValidateValueForTheKeyword(
                    effectiveConnectionOptions, EntityConnectionStringBuilder.ProviderParameterName);
                // Get the correct provider factory
                factory = DbConfiguration.DependencyResolver.GetService<DbProviderFactory>(providerName);

                // Create the underlying provider specific connection and give it the connection string from the DbConnectionOptions object
                storeConnection = GetStoreConnection(factory);

                try
                {
                    // When the value of 'Provider Connection String' is null, it means it has not been present in the entity connection string at all.
                    // Providers should still be able handle empty connection strings since those may be explicitly passed by clients.
                    var providerConnectionString =
                        effectiveConnectionOptions[EntityConnectionStringBuilder.ProviderConnectionStringParameterName];
                    if (providerConnectionString != null)
                    {
                        DbInterception.Dispatch.Connection.SetConnectionString(
                            storeConnection,
                            new DbConnectionPropertyInterceptionContext<string>(InterceptionContext).WithValue(providerConnectionString));
                    }
                }
                catch (Exception e)
                {
                    if (e.IsCatchableExceptionType())
                    {
                        throw new EntityException(Strings.EntityClient_ProviderSpecificError(@"ConnectionString"), e);
                    }

                    throw;
                }
            }

            // This lock is to ensure that the connection string matches with the provider connection and metadata workspace that's being
            // managed by this EntityConnection, so states in this connection object are not messed up.
            // It's not for security, but just to help reduce user error.
            lock (_connectionStringLock)
            {
                // Now we have sufficient information and verified the configuration string is good, use them for this connection object
                // Failure should not occur from this point to the end of this method
                _providerFactory = factory;

                _metadataWorkspace = null;

                ClearTransactions();
                UnsubscribeFromStoreConnectionStateChangeEvents();
                _storeConnection = storeConnection;
                SubscribeToStoreConnectionStateChangeEvents();

                // Remembers the connection options objects with the connection string set by the user
                _userConnectionOptions = userConnectionOptions;
                _effectiveConnectionOptions = effectiveConnectionOptions;
            }
        }

        private static string ValidateValueForTheKeyword(
            DbConnectionOptions effectiveConnectionOptions,
            string keywordName)
        {
            var keywordValue = effectiveConnectionOptions[keywordName];
            if (!string.IsNullOrEmpty(keywordValue))
            {
                keywordValue = keywordValue.Trim(); // be nice to user, always trim the value
            }

            // Check that we have a non-null and non-empty value for the keyword
            if (string.IsNullOrEmpty(keywordValue))
            {
                throw new ArgumentException(Strings.EntityClient_ConnectionStringMissingInfo(keywordName));
            }
            return keywordValue;
        }

        // <summary>
        // Clears the current DbTransaction and the transaction the user enlisted the connection in
        // with EnlistTransaction() method.
        // </summary>
        private void ClearTransactions()
        {
            ClearCurrentTransaction();
            ClearEnlistedTransaction();
        }

        // <summary>
        // Clears the transaction the user elinsted in using EnlistTransaction() method.
        // </summary>
        private void ClearEnlistedTransaction()
        {
            if (EnlistedInUserTransaction)
            {
                _enlistedTransaction.TransactionCompleted -= EnlistedTransactionCompleted;
            }

            _enlistedTransaction = null;
        }

        // <summary>
        // Event handler invoked when the transaction has completed (either by committing or rolling back).
        // </summary>
        // <param name="sender"> The source of the event. </param>
        // <param name="e">
        // The <see cref="TransactionEventArgs" /> that contains the event data.
        // </param>
        // <remarks>
        // Note that to avoid threading issues we never reset the <see cref=" _enlistedTransaction" /> field here.
        // </remarks>
        private void EnlistedTransactionCompleted(object sender, TransactionEventArgs e)
        {
            e.Transaction.TransactionCompleted -= EnlistedTransactionCompleted;
        }

        // <summary>
        // Store-specific helper method invoked as part of Close()/Dispose().
        // </summary>
        private void StoreCloseHelper()
        {
            try
            {
                if (_storeConnection != null
                    && (DbInterception.Dispatch.Connection.GetState(_storeConnection, InterceptionContext) != ConnectionState.Closed))
                {
                    DbInterception.Dispatch.Connection.Close(_storeConnection, InterceptionContext);
                }

                // Need to disassociate the transaction objects with this connection
                ClearTransactions();
            }
            catch (Exception e)
            {
                if (e.IsCatchableExceptionType())
                {
                    throw new EntityException(Strings.EntityClient_ErrorInClosingConnection, e);
                }

                throw;
            }
        }

        // <summary>
        // Uses DbProviderFactory to create a DbConnection
        // </summary>
        private static DbConnection GetStoreConnection(DbProviderFactory factory)
        {
            var storeConnection = factory.CreateConnection();
            if (storeConnection == null)
            {
                throw new ProviderIncompatibleException(
                    Strings.EntityClient_ReturnedNullOnProviderMethod("CreateConnection", factory.GetType().Name));
            }

            return storeConnection;
        }
    }
}
