// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Collections;
    using System.ComponentModel;
    using System.Data.Entity.Internal.Linq;
    using System.Data.Entity.Resources;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents a non-generic LINQ to Entities query against a DbContext.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    [DebuggerDisplay(@"{DebuggerDisplay()}")]
    public abstract class DbQuery : IOrderedQueryable, IListSource, IInternalQueryAdapter
#if !NET40
, IDbAsyncEnumerable
#endif
    {
        #region Fields and constructors

        private IQueryProvider _provider;

        // <summary>
        // Internal constructor prevents external classes deriving from DbQuery.
        // </summary>
        internal DbQuery()
        {
        }

        #endregion

        #region Data binding

        /// <summary>
        /// Returns <c>false</c>.
        /// </summary>
        /// <returns>
        /// <c>false</c> .
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool IListSource.ContainsListCollection
        {
            get { return false; }
        }

        /// <summary>
        /// Throws an exception indicating that binding directly to a store query is not supported.
        /// Instead populate a DbSet with data, for example by using the Load extension method, and
        /// then bind to local data.  For WPF bind to DbSet.Local.  For Windows Forms bind to
        /// DbSet.Local.ToBindingList().
        /// </summary>
        /// <returns> Never returns; always throws. </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        IList IListSource.GetList()
        {
            throw Error.DbQuery_BindingToDbQueryNotSupported();
        }

        #endregion

        #region IEnumerable

        /// <summary>
        /// Returns an <see cref="IEnumerator" /> which when enumerated will execute the query against the database.
        /// </summary>
        /// <returns> The query results. </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetInternalQueryWithCheck("IEnumerable.GetEnumerator").GetEnumerator();
        }

        #endregion

        #region IDbAsyncEnumerable

#if !NET40

        /// <summary>
        /// Returns an <see cref="IDbAsyncEnumerator" /> which when enumerated will execute the query against the database.
        /// </summary>
        /// <returns> The query results. </returns>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return GetInternalQueryWithCheck("IDbAsyncEnumerable.GetAsyncEnumerator").GetAsyncEnumerator();
        }

#endif

        #endregion

        #region IQueryable

        /// <summary>
        /// The IQueryable element type.
        /// </summary>
        public virtual Type ElementType
        {
            get { return GetInternalQueryWithCheck("ElementType").ElementType; }
        }

        /// <summary>
        /// The IQueryable LINQ Expression.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        Expression IQueryable.Expression
        {
            get { return GetInternalQueryWithCheck("IQueryable.Expression").Expression; }
        }

        /// <summary>
        /// The IQueryable provider.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        IQueryProvider IQueryable.Provider
        {
            get
            {
                return _provider ?? (_provider = new NonGenericDbQueryProvider(
                                                     GetInternalQueryWithCheck("IQueryable.Provider").InternalContext,
                                                     GetInternalQueryWithCheck("IQueryable.Provider")));
            }
        }

        #endregion

        #region Include

        /// <summary>
        /// Specifies the related objects to include in the query results.
        /// </summary>
        /// <remarks>
        /// Paths are all-inclusive. For example, if an include call indicates Include("Orders.OrderLines"), not only will
        /// OrderLines be included, but also Orders.  When you call the Include method, the query path is only valid on
        /// the returned instance of the DbQuery&lt;T&gt;. Other instances of DbQuery&lt;T&gt; and the object context itself are not affected.
        /// Because the Include method returns the query object, you can call this method multiple times on an DbQuery&lt;T&gt; to
        /// specify multiple paths for the query.
        /// </remarks>
        /// <param name="path"> The dot-separated list of related objects to return in the query results. </param>
        /// <returns>
        /// A new DbQuery&lt;T&gt; with the defined query path.
        /// </returns>
        public virtual DbQuery Include(string path)
        {
            return this;
        }

        #endregion

        #region AsNoTracking

        /// <summary>
        /// Returns a new query where the entities returned will not be cached in the <see cref="DbContext" />.
        /// </summary>
        /// <returns> A new query with NoTracking applied. </returns>
        public virtual DbQuery AsNoTracking()
        {
            return this;
        }

        #endregion

        #region AsStreaming

        /// <summary>
        /// Returns a new query that will stream the results instead of buffering.
        /// </summary>
        /// <returns> A new query with AsStreaming applied. </returns>
        [Obsolete("Queries are now streaming by default unless a retrying ExecutionStrategy is used. Calling this method will have no effect.")]
        public virtual DbQuery AsStreaming()
        {
            return this;
        }

        #endregion

        internal virtual DbQuery WithExecutionStrategy(IDbExecutionStrategy executionStrategy)
        {
            return this;
        }

        #region Conversion to generic

        /// <summary>
        /// Returns the equivalent generic <see cref="DbQuery{TElement}" /> object.
        /// </summary>
        /// <typeparam name="TElement"> The type of element for which the query was created. </typeparam>
        /// <returns> The generic set object. </returns>
        public DbQuery<TElement> Cast<TElement>()
        {
            if (InternalQuery == null)
            {
                throw new NotSupportedException(Strings.TestDoublesCannotBeConverted);
            }

            if (typeof(TElement) != InternalQuery.ElementType)
            {
                throw Error.DbEntity_BadTypeForCast(
                    typeof(DbQuery).Name, typeof(TElement).Name, InternalQuery.ElementType.Name);
            }

            return new DbQuery<TElement>((IInternalQuery<TElement>)InternalQuery);
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns a <see cref="System.String" /> representation of the underlying query.
        /// </summary>
        /// <returns> The query string. </returns>
        public override string ToString()
        {
            return InternalQuery == null ? base.ToString() : InternalQuery.ToTraceString();
        }

        private string DebuggerDisplay()
        {
            return base.ToString();
        }

        /// <summary>
        /// Gets a <see cref="System.String" /> representation of the underlying query.
        /// </summary>
        public string Sql
        {
            get { return ToString(); }
        }

        #endregion

        #region InternalQuery

        // <summary>
        // Gets the underlying internal query object.
        // </summary>
        // <value> The internal query. </value>
        internal virtual IInternalQuery InternalQuery
        {
            get { return null; }
        }

        internal virtual IInternalQuery GetInternalQueryWithCheck(string memberName)
        {
            throw new NotImplementedException(Strings.TestDoubleNotImplemented(memberName, GetType().Name, typeof(DbSet).Name));
        }

        // <summary>
        // The internal query object that is backing this DbQuery
        // </summary>
        IInternalQuery IInternalQueryAdapter.InternalQuery
        {
            get { return InternalQuery; }
        }

        #endregion

        #region Hidden Object methods

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

        #endregion
    }
}
