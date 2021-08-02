// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Core.EntityClient;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// An immutable representation of an Entity Data Model (EDM) model that can be used to create an
    /// <see cref="ObjectContext" /> or can be passed to the constructor of a <see cref="DbContext" />.
    /// For increased performance, instances of this type should be cached and re-used to construct contexts.
    /// </summary>
    public class DbCompiledModel
    {
        #region Fields and constructors

        // Cached delegates that have been created dynamically to call a constructors for a given derived type of ObjectContext.
        private static readonly ConcurrentDictionary<Type, Func<EntityConnection, ObjectContext>> _contextConstructors =
            new ConcurrentDictionary<Type, Func<EntityConnection, ObjectContext>>();

        // Delegate to create an instance of a non-derived ObjectContext.
        private static readonly Func<EntityConnection, ObjectContext> _objectContextConstructor =
            c => new ObjectContext(c);

        // An object that can be used to get a cached MetadataWorkspace.
        private readonly ICachedMetadataWorkspace _workspace;

        private readonly DbModelBuilder _cachedModelBuilder;
        private readonly string _defaultSchema;

        // <summary>
        // For mocking.
        // </summary>
        internal DbCompiledModel()
        {
        }

        internal DbCompiledModel(CodeFirstCachedMetadataWorkspace workspace, DbModelBuilder cachedModelBuilder)
        {
            _workspace = workspace;
            _cachedModelBuilder = cachedModelBuilder;
            _defaultSchema = cachedModelBuilder.ModelConfiguration.DefaultSchema;
        }

        internal DbCompiledModel(CodeFirstCachedMetadataWorkspace workspace, string defaultSchema)
        {
            _workspace = workspace;
            _defaultSchema = defaultSchema;
        }

        #endregion

        #region Model/database metadata

        // <summary>
        // A snapshot of the <see cref="DbModelBuilder" /> that was used to create this compiled model.
        // </summary>
        internal virtual DbModelBuilder CachedModelBuilder
        {
            get { return _cachedModelBuilder; }
        }

        // <summary>
        // The provider info (provider name and manifest token) that was used to create this model.
        // </summary>
        internal virtual DbProviderInfo ProviderInfo
        {
            get { return _workspace.ProviderInfo; }
        }

        // <summary> Gets the default schema of the model. </summary>
        // <returns> The default schema of the model. </returns>
        internal string DefaultSchema
        {
            get { return _defaultSchema; }
        }

        #endregion

        #region CreateObjectContext

        /// <summary>
        /// Creates an instance of ObjectContext or class derived from ObjectContext.  Note that an instance
        /// of DbContext can be created instead by using the appropriate DbContext constructor.
        /// If a derived ObjectContext is used, then it must have a public constructor with a single
        /// EntityConnection parameter.
        /// The connection passed is used by the ObjectContext created, but is not owned by the context.  The caller
        /// must dispose of the connection once the context has been disposed.
        /// </summary>
        /// <typeparam name="TContext"> The type of context to create. </typeparam>
        /// <param name="existingConnection"> An existing connection to a database for use by the context. </param>
        /// <returns>The context.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public TContext CreateObjectContext<TContext>(DbConnection existingConnection) where TContext : ObjectContext
        {
            Check.NotNull(existingConnection, "existingConnection");

            var metadataWorkspace = _workspace.GetMetadataWorkspace(existingConnection);
            var entityConnection = new EntityConnection(metadataWorkspace, existingConnection);
            var context = (TContext)GetConstructorDelegate<TContext>()(entityConnection);
            context.ContextOwnsConnection = true;

            // Set the DefaultContainerName if it is empty
            if (String.IsNullOrEmpty(context.DefaultContainerName))
            {
                context.DefaultContainerName = _workspace.DefaultContainerName;
            }

            foreach (var assembly in _workspace.Assemblies)
            {
                context.MetadataWorkspace.LoadFromAssembly(assembly);
            }

            return context;
        }

        // <summary>
        // Gets a cached delegate (or creates a new one) used to call the constructor for the given derived ObjectContext type.
        // </summary>
        internal static Func<EntityConnection, ObjectContext> GetConstructorDelegate<TContext>()
            where TContext : ObjectContext
        {
            // Optimize for case where just ObjectContext (non-derived) is asked for.
            if (typeof(TContext) == typeof(ObjectContext))
            {
                return _objectContextConstructor;
            }

            Func<EntityConnection, ObjectContext> constructorDelegate;
            if (!_contextConstructors.TryGetValue(typeof(TContext), out constructorDelegate))
            {
                // This is a reasonable non-ambiguous ordering of constructor lookups to preserve
                // everything that worked with the older Reflection APIs. Some classes that previously
                // would not have worked due to ambiguous constructor matches will now work since this
                // is less error-prone than attempting to re-implement the .NET best matching algorithm.
                var constructor = typeof(TContext).GetDeclaredConstructor(
                    c => c.IsPublic,
                    new[] { typeof(EntityConnection) },
                    new[] { typeof(DbConnection) },
                    new[] { typeof(IDbConnection) },
                    new[] { typeof(IDisposable) },
                    new[] { typeof(Component) },
                    new[] { typeof(MarshalByRefObject) },
                    new[] { typeof(object) });

                if (constructor == null)
                {
                    throw Error.DbModelBuilder_MissingRequiredCtor(typeof(TContext).Name);
                }

                var connectionParam = Expression.Parameter(typeof(EntityConnection), "connection");
                constructorDelegate =
                    Expression.Lambda<Func<EntityConnection, ObjectContext>>(
                        Expression.New(constructor, connectionParam), connectionParam).
                        Compile();

                _contextConstructors.TryAdd(typeof(TContext), constructorDelegate);
            }
            return constructorDelegate;
        }

        #endregion
    }
}
