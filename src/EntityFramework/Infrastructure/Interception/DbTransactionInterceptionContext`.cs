﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents contextual information associated with calls to <see cref="DbTransaction"/> with return type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The return type of the target method.</typeparam>
    public class DbTransactionInterceptionContext<TResult> : MutableInterceptionContext<TResult>
    {
        /// <summary>
        /// Constructs a new <see cref="DbTransactionInterceptionContext{TResult}" /> with no state.
        /// </summary>
        public DbTransactionInterceptionContext()
        {
        }

        /// <summary>
        /// Creates a new <see cref="DbTransactionInterceptionContext{TResult}" /> by copying immutable state from the given
        /// interception context. Also see <see cref="Clone" />
        /// </summary>
        /// <param name="copyFrom">The context from which to copy state.</param>
        public DbTransactionInterceptionContext(DbInterceptionContext copyFrom)
            : base(copyFrom)
        {
            Check.NotNull(copyFrom, "copyFrom");
        }

        /// <summary>
        /// Creates a new <see cref="DbTransactionInterceptionContext{TResult}" /> that contains all the contextual information in this
        /// interception context together with the <see cref="DbInterceptionContext.IsAsync" /> flag set to true.
        /// </summary>
        /// <returns>A new interception context associated with the async flag set.</returns>
        public new DbTransactionInterceptionContext<TResult> AsAsync()
        {
            return (DbTransactionInterceptionContext<TResult>)base.AsAsync();
        }

        /// <summary>
        /// Creates a new <see cref="DbTransactionInterceptionContext{TResult}" /> that contains all the contextual information in this
        /// interception context with the addition of the given <see cref="ObjectContext" />.
        /// </summary>
        /// <param name="context">The context to associate.</param>
        /// <returns>A new interception context associated with the given context.</returns>
        public new DbTransactionInterceptionContext<TResult> WithDbContext(DbContext context)
        {
            Check.NotNull(context, "context");

            return (DbTransactionInterceptionContext<TResult>)base.WithDbContext(context);
        }

        /// <summary>
        /// Creates a new <see cref="DbTransactionInterceptionContext{TResult}" /> that contains all the contextual information in this
        /// interception context with the addition of the given <see cref="ObjectContext" />.
        /// </summary>
        /// <param name="context">The context to associate.</param>
        /// <returns>A new interception context associated with the given context.</returns>
        public new DbTransactionInterceptionContext<TResult> WithObjectContext(ObjectContext context)
        {
            Check.NotNull(context, "context");

            return (DbTransactionInterceptionContext<TResult>)base.WithObjectContext(context);
        }

        /// <inheritdoc />
        protected override DbInterceptionContext Clone()
        {
            return new DbTransactionInterceptionContext<TResult>(this);
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
