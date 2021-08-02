﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Interception
{
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Used for dispatching operations to a <see cref="DbTransaction" /> such that any <see cref="IDbTransactionInterceptor" />
    /// registered on <see cref="DbInterception" /> will be notified before and after the
    /// operation executes.
    /// Instances of this class are obtained through the the <see cref="DbInterception.Dispatch" /> fluent API.
    /// </summary>
    /// <remarks>
    /// This class is used internally by Entity Framework when interacting with <see cref="DbTransaction" />.
    /// It is provided publicly so that code that runs outside of the core EF assemblies can opt-in to command
    /// interception/tracing. This is typically done by EF providers that are executing commands on behalf of EF.
    /// </remarks>
    public class DbTransactionDispatcher
    {
        private readonly InternalDispatcher<IDbTransactionInterceptor> _internalDispatcher
            = new InternalDispatcher<IDbTransactionInterceptor>();

        internal InternalDispatcher<IDbTransactionInterceptor> InternalDispatcher
        {
            get { return _internalDispatcher; }
        }

        internal DbTransactionDispatcher()
        {
        }

        /// <summary>
        /// Sends <see cref="IDbTransactionInterceptor.ConnectionGetting" /> and
        /// <see cref="IDbTransactionInterceptor.ConnectionGot" /> to any <see cref="IDbTransactionInterceptor" />
        /// registered on <see cref="DbInterception" /> before/after
        /// getting <see cref="DbTransaction.Connection" />.
        /// </summary>
        /// <remarks>
        /// Note that the value of the property is returned by this method. The result is not available
        /// in the interception context passed into this method since the interception context is cloned before
        /// being passed to interceptors.
        /// </remarks>
        /// <param name="transaction">The transaction on which the operation will be executed.</param>
        /// <param name="interceptionContext">Optional information about the context of the call being made.</param>
        /// <returns>The result of the operation, which may have been modified by interceptors.</returns>
        public virtual DbConnection GetConnection(DbTransaction transaction, DbInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            return InternalDispatcher.Dispatch(
                transaction,
                (t, c) => t.Connection,
                new DbTransactionInterceptionContext<DbConnection>(interceptionContext),
                (i, t, c) => i.ConnectionGetting(t, c),
                (i, t, c) => i.ConnectionGot(t, c));
        }

        /// <summary>
        /// Sends <see cref="IDbTransactionInterceptor.IsolationLevelGetting" /> and
        /// <see cref="IDbTransactionInterceptor.IsolationLevelGot" /> to any <see cref="IDbTransactionInterceptor" />
        /// registered on <see cref="DbInterception" /> before/after
        /// getting <see cref="DbTransaction.IsolationLevel" />.
        /// </summary>
        /// <remarks>
        /// Note that the value of the property is returned by this method. The result is not available
        /// in the interception context passed into this method since the interception context is cloned before
        /// being passed to interceptors.
        /// </remarks>
        /// <param name="transaction">The transaction on which the operation will be executed.</param>
        /// <param name="interceptionContext">Optional information about the context of the call being made.</param>
        /// <returns>The result of the operation, which may have been modified by interceptors.</returns>
        public virtual IsolationLevel GetIsolationLevel(DbTransaction transaction, DbInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            return InternalDispatcher.Dispatch(
                transaction,
                (t, c) => t.IsolationLevel,
                new DbTransactionInterceptionContext<IsolationLevel>(interceptionContext),
                (i, t, c) => i.IsolationLevelGetting(t, c),
                (i, t, c) => i.IsolationLevelGot(t, c));
        }

        /// <summary>
        /// Sends <see cref="IDbTransactionInterceptor.Committing" /> and
        /// <see cref="IDbTransactionInterceptor.Committed" /> to any <see cref="IDbConnectionInterceptor" />
        /// registered on <see cref="DbInterception" /> before/after making a
        /// call to <see cref="DbTransaction.Commit" />.
        /// </summary>
        /// <param name="transaction">The transaction on which the operation will be executed.</param>
        /// <param name="interceptionContext">Optional information about the context of the call being made.</param>
        public virtual void Commit(DbTransaction transaction, DbInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            InternalDispatcher.Dispatch(
                transaction,
                (t, c) => t.Commit(),
                new DbTransactionInterceptionContext(interceptionContext).WithConnection(transaction.Connection),
                (i, t, c) => i.Committing(t, c),
                (i, t, c) => i.Committed(t, c));
        }

        /// <summary>
        /// Sends <see cref="IDbTransactionInterceptor.Disposing" /> and
        /// <see cref="IDbTransactionInterceptor.Disposed" /> to any <see cref="IDbConnectionInterceptor" />
        /// registered on <see cref="DbInterception" /> before/after making a
        /// call to <see cref="DbTransaction.Dispose()" />.
        /// </summary>
        /// <param name="transaction">The transaction on which the operation will be executed.</param>
        /// <param name="interceptionContext">Optional information about the context of the call being made.</param>
        public virtual void Dispose(DbTransaction transaction, DbInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            var clonedInterceptionContext = new DbTransactionInterceptionContext(interceptionContext);

            if (transaction.Connection != null)
            {
                clonedInterceptionContext = clonedInterceptionContext.WithConnection(transaction.Connection);
            }

            InternalDispatcher.Dispatch(
                transaction,
                (t, c) => t.Dispose(),
                clonedInterceptionContext,
                (i, t, c) => i.Disposing(t, c),
                (i, t, c) => i.Disposed(t, c));
        }

        /// <summary>
        /// Sends <see cref="IDbTransactionInterceptor.RollingBack" /> and
        /// <see cref="IDbTransactionInterceptor.RolledBack" /> to any <see cref="IDbConnectionInterceptor" />
        /// registered on <see cref="DbInterception" /> before/after making a
        /// call to <see cref="DbTransaction.Rollback" />.
        /// </summary>
        /// <param name="transaction">The transaction on which the operation will be executed.</param>
        /// <param name="interceptionContext">Optional information about the context of the call being made.</param>
        public virtual void Rollback(DbTransaction transaction, DbInterceptionContext interceptionContext)
        {
            Check.NotNull(transaction, "transaction");
            Check.NotNull(interceptionContext, "interceptionContext");

            InternalDispatcher.Dispatch(
                transaction,
                (t, c) => t.Rollback(),
                new DbTransactionInterceptionContext(interceptionContext).WithConnection(transaction.Connection),
                (i, t, c) => i.RollingBack(t, c),
                (i, t, c) => i.RolledBack(t, c));
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

        /// <summary>
        /// Gets the <see cref="Type" /> of the current instance.
        /// </summary>
        /// <returns>The exact runtime type of the current instance.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
