// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Internal
{
    using System.Collections.Concurrent;
    using System.Data.Common;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Infrastructure.DependencyResolution;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Migrations.History;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    // <summary>
    // A <see cref="LazyInternalContext" /> is a concrete <see cref="InternalContext" /> type that will lazily create the
    // underlying <see cref="ObjectContext" /> when needed. The <see cref="ObjectContext" /> created is owned by the
    // internal context and will be disposed when the internal context is disposed.
    // </summary>
    internal class LazyInternalContext : InternalContext
    {
        #region Fields and constructors

        // The initialization strategy to use for Code First if no other strategy is set for a context.
        private static readonly CreateDatabaseIfNotExists<DbContext> _defaultCodeFirstInitializer =
            new CreateDatabaseIfNotExists<DbContext>();

        // A cache from context type and provider invariant name to DbCompiledModel objects such that the model for a derived context type is only used once.
        private static readonly
            ConcurrentDictionary<IDbModelCacheKey, RetryLazy<LazyInternalContext, DbCompiledModel>> _cachedModels =
                new ConcurrentDictionary<IDbModelCacheKey, RetryLazy<LazyInternalContext, DbCompiledModel>>();

        // The databases that have been initialized in this app domain in terms of the DbCompiledModel
        // and the connection strings that have been used with that model.
        // This is used to check whether or not database initialization has been performed for a given
        // model/connection pair so that it is only done once per app domain.
        // The lazy is there so that even if two threads attempt to set initialized or perform initialization
        // at virtually the same time the database will only actually be initialized once.
        private static readonly ConcurrentDictionary<Tuple<DbCompiledModel, string>, RetryAction<InternalContext>>
            InitializedDatabases =
                new ConcurrentDictionary<Tuple<DbCompiledModel, string>, RetryAction<InternalContext>>();

        // Responsible for creating a connection lazily when the context is used for the first time.
        private IInternalConnection _internalConnection;

        // Flag set when in the OnModelCreating call of the DbContext so that we can detect attempts
        // to recursively initialize inside that method.
        private bool _creatingModel;

        // The underlying ObjectContext; null until first use.
        private ObjectContext _objectContext;

        // The DbCompiledModel that was used to create this context or was created by the context.
        private DbCompiledModel _model;

        // Set to true if the context was created with an existing DbCompiledModel instance.
        private readonly bool _createdWithExistingModel;

        // This flag is used to keep the user's selected default transactional behavior option before the ObjectContext is initialized.  
        private bool _initialEnsureTransactionsForFunctionsAndCommands = true;

        // This flag is used to keep the user's selected lazy loading option before the ObjectContext is initialized.  
        private bool _initialLazyLoadingFlag = true;

        // This flag is used to keep the user's selected proxy creation option before the ObjectContext is initialized.  
        private bool _initialProxyCreationFlag = true;

        // This flag is used to keep the user's database null comparison behavior option before the ObjectContext is initialized.  
        private bool _useDatabaseNullSemanticsFlag;

        // This flag is used to keep the user's command timeout before the ObjectContext is initialized.  
        private int? _commandTimeout;

        // Set when database initialization is in-progress to prevent attempts to recursively initialize from
        // the initalizer.
        private bool _inDatabaseInitialization;

        private Action<DbModelBuilder> _onModelCreating;

        private readonly Func<DbContext, IDbModelCacheKey> _cacheKeyFactory;

        private readonly AttributeProvider _attributeProvider;

        private DbModel _modelBeingInitialized;

        // <summary>
        // Constructs a <see cref="LazyInternalContext" /> for the given <see cref="DbContext" /> owner that will be initialized
        // on first use.
        // </summary>
        // <param name="owner">
        // The owner <see cref="DbContext" /> .
        // </param>
        // <param name="internalConnection"> Responsible for creating a connection lazily when the context is used for the first time. </param>
        // <param name="model"> The model, or null if it will be created by convention </param>
        public LazyInternalContext(
            DbContext owner,
            IInternalConnection internalConnection,
            DbCompiledModel model,
            Func<DbContext, IDbModelCacheKey> cacheKeyFactory = null,
            AttributeProvider attributeProvider = null,
            Lazy<DbDispatchers> dispatchers = null,
            ObjectContext objectContext = null)
            : base(owner, dispatchers)
        {
            DebugCheck.NotNull(internalConnection);

            _internalConnection = internalConnection;
            _model = model;
            _cacheKeyFactory = cacheKeyFactory ?? new DefaultModelCacheKeyFactory().Create;
            _attributeProvider = attributeProvider ?? new AttributeProvider();
            _objectContext = objectContext;

            _createdWithExistingModel = model != null;

            LoadContextConfigs();
        }

        #endregion

        #region ObjectContext and model

        // <summary>
        // Returns the underlying <see cref="ObjectContext" />.
        // </summary>
        public override ObjectContext ObjectContext
        {
            get
            {
                Initialize();
                return ObjectContextInUse;
            }
        }

        // <summary>
        // The compiled model created from the Code First pipeline, or null if Code First was
        // not used to create this context.
        // Causes the Code First pipeline to be run to create the model if it has not already been
        // created.
        // </summary>
        public override DbCompiledModel CodeFirstModel
        {
            get
            {
                InitializeContext();
                return _model;
            }
        }

        public override DbModel ModelBeingInitialized
        {
            get
            {
                InitializeContext();
                return _modelBeingInitialized;
            }
        }

        // <summary>
        // Returns the underlying <see cref="ObjectContext" /> without causing the underlying database to be created
        // or the database initialization strategy to be executed.
        // This is used to get a context that can then be used for database creation/initialization.
        // </summary>
        public override ObjectContext GetObjectContextWithoutDatabaseInitialization()
        {
            InitializeContext();
            return ObjectContextInUse;
        }

        // <summary>
        // The <see cref="ObjectContext" /> actually being used, which may be the
        // temp context for initialization or the real context.
        // </summary>
        public virtual ObjectContext ObjectContextInUse
        {
            get { return TempObjectContext ?? _objectContext; }
        }

        #endregion

        #region SaveChanges

        // <summary>
        // Saves all changes made in this context to the underlying database, but only if the
        // context has been initialized. If the context has not been initialized, then this
        // method does nothing because there is nothing to do; in particular, it does not
        // cause the context to be initialized.
        // </summary>
        // <returns> The number of objects written to the underlying database. </returns>
        public override int SaveChanges()
        {
            return ObjectContextInUse == null ? 0 : base.SaveChanges();
        }

#if !NET40

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ObjectContextInUse == null ? Task.FromResult(0) : base.SaveChangesAsync(cancellationToken);
        }

#endif

        #endregion

        #region Dispose

        // <summary>
        // Disposes the context. The underlying <see cref="ObjectContext" /> is also disposed.
        // The connection to the database (<see cref="DbConnection" /> object) is also disposed if it was created by
        // the context, otherwise it is not disposed.
        // </summary>
        public override void DisposeContext(bool disposing)
        {
            if (!IsDisposed)
            {
                base.DisposeContext(disposing);

                if (disposing)
                {
                    // Note: issue 1805 - it is important that the ObjectContext be disposed before the InternalConnection.
                    // If the ObjectContext is responsible for calling Dispose() on the EntityConnection (which is
                    // the case for non-EF connection strings) then the EntityConnection needs the chance to unsubscribe
                    // itself from the underlying connection's StateChange event _before_ that underlying connection is disposed.
                    if (_objectContext != null)
                    {
                        _objectContext.Dispose();
                    }
                    _internalConnection.Dispose();
                }
            }
        }

        #endregion

        #region Connection access

        // <summary>
        // The connection underlying this context.  Accessing this property does not cause the context
        // to be initialized, only its connection.
        // </summary>
        public override DbConnection Connection
        {
            get
            {
                CheckContextNotDisposed();

                // If using a temporary ObjectContext we will also be using a cloned connection, so make
                // sure that the connection object returned really is that cloned connection.
                if (TempObjectContext != null)
                {
                    return ((EntityConnection)TempObjectContext.Connection).StoreConnection;
                }

                return _internalConnection.Connection;
            }
        }

        // <summary>
        // The connection string as originally applied to the context. This is used to perform operations
        // that need the connection string in a non-mutated form, such as with security info still intact.
        // </summary>
        public override string OriginalConnectionString
        {
            get { return _internalConnection.OriginalConnectionString; }
        }

        // <summary>
        // Returns the origin of the underlying connection string.
        // </summary>
        public override DbConnectionStringOrigin ConnectionStringOrigin
        {
            get
            {
                CheckContextNotDisposed();
                return _internalConnection.ConnectionStringOrigin;
            }
        }

        // <summary>
        // Gets or sets an object representing a config file used for looking for DefaultConnectionFactory entries
        // and connection strings.
        // </summary>
        public override AppConfig AppConfig
        {
            get { return base.AppConfig; }
            set
            {
                base.AppConfig = value;
                _internalConnection.AppConfig = value;
            }
        }

        // <summary>
        // Gets the name of the underlying connection string.
        // </summary>
        public override string ConnectionStringName
        {
            get
            {
                CheckContextNotDisposed();
                return _internalConnection.ConnectionStringName;
            }
        }

        private DbProviderInfo _modelProviderInfo;

        // <summary>
        // Gets or sets the provider details to be used when building the EDM model.
        // </summary>
        public override DbProviderInfo ModelProviderInfo
        {
            get
            {
                CheckContextNotDisposed();

                return _modelProviderInfo;
            }
            set
            {
                CheckContextNotDisposed();

                _modelProviderInfo = value;
                _internalConnection.ProviderName = _modelProviderInfo.ProviderInvariantName;
            }
        }

        // <inheritdoc />
        public override string ProviderName
        {
            get { return _internalConnection.ProviderName; }
        }

        // <summary>
        // Gets or sets a custom OnModelCreating action.
        // </summary>
        public override Action<DbModelBuilder> OnModelCreating
        {
            get
            {
                CheckContextNotDisposed();

                return _onModelCreating;
            }
            set
            {
                CheckContextNotDisposed();

                _onModelCreating = value;
            }
        }

        // <inheritdoc />
        public override void OverrideConnection(IInternalConnection connection)
        {
            DebugCheck.NotNull(connection);

            // Connection should not be changed once context is initialized
            Debug.Assert(_creatingModel == false);
            Debug.Assert(_objectContext == null);

            connection.AppConfig = AppConfig;

            if (connection.ConnectionHasModel
                != _internalConnection.ConnectionHasModel)
            {
                throw _internalConnection.ConnectionHasModel
                          ? Error.LazyInternalContext_CannotReplaceEfConnectionWithDbConnection()
                          : Error.LazyInternalContext_CannotReplaceDbConnectionWithEfConnection();
            }

            _internalConnection.Dispose();

            _internalConnection = connection;
        }

        #endregion

        #region Initialization

        // <summary>
        // Initializes the underlying <see cref="ObjectContext" />.
        // </summary>
        protected override void InitializeContext()
        {
            CheckContextNotDisposed();

            if (_objectContext == null)
            {
                if (_creatingModel)
                {
                    throw Error.DbContext_ContextUsedInModelCreating();
                }
                try
                {
                    var contextInfo = DbContextInfo.CurrentInfo;
                    if (contextInfo != null)
                    {
                        ApplyContextInfo(contextInfo);
                    }

                    _creatingModel = true;

                    if (_createdWithExistingModel)
                    {
                        // A DbCompiledModel was supplied, which means we should just create the ObjectContext from the model.
                        // The connection cannot be an EF connection because it would then contain a second source of model info.
                        if (_internalConnection.ConnectionHasModel)
                        {
                            throw Error.DbContext_ConnectionHasModel();
                        }

                        Debug.Assert(_model != null);
                        _objectContext = _model.CreateObjectContext<ObjectContext>(_internalConnection.Connection);
                    }
                    else
                    {
                        // No model was supplied, so we should either create one using Code First, or if an EF connection
                        // was supplied then we should use the metadata in that connection to create a model.

                        if (_internalConnection.ConnectionHasModel)
                        {
                            _objectContext = _internalConnection.CreateObjectContextFromConnectionModel();
                        }
                        else
                        {
                            // The idea here is that for a given derived context type and provider we will only ever create one DbCompiledModel.
                            // The delegate given to GetOrAdd may be executed more than once even though ultimately only one of the
                            // values will make it in the dictionary. The RetryLazy ensures that that delegate only gets called
                            // exactly one time, thereby ensuring that OnModelCreating will only ever be called once.  BUT, sometimes
                            // the delegate will fail (and throw and exception). This may be due to some resource issue--most notably
                            // a problem with the database connection. In such a situation it makes sense to have the model creation
                            // try again later when the resource issue has potentially been resolved. To enable this RetryLazy will
                            // try again next time GetValue called. We have to pass the context to GetValue so that the next time it tries
                            // again it will use the new connection.

                            var key = _cacheKeyFactory(Owner);

                            var model
                                = _cachedModels.GetOrAdd(
                                    key, t => new RetryLazy<LazyInternalContext, DbCompiledModel>(CreateModel)).GetValue(this);

                            _objectContext = model.CreateObjectContext<ObjectContext>(_internalConnection.Connection);

                            // Don't actually set the _model unless we succeed in creating the object context.
                            _model = model;
                        }
                    }

                    _objectContext.ContextOptions.EnsureTransactionsForFunctionsAndCommands = _initialEnsureTransactionsForFunctionsAndCommands;
                    _objectContext.ContextOptions.LazyLoadingEnabled = _initialLazyLoadingFlag;
                    _objectContext.ContextOptions.ProxyCreationEnabled = _initialProxyCreationFlag;
                    _objectContext.ContextOptions.UseCSharpNullComparisonBehavior = !_useDatabaseNullSemanticsFlag;
                    _objectContext.CommandTimeout = _commandTimeout;

                    _objectContext.ContextOptions.UseConsistentNullReferenceBehavior = true;

                    _objectContext.InterceptionContext = _objectContext.InterceptionContext.WithDbContext(Owner);

                    ResetDbSets();

                    _objectContext.InitializeMappingViewCacheFactory(Owner);
                }
                finally
                {
                    _creatingModel = false;
                }
            }
        }

        // <summary>
        // Creates an immutable, cacheable representation of the model defined by this builder.
        // This model can be used to create an <see cref="ObjectContext" /> or can be passed to a <see cref="DbContext" />
        // constructor to create a <see cref="DbContext" /> for this model.
        // </summary>
        public static DbCompiledModel CreateModel(LazyInternalContext internalContext)
        {
            var contextType = internalContext.Owner.GetType();

            DbModelStore modelStore = null;
            if (!(internalContext.Owner is HistoryContext))
            {
                modelStore = DbConfiguration.DependencyResolver.GetService<DbModelStore>();
                if (modelStore != null)
                {
                    var compiledModel = modelStore.TryLoad(contextType);
                    if (compiledModel != null)
                    {
                        return compiledModel;
                    }
                }
            }

            var modelBuilder = internalContext.CreateModelBuilder();

            var model
                = (internalContext._modelProviderInfo == null)
                      ? modelBuilder.Build(internalContext._internalConnection.Connection)
                      : modelBuilder.Build(internalContext._modelProviderInfo);

            internalContext._modelBeingInitialized = model;

            if (modelStore != null)
            {
                modelStore.Save(contextType, model);
            }

            return model.Compile();
        }

        // <summary>
        // Creates and configures the <see cref="DbModelBuilder" /> instance that will be used to build the
        // <see cref="DbCompiledModel" />.
        // </summary>
        // <returns> The builder. </returns>
        public DbModelBuilder CreateModelBuilder()
        {
            var versionAttribute = _attributeProvider.GetAttributes(Owner.GetType())
                                                     .OfType<DbModelBuilderVersionAttribute>()
                                                     .FirstOrDefault();
            var version = versionAttribute != null ? versionAttribute.Version : DbModelBuilderVersion.Latest;

            var modelBuilder = new DbModelBuilder(version);

            var modelNamespace = StripInvalidCharacters(Owner.GetType().Namespace);
            if (!String.IsNullOrWhiteSpace(modelNamespace))
            {
                modelBuilder.Conventions.Add(new ModelNamespaceConvention(modelNamespace));
            }

            var modelContainer = StripInvalidCharacters(Owner.GetType().Name);
            if (!String.IsNullOrWhiteSpace(modelContainer))
            {
                modelBuilder.Conventions.Add(new ModelContainerConvention(modelContainer));
            }

            new DbSetDiscoveryService(Owner).RegisterSets(modelBuilder);

            Owner.CallOnModelCreating(modelBuilder);

            if (OnModelCreating != null)
            {
                OnModelCreating(modelBuilder);
            }

            return modelBuilder;
        }

        private static string StripInvalidCharacters(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                // The case where the value is null or whitespace is treated as a special case
                // of a string with no invalid characters and hence the consistent return type
                // for other input of this type is to return the empty string.
                return String.Empty;
            }

            var builder = new StringBuilder(value.Length);
            var nextMustBeStartChar = true;
            foreach (var c in value)
            {
                if (c == '.')
                {
                    if (!nextMustBeStartChar)
                    {
                        builder.Append(c);
                    }
                    continue;
                }

                switch (Char.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                    case UnicodeCategory.ModifierLetter:
                    case UnicodeCategory.OtherLetter:
                    case UnicodeCategory.LetterNumber:
                        {
                            nextMustBeStartChar = false;
                            builder.Append(c);
                            break;
                        }
                    case UnicodeCategory.NonSpacingMark:
                    case UnicodeCategory.SpacingCombiningMark:
                    case UnicodeCategory.DecimalDigitNumber:
                    case UnicodeCategory.ConnectorPunctuation:
                        if (!nextMustBeStartChar)
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            return builder.ToString();
        }

        // <summary>
        // Marks the database as having not been initialized. This is called when the app calls Database.Delete so
        // that the database if the app attempts to then use the database again it will be re-initialized automatically.
        // </summary>
        public override void MarkDatabaseNotInitialized()
        {
            if (!InInitializationAction)
            {
                RetryAction<InternalContext> _;
                InitializedDatabases.TryRemove(Tuple.Create(_model, _internalConnection.ConnectionKey), out _);
            }
        }

        // <summary>
        // Marks the database as having been initialized without actually running the
        // <see
        //     cref="IDatabaseInitializer{TContext}" />
        // .
        // </summary>
        public override void MarkDatabaseInitialized()
        {
            InitializeContext();
            InitializeDatabaseAction(c => { });
        }

        // <summary>
        // Runs the <see cref="IDatabaseInitializer{TContext}" /> unless it has already been run or there
        // is no initializer for this context type in which case this method does nothing.
        // </summary>
        protected override void InitializeDatabase()
        {
            InitializeDatabaseAction(c => c.PerformDatabaseInitialization());
        }

        // <summary>
        // Performs some action (which may do nothing) in such a way that it is guaranteed only to be run
        // once for the model and connection in this app domain, unless it fails by throwing an exception,
        // in which case it will be re-tried next time the context is initialized.
        // </summary>
        // <param name="action"> The action. </param>
        private void InitializeDatabaseAction(Action<InternalContext> action)
        {
            if (!_inDatabaseInitialization && !InitializerDisabled)
            {
                try
                {
                    _inDatabaseInitialization = true;

                    // The idea here is that multiple threads can try to put an entry into InitializedDatabases
                    // at the same time but only one entry will actually make it into the collection, even though
                    // several may be constructed. The RetryAction ensures that that delegate only gets called
                    // exactly one time, thereby ensuring that database initialization will only happen once.  But,
                    // sometimes the delegate will fail (and throw and exception). This may be due to some resource
                    // issue--most notably a problem with the database connection. In such a situation it makes
                    // sense to have initialization try again later when the resource issue has potentially been
                    // resolved. To enable this RetryAction will try again next time PerformAction called. We
                    // have to pass the context to PerformAction so that the next time it tries again it will use
                    // the new connection.
                    InitializedDatabases.GetOrAdd(
                        Tuple.Create(_model, _internalConnection.ConnectionKey),
                        t => new RetryAction<InternalContext>(action)).PerformAction(this);
                }
                finally
                {
                    _inDatabaseInitialization = false;
                    _modelBeingInitialized = null;
                }
            }
        }

        // <summary>
        // Gets the default database initializer to use for this context if no other has been registered.
        // For code first this property returns a <see cref="CreateDatabaseIfNotExists{TContext}" /> instance.
        // For database/model first, this property returns null.
        // </summary>
        // <value> The default initializer. </value>
        public override IDatabaseInitializer<DbContext> DefaultInitializer
        {
            get { return _model != null ? _defaultCodeFirstInitializer : null; }
        }

        #endregion

        #region Context options

        public override bool EnsureTransactionsForFunctionsAndCommands
        {
            get
            {
                var objectContext = ObjectContextInUse;
                return objectContext != null ? objectContext.ContextOptions.EnsureTransactionsForFunctionsAndCommands : _initialEnsureTransactionsForFunctionsAndCommands;
            }
            set
            {
                var objectContext = ObjectContextInUse;
                if (objectContext != null)
                {
                    objectContext.ContextOptions.EnsureTransactionsForFunctionsAndCommands = value;
                }
                else
                {
                    _initialEnsureTransactionsForFunctionsAndCommands = value;
                }
            }
        }

        // <summary>
        // Gets or sets a value indicating whether lazy loading is enabled.
        // If the underlying <see cref="ObjectContext" /> exists, then this property acts as a wrapper over the flag stored there.
        // If the underlying <see cref="ObjectContext" /> has not been created yet, then we store the value given so we can later
        // use it when we create the <see cref="ObjectContext" />.  This allows the flag to be changed, for example in
        // a DbContext constructor, without it causing the <see cref="ObjectContext" /> to be created.
        // </summary>
        public override bool LazyLoadingEnabled
        {
            get
            {
                var objectContext = ObjectContextInUse;
                return objectContext != null
                           ? objectContext.ContextOptions.LazyLoadingEnabled
                           : _initialLazyLoadingFlag;
            }
            set
            {
                var objectContext = ObjectContextInUse;
                if (objectContext != null)
                {
                    objectContext.ContextOptions.LazyLoadingEnabled = value;
                }
                else
                {
                    _initialLazyLoadingFlag = value;
                }
            }
        }

        // <summary>
        // Gets or sets a value indicating whether proxy creation is enabled.
        // If the underlying ObjectContext exists, then this property acts as a wrapper over the flag stored there.
        // If the underlying ObjectContext has not been created yet, then we store the value given so we can later
        // use it when we create the ObjectContext.  This allows the flag to be changed, for example in
        // a DbContext constructor, without it causing the ObjectContext to be created.
        // </summary>
        public override bool ProxyCreationEnabled
        {
            get
            {
                var objectContext = ObjectContextInUse;
                return objectContext != null
                           ? objectContext.ContextOptions.ProxyCreationEnabled
                           : _initialProxyCreationFlag;
            }
            set
            {
                var objectContext = ObjectContextInUse;
                if (objectContext != null)
                {
                    objectContext.ContextOptions.ProxyCreationEnabled = value;
                }
                else
                {
                    _initialProxyCreationFlag = value;
                }
            }
        }

        // <summary>
        // Gets or sets a value indicating whether database null comparison behavior is enabled.
        // If the underlying ObjectContext exists, then this property acts as a wrapper over the flag stored there.
        // If the underlying ObjectContext has not been created yet, then we store the value given so we can later
        // use it when we create the ObjectContext.  This allows the flag to be changed, for example in
        // a DbContext constructor, without it causing the ObjectContext to be created.
        // </summary>
        public override bool UseDatabaseNullSemantics
        {
            get
            {
                var objectContext = ObjectContextInUse;
                return objectContext != null
                           ? !objectContext.ContextOptions.UseCSharpNullComparisonBehavior
                           : _useDatabaseNullSemanticsFlag;
            }
            set
            {
                var objectContext = ObjectContextInUse;
                if (objectContext != null)
                {
                    objectContext.ContextOptions.UseCSharpNullComparisonBehavior = !value;
                }
                else
                {
                    _useDatabaseNullSemanticsFlag = value;
                }
            }
        }

        public override int? CommandTimeout
        {
            get
            {
                var objectContext = ObjectContextInUse;
                return objectContext != null ? objectContext.CommandTimeout : _commandTimeout;
            }
            set
            {
                var objectContext = ObjectContextInUse;
                if (objectContext != null)
                {
                    objectContext.CommandTimeout = value;
                }
                else
                {
                    _commandTimeout = value;
                }
            }
        }

        #endregion

        public override string DefaultSchema
        {
            get { return CodeFirstModel.DefaultSchema; }
        }
    }
}
