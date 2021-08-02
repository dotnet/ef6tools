// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.DependencyResolution
{
    using System.ComponentModel;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Event arguments passed to <see cref="DbConfiguration.Loaded" /> event handlers.
    /// </summary>
    public class DbConfigurationLoadedEventArgs : EventArgs
    {
        private readonly InternalConfiguration _internalConfiguration;

        internal DbConfigurationLoadedEventArgs(InternalConfiguration configuration)
        {
            DebugCheck.NotNull(configuration);

            _internalConfiguration = configuration;
        }

        /// <summary>
        /// Returns a snapshot of the <see cref="IDbDependencyResolver" /> that is about to be locked.
        /// Use the GetService methods on this object to get services that have been registered.
        /// </summary>
        public IDbDependencyResolver DependencyResolver
        {
            get { return _internalConfiguration.ResolverSnapshot; }
        }

        /// <summary>
        /// Call this method to add a <see cref="IDbDependencyResolver" /> instance to the Chain of
        /// Responsibility of resolvers that are used to resolve dependencies needed by the Entity Framework.
        /// </summary>
        /// <remarks>
        /// Resolvers are asked to resolve dependencies in reverse order from which they are added. This means
        /// that a resolver can be added to override resolution of a dependency that would already have been
        /// resolved in a different way.
        /// The only exception to this is that any dependency registered in the application's config file
        /// will always be used in preference to using a dependency resolver added here, unless the
        /// overrideConfigFile is set to true in which case the resolver added here will also override config
        /// file settings.
        /// </remarks>
        /// <param name="resolver"> The resolver to add. </param>
        /// <param name="overrideConfigFile">If true, then the resolver added will take precedence over settings in the config file.</param>
        public void AddDependencyResolver(IDbDependencyResolver resolver, bool overrideConfigFile)
        {
            Check.NotNull(resolver, "resolver");

            _internalConfiguration.CheckNotLocked("AddDependencyResolver");
            _internalConfiguration.AddDependencyResolver(resolver, overrideConfigFile);
        }

        /// <summary>
        /// Call this method to add a <see cref="IDbDependencyResolver" /> instance to the Chain of Responsibility
        /// of resolvers that are used to resolve dependencies needed by the Entity Framework. Unlike the AddDependencyResolver
        /// method, this method puts the resolver at the bottom of the Chain of Responsibility such that it will only
        /// be used to resolve a dependency that could not be resolved by any of the other resolvers.
        /// </summary>
        /// <param name="resolver"> The resolver to add. </param>
        public void AddDefaultResolver(IDbDependencyResolver resolver)
        {
            Check.NotNull(resolver, "resolver");

            _internalConfiguration.CheckNotLocked("AddDefaultResolver");
            _internalConfiguration.AddDefaultResolver(resolver);
        }

        /// <summary>
        /// Adds a wrapping resolver to the configuration that is about to be locked. A wrapping
        /// resolver is a resolver that incepts a service would have been returned by the resolver
        /// chain and wraps or replaces it with another service of the same type.
        /// </summary>
        /// <typeparam name="TService">The type of service to wrap or replace.</typeparam>
        /// <param name="serviceInterceptor">A delegate that takes the unwrapped service and key and returns the wrapped or replaced service.</param>
        public void ReplaceService<TService>(Func<TService, object, TService> serviceInterceptor)
        {
            Check.NotNull(serviceInterceptor, "serviceInterceptor");

            AddDependencyResolver(
                new WrappingDependencyResolver<TService>(DependencyResolver, serviceInterceptor),
                overrideConfigFile: true);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
