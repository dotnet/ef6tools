// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.MappingViews
{
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Defines a custom attribute that specifies the mapping view cache type (subclass of <see cref="DbMappingViewCache" />)
    /// associated with a context type (subclass of <see cref="ObjectContext" /> or <see cref="DbContext" />).
    /// The cache type is instantiated at runtime and used to retrieve pre-generated views in the
    /// corresponding context.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DbMappingViewCacheTypeAttribute : Attribute
    {
        private readonly Type _contextType;
        private readonly Type _cacheType;

        /// <summary>
        /// Creates a <see cref="DbMappingViewCacheTypeAttribute" />  instance that associates a context type
        /// with a mapping view cache type.
        /// </summary>
        /// <param name="contextType">
        /// A subclass of <see cref="ObjectContext" /> or <see cref="DbContext" />.
        /// </param>
        /// <param name="cacheType">
        /// A subclass of <see cref="DbMappingViewCache" />.
        /// </param>
        public DbMappingViewCacheTypeAttribute(Type contextType, Type cacheType)
        {
            Check.NotNull(contextType, "contextType");
            Check.NotNull(cacheType, "cacheType");

            if (!contextType.IsSubclassOf(typeof(ObjectContext))
                && !contextType.IsSubclassOf(typeof(DbContext)))
            {
                throw new ArgumentException(
                    Strings.DbMappingViewCacheTypeAttribute_InvalidContextType(contextType),
                    "contextType");
            }

            if (!cacheType.IsSubclassOf(typeof(DbMappingViewCache)))
            {
                throw new ArgumentException(
                    Strings.Generated_View_Type_Super_Class(cacheType),
                    "cacheType");
            }

            _contextType = contextType;
            _cacheType = cacheType;
        }

        /// <summary>
        /// Creates a <see cref="DbMappingViewCacheTypeAttribute" /> instance that associates a context type
        /// with a mapping view cache type.
        /// </summary>
        /// <param name="contextType">
        /// A subclass of <see cref="ObjectContext" /> or <see cref="DbContext" />.
        /// </param>
        /// <param name="cacheTypeName">The assembly qualified full name of the cache type.</param>
        public DbMappingViewCacheTypeAttribute(Type contextType, string cacheTypeName)
        {
            Check.NotNull(contextType, "contextType");
            Check.NotEmpty(cacheTypeName, "cacheTypeName");

            if (!contextType.IsSubclassOf(typeof(ObjectContext))
                && !contextType.IsSubclassOf(typeof(DbContext)))
            {
                throw new ArgumentException(
                    Strings.DbMappingViewCacheTypeAttribute_InvalidContextType(contextType),
                    "contextType");
            }

            _contextType = contextType;

            try
            {
                _cacheType = Type.GetType(cacheTypeName, throwOnError: true);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    Strings.DbMappingViewCacheTypeAttribute_CacheTypeNotFound(cacheTypeName),
                    "cacheTypeName",
                    ex);
            }
        }

        // <summary>
        // Gets the context type that is associated with the mapping view cache type.
        // </summary>
        internal Type ContextType
        {
            get { return _contextType; }
        }

        // <summary>
        // Gets the type that implements the mapping view cache.
        // </summary>
        internal Type CacheType
        {
            get { return _cacheType; }
        }
    }
}
